using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Cryptography;

namespace QBasCopier;

public enum CopyAction { Overwrite, Skip, Resume, Rename, OverwriteIfDifferent, CancelAll, Retry }

public enum ItemState { Ready, Copying, Done, Skipped, Error, Cancelled, Conflict }

public sealed record CollisionDecision(CopyAction Action, bool Always);
public sealed record ErrorDecision(CopyAction Action, bool Always);

public sealed class CopyItem : INotifyPropertyChanged
{
    public string SourcePath { get; set; } = "";
    public string DestPath { get; set; } = "";
    public bool IsDirectory { get; set; }
    public bool IsContent { get; set; }   // true cuando SourcePath es content:// (Android/SAF)

    private long _total;
    public long TotalBytes { get => _total; set { _total = value; On(nameof(TotalBytes)); On(nameof(Percent)); } }

    private long _done;
    public long DoneBytes { get => _done; set { _done = value; On(nameof(DoneBytes)); On(nameof(Percent)); } }

    public double Percent => TotalBytes > 0 ? Math.Min(100, _done * 100.0 / TotalBytes) : 0;

    private ItemState _state = ItemState.Ready;
    public ItemState State { get => _state; set { _state = value; On(nameof(State)); On(nameof(StateText)); } }

    private string _stateText = "";
    public string StateText { get => _stateText; set { _stateText = value; On(nameof(StateText)); } }

    private string _speed = "";
    public string SpeedText { get => _speed; set { _speed = value; On(nameof(SpeedText)); } }

    private string _conflictInfo = "";
    public string ConflictInfo { get => _conflictInfo; set => On(nameof(ConflictInfo)); }

    public string Name => Path.GetFileName(SourcePath.TrimEnd('\\'));
    public string SizeText => FilePane.Human(TotalBytes);
    public string DestName => Path.GetFileName(DestPath.TrimEnd('\\'));

    public void AddBytes(long n) { DoneBytes = DoneBytes + n; }

    public event PropertyChangedEventHandler? PropertyChanged;
    void On(string p) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(p));
}

public sealed class CopyEngine
{
    public CopyEngine(Settings s) => S = s;

    public Settings S { get; }
    public List<CopyItem> Items { get; } = new();
    public bool Move { get; set; }

    public Func<CopyItem, string, Task<CollisionDecision>>? AskCollision;
    public Func<CopyItem, string, Task<ErrorDecision>>? AskError;

    private CancellationTokenSource? _cts;
    private volatile bool _paused;
    private readonly ManualResetEventSlim _gate = new(true);
    private readonly Stopwatch _throttleSw = Stopwatch.StartNew();
    private double _throttleBytes;
    private readonly Random _rnd = new();

    private long _totalBytes, _doneBytes;
    private int _ok, _err;
    private bool _cancelled;
    public bool IsBusy => _running;

    public long TotalBytes { get => Interlocked.Read(ref _totalBytes); }
    public long DoneBytes { get => Interlocked.Read(ref _doneBytes); }

    public event Action<CopyItem>? ItemStatus;      // per-file refresh
    public event Action? TotalsChanged;             // speed/totals refresh
    public event Action<string>? LogMessage;        // to error log tab
    public event Action<int, int, bool, bool>? BatchEnd; // ok, err, cancelled, paused

    private volatile bool _running;
    public bool PauseRequested => _paused;

    public void Pause()
    {
        _paused = true;
        _gate.Reset();
    }

    public void Resume()
    {
        _paused = false;
        _gate.Set();
    }

    public void Cancel()
    {
        _cts?.Cancel();
        _gate.Set();
        _cancelled = true;
    }

    public async Task RunAsync()
    {
        if (_running) return;
        _running = true;
        _cts = new CancellationTokenSource();
        _ok = _err = 0;
        _cancelled = false;

        try
        {
            ComputeTotals();
            ConfirmLimits();

            int threads = Math.Clamp(S.Threads, 1, 32);
            var tasks = new List<Task>();
            var next = new PendingIndex(Items);
            var bottom = new ConcurrentQueue<CopyItem>();

            for (int w = 0; w < threads; w++)
            {
                tasks.Add(Task.Run(() => WorkerAsync(next, bottom, _cts!.Token)));
            }

            await Task.WhenAll(tasks);
        }
        finally
        {
            _running = false;
            _gate.Set();
        }

        BatchEnd?.Invoke(_ok, _err, _cancelled, _paused);
    }

    private readonly object _lock = new();
    private sealed class PendingIndex
    {
        private readonly List<CopyItem> _it;
        private long _i = -1;
        public PendingIndex(List<CopyItem> it) => _it = it;
        public CopyItem? Next()
        {
            while (true)
            {
                var i = Interlocked.Increment(ref _i);
                if (i >= _it.Count) return null;
                var it = _it[(int)i];
                if (it.State is ItemState.Ready or ItemState.Conflict) return it;
            }
        }
    }

    private async Task WorkerAsync(PendingIndex next, ConcurrentQueue<CopyItem> bottom, CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            _gate.Wait();
            CopyItem? item;
            if (!bottom.TryDequeue(out item!))
            {
                item = next.Next();
                if (item == null)
                {
                    await Task.Delay(30, CancellationToken.None);
                    if (!bottom.IsEmpty) continue;
                    if (AllFinished()) return;
                    await Task.Delay(50, CancellationToken.None);
                    continue;
                }
            }

            try
            {
                var res = await CopyOneAsync(item, ct);
                switch (res)
                {
                    case CopyAction.Retry:
                        bottom.Enqueue(item);
                        break;
                    case CopyAction.CancelAll:
                        Cancel();
                        return;
                }
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (PauseInterrupt)
            {
                item.State = ItemState.Ready;
                item.StateText = L.Get("stateReady");
                return;
            }
        }
    }

    private sealed class PauseInterrupt : Exception { }

    private bool AllFinished()
    {
        lock (_lock)
        {
            foreach (var it in Items)
                if (it.State is ItemState.Ready or ItemState.Copying or ItemState.Conflict) return false;
        }
        return true;
    }

    private void ComputeTotals()
    {
        lock (_lock)
        {
            long t = 0;
            foreach (var it in Items)
                if (it.State is ItemState.Ready or ItemState.Conflict)
                    t += Math.Max(1, it.TotalBytes);
            _totalBytes = t;
        }
        TotalsChanged?.Invoke();
    }

    private void ConfirmLimits() => ApplyPriority();

    private void ApplyPriority()
    {
        try
        {
            using var p = Process.GetCurrentProcess();
            switch (S.Priority)
            {
                case "idle": p.PriorityClass = ProcessPriorityClass.Idle; break;
                case "high": p.PriorityClass = ProcessPriorityClass.High; break;
                default: p.PriorityClass = ProcessPriorityClass.Normal; break;
            }
        }
        catch { }
    }

    // ---------------- copy one ----------------
    private async Task<CopyAction> CopyOneAsync(CopyItem it, CancellationToken ct)
    {
        if (SkipHiddenOrSystem(it)) return Mark(it, ItemState.Skipped, L.Get("stateSkipped"));

        if (TryGetSize(it, out var size))
            it.TotalBytes = size;

        if (File.Exists(it.DestPath) || Directory.Exists(it.DestPath))
        {
            var cd = await ResolveCollisionAsync(it, ct);
            if (cd.Action == CopyAction.CancelAll) return CopyAction.CancelAll;
            if (cd.Action == CopyAction.Skip) return Mark(it, ItemState.Skipped, L.Get("stateSkipped"));
            if (cd.Action == CopyAction.Rename) it.DestPath = NextRenamePath(it.DestPath);
            if (cd.Action == CopyAction.OverwriteIfDifferent)
            {
                if (!it.IsDirectory && SameLengthAndNewer(it)) return Mark(it, ItemState.Skipped, L.Get("stateSkipped"));
            }
        }

        ct.ThrowIfCancellationRequested();
        it.State = ItemState.Copying;
        it.StateText = L.Get("stateCopying");
        it.SpeedText = "";
        it.DoneBytes = 0;
        ItemStatus?.Invoke(it);
        _gate.Wait();

        try
        {
            bool resume = it.State == ItemState.Conflict;

            if (it.IsDirectory)
                await CopyDirectoryAsync(it, ct);
            else
                await CopyFileWithEngineAsync(it, resume, ct);

            CopyMetadata(it);
            if (S.VerifyChecksum && !it.IsDirectory && !it.IsContent) VerifyIntegrity(it);

            if (Move && !it.IsContent)
            {
                try { if (it.IsDirectory) Directory.Delete(it.SourcePath, true); else File.Delete(it.SourcePath); }
                catch { }
            }

            TotalsChanged?.Invoke();
            return Mark(it, ItemState.Done, L.Get("stateDone"));
        }
        catch (PauseInterrupt) { throw; }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            Interlocked.Increment(ref _err);
            LogMessage?.Invoke($"{DateTime.Now:HH:mm:ss} | {it.SourcePath} | {ex.Message}");
            return await HandleErrorAsync(it, ex, ct).ConfigureAwait(false);
        }
    }

    private CopyAction Mark(CopyItem it, ItemState st, string txt)
    {
        it.State = st;
        it.StateText = txt;
        if (st == ItemState.Done) Interlocked.Increment(ref _ok);
        ItemStatus?.Invoke(it);
        return CopyAction.Skip;
    }

    private bool SkipHiddenOrSystem(CopyItem it)
    {
        if (it.IsContent) return false;
        if (!S.SkipHiddenSystem) return false;
        try { return (File.GetAttributes(it.SourcePath) & (FileAttributes.Hidden | FileAttributes.System)) != 0; }
        catch { return false; }
    }

    private bool TryGetSize(CopyItem it, out long size)
    {
        size = -1;
        try
        {
            if (it.IsContent)
            {
#if ANDROID
                var (_, sz) = QBasCopier.Android.DroidFile.Info(it.SourcePath);
                size = sz;
                return true;
#else
                return false;
#endif
            }
            if (it.IsDirectory)
            {
                size = new DirectoryInfo(it.SourcePath)
                       .EnumerateFiles("*", SearchOption.AllDirectories)
                       .Sum(f => f.Length);
                return size >= 0;
            }
            var fi = new FileInfo(it.SourcePath);
            size = fi.Length; return true;
        }
        catch { return false; }
    }

    private bool SameLengthAndNewer(CopyItem it)
    {
        try
        {
            var a = new FileInfo(it.SourcePath); var b = new FileInfo(it.DestPath);
            return a.Length == b.Length;
        }
        catch { return false; }
    }

    private string NextRenamePath(string path)
    {
        if (S != null && !string.IsNullOrEmpty(S.RenameNewPattern) && S.RenameNewPattern.Contains("%NAME%"))
        {
            var dir = Path.GetDirectoryName(path) ?? "";
            var name = Path.GetFileNameWithoutExtension(path);
            var ext = Path.GetExtension(path).TrimStart('.');
            for (int n = 1; n < 1000; n++)
            {
                var fn = S.RenameNewPattern
                    .Replace("%NAME%", name)
                    .Replace("%EXT%", ext)
                    .Replace("%COPY%", n.ToString());
                if (fn.Length == 0) break;
                var cand = Path.Combine(dir, fn);
                if (!File.Exists(cand) && !Directory.Exists(cand)) return cand;
            }
        }
        for (int n = 1; n < 100000; n++)
        {
            string cand = $"{Path.GetDirectoryName(path)}/{Path.GetFileNameWithoutExtension(path)} ({n}){Path.GetExtension(path)}";
            if (!File.Exists(cand) && !Directory.Exists(cand)) return cand;
        }
        return path;
    }

    // ------------- native + buffer engines -------------
    private async Task CopyFileWithEngineAsync(CopyItem it, bool resumeOffset, CancellationToken ct)
    {
        if (it.IsContent)
        {
#if ANDROID
            await ContentCopyAsync(it, ct);
#else
            await BufferedCopyAsync(it, resumeOffset, ct);
#endif
            return;
        }
        bool useNative = S.Engine != "buffer" && !resumeOffset && OperatingSystem.IsWindows();
        if (useNative)
            await NativeCopyAsync(it, ct);
        else
            await BufferedCopyAsync(it, resumeOffset, ct);
    }

#if ANDROID
    private async Task ContentCopyAsync(CopyItem it, CancellationToken ct)
    {
        using var src = QBasCopier.Android.DroidList.Open(it.SourcePath);
        int buf = (int)Math.Clamp(S.BufferBytes, 64 * 1024, 64 * 1024 * 1024);
        var dstStream = OpenDestWrite(it.DestPath, FileMode.Create, buf);
        if (dstStream == null) throw new IOException("No se pudo escribir en: " + it.DestPath);
        await using var dst = dstStream;
        var buffer = new byte[buf];
        long done = 0;
        while (true)
        {
            _gate.Wait(ct);
            if (_paused) continue;
            ct.ThrowIfCancellationRequested();
            int n = await src.ReadAsync(buffer, 0, buf, ct);
            if (n <= 0) break;
            Throttle(n);
            await dst.WriteAsync(buffer.AsMemory(0, n), ct);
            done += n;
            it.DoneBytes = done;
            it.TotalBytes = it.TotalBytes > 0 ? it.TotalBytes : done;
            Interlocked.Add(ref _doneBytes, n);
            ItemStatus?.Invoke(it);
            TotalsChanged?.Invoke();
        }
    }
#endif

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool CopyFileEx(string existing, string nw, CopyProgressRoutine? proc,
        IntPtr data, ref int cancel, uint flags);

    private delegate CopyProgressResult CopyProgressRoutine(long total, long done, long streamTotal,
        long streamDone, uint stream, uint reason, IntPtr hSrc, IntPtr hDst, IntPtr data);

    private enum CopyProgressResult : uint { Continue = 0, Cancel = 1, Stop = 2, Quiet = 3 }

    private const uint COPY_FILE_FAIL_IF_EXISTS = 0x1;
    private const uint COPY_FILE_RESTARTABLE = 0x2;
    private const uint COPY_FILE_ALLOW_DECRYPTED_DESTINATION = 0x8;

    private async Task NativeCopyAsync(CopyItem it, CancellationToken ct)
    {
        await Task.Yield();
        int cancelFlag = 0;
        bool pausedHere = false;

        long lastExt = 0;
        var cb = new CopyProgressRoutine((total, done, st, sd, sn, reason, hs, hd, d) =>
        {
            if (!_gate.IsSet || _paused) { pausedHere = true; return CopyProgressResult.Cancel; }
            if (ct.IsCancellationRequested) { cancelFlag = 1; return CopyProgressResult.Cancel; }
            Throttle(done);
            it.TotalBytes = total;
            long delta = done - lastExt;
            lastExt = done;
            it.AddBytes(delta);
            Interlocked.Add(ref _doneBytes, delta);
            ItemStatus?.Invoke(it);
            TotalsChanged?.Invoke();
            return CopyProgressResult.Continue;
        });

        bool ok = CopyFileEx(it.SourcePath, it.DestPath, cb, IntPtr.Zero, ref cancelFlag,
            S.OverwriteReadOnly ? 0u : COPY_FILE_FAIL_IF_EXISTS);
        if (!ok)
        {
            int err = Marshal.GetLastWin32Error();
            if (pausedHere) { if (DelPartial()) CleanupPartial(it); throw new PauseInterrupt(); }
            if (ct.IsCancellationRequested || cancelFlag != 0) { CleanupPartial(it); throw new OperationCanceledException(); }
            throw new IOException($"Win32 error {err}");
        }
    }

    private void Throttle(long done)
    {
        if (!S.SpeedLimitEnabled || done <= 0) return;
        double budget = S.SpeedLimitKb * 1024;
        double elapsed = _throttleSw.Elapsed.TotalSeconds;
        _throttleBytes += done;
        double allowed = budget * elapsed;
        if (_throttleBytes > allowed)
        {
            double over = _throttleBytes - allowed;
            int ms = (int)Math.Min(2000, over / Math.Max(1, budget / 1000.0));
            try { Thread.Sleep(ms); } catch { }
        }
        else
        {
            _throttleSw.Restart();
            _throttleBytes = 0;
        }
    }

    private async Task BufferedCopyAsync(CopyItem it, bool resume, CancellationToken ct)
    {
        long sourceLen = new FileInfo(it.SourcePath).Length;
        if (it.TotalBytes <= 0) it.TotalBytes = sourceLen;
        int buf = (int)Math.Clamp(S.BufferBytes, 64 * 1024, 64 * 1024 * 1024);

        await using var src = new FileStream(it.SourcePath, FileMode.Open, FileAccess.Read, FileShare.Read, buf, true);
        long offset = 0;
        Stream dst;
        if (resume && !IsSafPath(it.DestPath) && File.Exists(it.DestPath))
        {
            var ds = new FileInfo(it.DestPath);
            if (ds.Length < sourceLen)
            {
                offset = ds.Length;
                dst = new FileStream(it.DestPath, FileMode.Open, FileAccess.Write, FileShare.None, buf, true);
                dst.Seek(offset, SeekOrigin.Begin);
            }
            else { CleanupPartial(it); throw new IOException("Destination same or larger"); }
        }
        else
        {
            var d = OpenDestWrite(it.DestPath, FileMode.Create, buf);
            if (d == null) throw new IOException("No se pudo escribir en: " + it.DestPath);
            dst = d;
        }

        src.Seek(offset, SeekOrigin.Begin);
        var buffer = new byte[buf];
        try
        {
            long done = offset;
            while (done < sourceLen)
            {
                _gate.Wait(ct);
                if (_paused) continue;
                ct.ThrowIfCancellationRequested();
                int n = await src.ReadAsync(buffer, 0, buffer.Length, ct);
                if (n <= 0) break;
                Throttle(n);
                await dst.WriteAsync(buffer.AsMemory(0, n), ct);
                done += n;
                it.DoneBytes = done;
                it.TotalBytes = sourceLen;
                Interlocked.Add(ref _doneBytes, n);
                ItemStatus?.Invoke(it);
                TotalsChanged?.Invoke();
            }
        }
        finally
        {
            await dst.DisposeAsync();
        }
    }

    private async Task CopyDirectoryAsync(CopyItem it, CancellationToken ct)
    {
        // En Android el destino puede ser una carpeta SAF (content://). No existe en el
        // sistema de archivos, asi que no se puede crear con Directory.CreateDirectory:
        // DocumentsContract crea las carpetas solas al crear el documento hijo.
        if (!IsSafPath(it.DestPath)) Directory.CreateDirectory(it.DestPath);
        long acc = 0;
        foreach (var file in Directory.EnumerateFiles(it.SourcePath, "*", SearchOption.AllDirectories))
        {
            _gate.Wait(ct);
            ct.ThrowIfCancellationRequested();
            var rel = Path.GetRelativePath(it.SourcePath, file);
            var target = CombineDest(it.DestPath, rel);
            if (!IsSafPath(target))
            {
                var pd = Path.GetDirectoryName(target);
                if (!string.IsNullOrEmpty(pd)) Directory.CreateDirectory(pd);
            }
            var sub = new CopyItem { SourcePath = file, DestPath = target, TotalBytes = new FileInfo(file).Length };
            await CopyFileWithEngineAsync(sub, false, ct);
            acc += sub.TotalBytes;
            it.DoneBytes = acc;
            ItemStatus?.Invoke(it);
            TotalsChanged?.Invoke();
        }
    }

    /// <summary>
    /// Solo Android. Abre un destino content:// de SAF: (uriCarpeta, nombreArchivo) -> Stream.
    /// Lo asigna MainActivity al arrancar. En PC se queda null y se usa FileStream.
    /// </summary>
    public static Func<string, string, Stream?>? SafOpenDest { get; set; }

    /// <summary>True si la ruta es un content:// de SAF (solo Android).</summary>
    public static bool IsSafPath(string p) =>
        p.StartsWith("content://", StringComparison.OrdinalIgnoreCase);

    /// <summary>Une subruta con un destino SAF conservando el esquema content://.</summary>
    private static string CombineDest(string dir, string rel)
    {
        if (!IsSafPath(dir)) return Path.Combine(dir, rel);
        var slash = dir.EndsWith("/") ? dir : dir + "/";
        return slash + rel.Replace('\\', '/');
    }

    /// <summary>
    /// Abre el destino para escribir. En Android el destino puede ser un documento SAF,
    /// y en Android 11+ escribir con FileStream en /storage/emulated/0 esta prohibido
    /// (scoped storage), por eso se delega en DocumentsContract.
    /// </summary>
    private static Stream? OpenDestWrite(string destPath, FileMode mode, int buf)
    {
        if (IsSafPath(destPath) && SafOpenDest != null)
        {
            var i = destPath.LastIndexOf('/');
            var dir = i > 0 ? destPath.Substring(0, i) : destPath;
            var name = i >= 0 ? destPath.Substring(i + 1) : destPath;
            return SafOpenDest(dir, name);
        }
        return new FileStream(destPath, mode, FileAccess.Write, FileShare.None, buf, true);
    }

    private void CopyMetadata(CopyItem it)
    {
        if (it.IsContent) return;
        try
        {
            File.SetAttributes(it.DestPath, File.GetAttributes(it.SourcePath));
        }
        catch { }
        if (!S.CopyAttributes) return;
#if !ANDROID
        if (S.CopySecurity)
        {
            try
            {
                var acl = File.GetAccessControl(it.SourcePath);
                File.SetAccessControl(it.DestPath, acl);
            }
            catch { }
        }
#endif
        try
        {
            if (!it.IsDirectory)
            {
                File.SetLastWriteTime(it.DestPath, File.GetLastWriteTime(it.SourcePath));
                File.SetCreationTime(it.DestPath, File.GetCreationTime(it.SourcePath));
            }
        }
        catch { }
    }

    private void VerifyIntegrity(CopyItem it)
    {
        if (!File.Exists(it.SourcePath) || !File.Exists(it.DestPath))
            throw new IOException("verify: missing files");
        using var s = File.OpenRead(it.SourcePath);
        using var d = File.OpenRead(it.DestPath);
        var h1 = SHA256.HashData(s);
        var h2 = SHA256.HashData(d);
        if (!h1.AsSpan().SequenceEqual(h2))
            throw new IOException("SHA-256 mismatch");
    }

    private bool DelPartial() => S.DeleteUnfinished;

    private void CleanupPartial(CopyItem it)
    {
        if (!DelPartial()) return;
        try { if (File.Exists(it.DestPath)) File.Delete(it.DestPath); } catch { }
    }

    // ------------- collision / error resolvers -------------
    private async Task<CollisionDecision> ResolveCollisionAsync(CopyItem it, CancellationToken ct)
    {
        it.State = ItemState.Conflict;
        it.StateText = L.Get("colTitle");
        ItemStatus?.Invoke(it);

        switch (S.CollisionDefault)
        {
            case "cancel": return new CollisionDecision(CopyAction.CancelAll, false);
            case "skip": return new (CopyAction.Skip, false);
            case "resume": return new (CopyAction.Resume, false);
            case "overwrite": return new (CopyAction.Overwrite, false);
            case "overwriteIfDifferent": return new (CopyAction.OverwriteIfDifferent, false);
            case "renameNew": return new (CopyAction.Rename, false);
        }

        if (AskCollision == null) return new(CopyAction.Overwrite, false);
        return await AskCollision(it, it.DestPath);
    }

    private async Task<CopyAction> HandleErrorAsync(CopyItem it, Exception ex, CancellationToken ct)
    {
        it.State = ItemState.Error;
        it.StateText = L.Get("stateError");
        it.ConflictInfo = ex.Message;
        ItemStatus?.Invoke(it);

        switch (S.ErrorDefault)
        {
            case "skip": return CopyAction.Skip;
            case "retry":
                for (int i = 0; i < 3; i++)
                {
                    try
                    {
                        await Task.Delay(Math.Max(0, S.RetryIntervalMs), ct);
                        return await CopyOneAsync(it, ct);
                    }
                    catch { }
                }
                return CopyAction.Skip;
            case "cancel":
                Cancel();
                return CopyAction.CancelAll;
            case "bottom":
                return CopyAction.Retry;
        }

        if (AskError == null) return CopyAction.Skip;
        var dec = await AskError(it, ex.Message);

        if (dec.Action == CopyAction.Retry) return CopyAction.Retry;
        if (dec.Action == CopyAction.CancelAll) { Cancel(); return CopyAction.CancelAll; }
        return CopyAction.Skip;
    }
}