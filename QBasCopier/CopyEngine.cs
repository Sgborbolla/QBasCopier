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

    /// <summary>Carpeta destino: ruta del sistema de archivos o URI de carpeta SAF.</summary>
    public string DestDir { get; set; } = "";

    /// <summary>Ruta del elemento dentro de DestDir, incluyendo su propio nombre.</summary>
    public string Rel { get; set; } = "";

    public bool IsDirectory { get; set; }
    public bool IsContent { get; set; }   // true cuando SourcePath es content:// (Android/SAF)

    /// <summary>El destino es una carpeta SAF: no existe en el sistema de archivos.</summary>
    public bool IsSafDest =>
        DestDir.StartsWith("content://", StringComparison.OrdinalIgnoreCase);

    /// <summary>Solo para mostrar. Un destino SAF es (carpeta + ruta relativa), no una ruta.</summary>
    public string DestPath =>
        IsSafDest ? DestDir.TrimEnd('/') + "/" + Rel : Path.Combine(DestDir, Rel);

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
    public string ConflictInfo { get => _conflictInfo; set { _conflictInfo = value; On(nameof(ConflictInfo)); } }

    /// <summary>Reintentos ya gastados, para no reintentar un error de forma infinita.</summary>
    public int Retries { get; set; }

    /// <summary>Reavisa todos los datos visibles: lo llama la UI al renombrar o recargar.</summary>
    public void RaiseAll()
    {
        On(nameof(Name));
        On(nameof(SizeText));
        On(nameof(SourcePath));
        On(nameof(DestPath));
        On(nameof(DestName));
        On(nameof(Percent));
        On(nameof(StateText));
        On(nameof(SpeedText));
        On(nameof(IsDirectory));
    }

    /// <summary>
    /// Nombre real del origen. Para content:// hay que preguntarlo al ContentResolver:
    /// Path.GetFileName devolveria la docId codificada ("primary%3ADownloads%2Ffoto.jpg").
    /// </summary>
    public string Name
    {
        get
        {
#if ANDROID
            if (IsContent)
            {
                try { return QBasCopier.Android.DroidPub.SafeName(SourcePath); } catch { }
            }
#endif
            return Path.GetFileName(SourcePath.TrimEnd('\\', '/'));
        }
    }

    public string SizeText => FilePane.Human(TotalBytes);

    public string DestName
    {
        get
        {
            var r = Rel.Replace('\\', '/');
            var i = r.LastIndexOf('/');
            return i < 0 ? r : r[(i + 1)..];
        }
    }

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

    /// <summary>
    /// Progreso de toda la cola, no de un elemento. Es lo que ve el usuario en la
    /// barra global y en el tooltip de la bandeja cuando solo hay una ventana.
    /// Se lee con Interlocked porque los hilos workers van sumando mientras se lee.
    /// </summary>
    public double OverallPercent
    {
        get
        {
            var t = TotalBytes;
            return t > 0 ? Math.Min(100, DoneBytes * 100.0 / t) : 0;
        }
    }

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

    private const int MaxRetries = 5;

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

        if (DestExists(it))
        {
            var cd = await ResolveCollisionAsync(it, ct);
            if (cd.Action == CopyAction.CancelAll) return CopyAction.CancelAll;
            if (cd.Action == CopyAction.Skip) return Mark(it, ItemState.Skipped, L.Get("stateSkipped"));
            if (cd.Action == CopyAction.Rename) RenameDest(it);
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
            if (S.VerifyChecksum && !it.IsDirectory) VerifyIntegrity(it);

            // "Mover" tambien borra el origen cuando viene del explorador SAF.
            if (Move) DeleteSource(it);

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
        size = SourceLength(it);
        return size >= 0;
    }

    private bool SameLengthAndNewer(CopyItem it)
    {
        // En SAF no se puede comparar sin abrir el documento: se asume diferente y se
        // sobrescribe, que es el comportamiento esperado de "sobrescribir si es distinto".
        if (it.IsSafDest) return false;
        try
        {
            var a = new FileInfo(it.SourcePath); var b = new FileInfo(it.DestPath);
            return a.Length == b.Length;
        }
        catch { return false; }
    }

    private void RenameDest(CopyItem it)
    {
#if ANDROID
        if (it.IsSafDest)
        {
            it.Rel = QBasCopier.Android.DroidDir.NextFreeName(it.DestDir, it.Rel);
            return;
        }
#endif
        it.Rel = NextFreeRel(it.DestPath);
    }

    private void DeleteSource(CopyItem it)
    {
        try
        {
#if ANDROID
            if (it.IsContent) { QBasCopier.Android.DroidDir.Delete(it.SourcePath); return; }
#endif
            if (it.IsDirectory) Directory.Delete(it.SourcePath, true);
            else File.Delete(it.SourcePath);
        }
        catch { }
    }

    /// <summary>
    /// Primer nombre libre dentro de la carpeta de "path". Devuelve solo el nombre
    /// nuevo, no la ruta completa, porque se guarda en CopyItem.Rel.
    /// </summary>
    private string NextFreeRel(string path)
    {
        var dir = Path.GetDirectoryName(path) ?? "";
        var stem = Path.GetFileNameWithoutExtension(path);
        var ext = Path.GetExtension(path);

        if (!string.IsNullOrEmpty(S.RenameNewPattern) && S.RenameNewPattern.Contains("%NAME%"))
        {
            for (int n = 1; n < 1000; n++)
            {
                var fn = S.RenameNewPattern
                    .Replace("%NAME%", stem)
                    .Replace("%EXT%", ext.TrimStart('.'))
                    .Replace("%COPY%", n.ToString());
                if (fn.Length == 0) break;
                // El patron es configurable: si trae separadores se descarta, o el
                // archivo se escribiria fuera de la carpeta de destino.
                if (fn.Contains('/') || fn.Contains('\\')) continue;
                var cand = Path.Combine(dir, fn);
                if (!File.Exists(cand) && !Directory.Exists(cand)) return fn;
            }
        }

        for (int n = 1; n < 100000; n++)
        {
            string fn = $"{stem} ({n}){ext}";
            var cand = Path.Combine(dir, fn);
            if (!File.Exists(cand) && !Directory.Exists(cand)) return fn;
        }
        return Path.GetFileName(path);
    }

    // ------------- native + buffer engines -------------
    private async Task CopyFileWithEngineAsync(CopyItem it, bool resumeOffset, CancellationToken ct)
    {
#if ANDROID
        // En Android tanto el origen como el destino pueden ser SAF: se copia por Stream.
        if (it.IsContent || it.IsSafDest) { await BufferedCopyAsync(it, resumeOffset, ct); return; }
#endif
        bool useNative = S.Engine != "buffer" && !resumeOffset && OperatingSystem.IsWindows();
        if (useNative) await NativeCopyAsync(it, ct);
        else await BufferedCopyAsync(it, resumeOffset, ct);
    }

    /// <summary>Abre el origen como Stream (archivo normal o documento SAF).</summary>
    private Stream OpenSource(CopyItem it)
    {
#if ANDROID
        if (it.IsContent) return QBasCopier.Android.DroidList.Open(it.SourcePath);
#endif
        int buf = (int)Math.Clamp(S.BufferBytes, 64 * 1024, 64 * 1024 * 1024);
        return new FileStream(it.SourcePath, FileMode.Open, FileAccess.Read, FileShare.Read, buf, true);
    }

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

    /// <summary>
    /// Copia por Stream. Sirve para los tres casos: origen SAF, destino SAF o ambos
    /// (file:///storage/emulated/0 -> content://... en Android 11+, que antes fallaba).
    /// </summary>
    private async Task BufferedCopyAsync(CopyItem it, bool resume, CancellationToken ct)
    {
        long sourceLen = it.TotalBytes > 0 ? it.TotalBytes : SourceLength(it);
        if (sourceLen < 0) throw new IOException("No se pudo obtener el tamano: " + it.Name);
        it.TotalBytes = sourceLen;
        int buf = (int)Math.Clamp(S.BufferBytes, 64 * 1024, 64 * 1024 * 1024);

        long offset = 0;
        Stream? opened = null;
        try
        {
            if (resume && !it.IsSafDest && File.Exists(it.DestPath))
            {
                var ds = new FileInfo(it.DestPath);
                if (ds.Length < sourceLen)
                {
                    offset = ds.Length;
                    opened = new FileStream(it.DestPath, FileMode.Open, FileAccess.Write, FileShare.None, buf, true);
                    opened.Seek(offset, SeekOrigin.Begin);
                }
                else { CleanupPartial(it); throw new IOException("El destino es igual o mayor que el origen"); }
            }
        }
        catch when (opened == null) { CleanupPartial(it); throw; }

        Stream? dst = opened;
        try
        {
            if (dst == null)
            {
                dst = OpenDestWrite(it, FileMode.Create, buf);
                if (dst == null) throw new IOException("No se pudo escribir en: " + it.DestPath);
            }

            await using var src = OpenSource(it);
            if (offset > 0) src.Seek(offset, SeekOrigin.Begin);

            var buffer = new byte[buf];
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
            if (dst != null) await dst.DisposeAsync();
        }
    }

    private long SourceLength(CopyItem it)
    {
        try
        {
#if ANDROID
            if (it.IsContent) return QBasCopier.Android.DroidFile.Info(it.SourcePath).Size;
#endif
            if (it.IsDirectory)
                return new DirectoryInfo(it.SourcePath).EnumerateFiles("*", SearchOption.AllDirectories).Sum(f => f.Length);
            return new FileInfo(it.SourcePath).Length;
        }
        catch { return -1; }
    }

    private async Task CopyDirectoryAsync(CopyItem it, CancellationToken ct)
    {
        // En Android el destino puede ser una carpeta SAF (content://). No existe en el
        // sistema de archivos, asi que no se puede crear con Directory.CreateDirectory:
        // las carpetas se crean a traves de DocumentsContract al abrir cada hijo.
        if (!it.IsSafDest) Directory.CreateDirectory(it.DestPath);
        long acc = 0;
        foreach (var file in Directory.EnumerateFiles(it.SourcePath, "*", SearchOption.AllDirectories))
        {
            _gate.Wait(ct);
            ct.ThrowIfCancellationRequested();
            var rel = Path.GetRelativePath(it.SourcePath, file);
            var sub = new CopyItem
            {
                SourcePath = file,
                DestDir = it.DestDir,
                Rel = CombineRel(it.Rel, rel),
                TotalBytes = new FileInfo(file).Length
            };
            if (!sub.IsSafDest)
            {
                var pd = Path.GetDirectoryName(sub.DestPath);
                if (!string.IsNullOrEmpty(pd)) Directory.CreateDirectory(pd);
            }
            await CopyFileWithEngineAsync(sub, false, ct);
            acc += sub.TotalBytes;
            it.DoneBytes = acc;
            ItemStatus?.Invoke(it);
            TotalsChanged?.Invoke();
        }
    }

    /// <summary>Une la ruta interna de un elemento con la ruta de la carpeta que se copia.</summary>
    private static string CombineRel(string rel, string sub)
    {
        var r = rel.Replace('\\', '/').Trim('/');
        var v = sub.Replace('\\', '/');
        return r.Length == 0 ? v : r + "/" + v;
    }

    /// <summary>True si la ruta es un content:// de SAF (solo Android).</summary>
    public static bool IsSafPath(string p) =>
        p.StartsWith("content://", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Abre el destino para escribir. En Android el destino puede ser una carpeta SAF,
    /// y en Android 11+ escribir con FileStream en /storage/emulated/0 esta prohibido
    /// (scoped storage), asi que se delega en DocumentsContract creando las carpetas
    /// intermedias que falten.
    /// </summary>
    private Stream? OpenDestWrite(CopyItem it, FileMode mode, int buf)
    {
#if ANDROID
        if (it.IsSafDest)
        {
            var rel = it.Rel.Replace('\\', '/');
            var i = rel.LastIndexOf('/');
            var dirRel = i < 0 ? "" : rel.Substring(0, i);
            var name = i < 0 ? rel : rel.Substring(i + 1);
            if (name.Length == 0) return null;
            var dir = dirRel.Length == 0
                ? it.DestDir
                : QBasCopier.Android.DroidDir.EnsurePath(it.DestDir, dirRel);
            if (dir == null) return null;
            return QBasCopier.Android.DroidDir.OpenForWriteIn(dir, name);
        }
#endif
        var pd = Path.GetDirectoryName(it.DestPath);
        if (!string.IsNullOrEmpty(pd) && mode == FileMode.Create) Directory.CreateDirectory(pd);
        return new FileStream(it.DestPath, mode, FileAccess.Write, FileShare.None, buf, true);
    }

    /// <summary>Existe ya el destino (archivo o carpeta), sea ruta o SAF.</summary>
    private static bool DestExists(CopyItem it)
    {
#if ANDROID
        if (it.IsSafDest) return QBasCopier.Android.DroidDir.FileExistsIn(it.DestDir, it.Rel);
#endif
        return File.Exists(it.DestPath) || Directory.Exists(it.DestPath);
    }

    private void CopyMetadata(CopyItem it)
    {
        if (it.IsContent || it.IsSafDest) return;   // SAF no expone atributos de archivo
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

    /// <summary>
    /// SHA-256 del origen y del destino. Funciona tambien con SAF: antes solo miraba
    /// File.Exists, asi que copiar de /storage a una carpeta SAF con la casilla de
    /// verificacion marcada terminaba siempre en error "missing files".
    /// </summary>
    private void VerifyIntegrity(CopyItem it)
    {
        using var s = OpenSource(it);
        var h1 = SHA256.HashData(s);
        using var d = OpenDestRead(it);
        var h2 = SHA256.HashData(d);
        if (!h1.AsSpan().SequenceEqual(h2))
            throw new IOException("SHA-256 mismatch");
    }

    private Stream OpenDestRead(CopyItem it)
    {
#if ANDROID
        if (it.IsSafDest)
        {
            var rel = it.Rel.Replace('\\', '/');
            var i = rel.LastIndexOf('/');
            var dirRel = i < 0 ? "" : rel.Substring(0, i);
            var name = i < 0 ? rel : rel.Substring(i + 1);
            var dir = dirRel.Length == 0
                ? it.DestDir
                : QBasCopier.Android.DroidDir.EnsurePath(it.DestDir, dirRel);
            if (dir != null)
            {
                var kids = QBasCopier.Android.DroidList.Children(dir);
                foreach (var k in kids)
                    if (!k.IsDir && k.Name == name) return QBasCopier.Android.DroidList.Open(k.Uri);
            }
            throw new IOException("No se pudo reabrir el destino para verificar");
        }
#endif
        return new FileStream(it.DestPath, FileMode.Open, FileAccess.Read, FileShare.Read);
    }

    private bool DelPartial() => S.DeleteUnfinished;

    private void CleanupPartial(CopyItem it)
    {
        if (!DelPartial() || it.IsSafDest) return;   // en SAF se deja la parcial: borrarla
        try { if (File.Exists(it.DestPath)) File.Delete(it.DestPath); } catch { }  // da menos miedo
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
                // Sin tope el mismo error se reencolaba para siempre y el lote no terminaba.
                if (++it.Retries <= MaxRetries) return CopyAction.Retry;
                LogMessage?.Invoke($"{DateTime.Now:HH:mm:ss} | {it.SourcePath} | se agoto tras {MaxRetries} intentos");
                return CopyAction.Skip;
        }

        if (AskError == null) return CopyAction.Skip;
        var dec = await AskError(it, ex.Message);

        if (dec.Action == CopyAction.Retry) return CopyAction.Retry;
        if (dec.Action == CopyAction.CancelAll) { Cancel(); return CopyAction.CancelAll; }
        return CopyAction.Skip;
    }
}