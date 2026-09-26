using System;
using System.Collections.Generic;
using global::Android.App;
using global::Android.Content;
using global::Android.Content.PM;
using global::Android.Graphics;
using global::Android.OS;
using global::Android.Views;
using Avalonia.Android;

namespace QBasCopier.Android;

public static class DroidFile
{
    public static (string Name, long Size) Info(string uri)
    {
        var cr = MainActivity.Current?.ContentResolver;
        if (cr == null) return ("archivo.bin", 0);
        try
        {
            var u = global::Android.Net.Uri.Parse(uri);
            if (u == null) return ("archivo.bin", 0);
            string name = "archivo.bin";
            long size = 0;
            var c = cr.Query(u, null, null, null, null);
            if (c != null)
            {
                try
                {
                    if (c.MoveToFirst())
                    {
                        int ni = c.GetColumnIndex(global::Android.Provider.OpenableColumns.DisplayName);
                        if (ni >= 0) name = c.GetString(ni) ?? name;
                        int si = c.GetColumnIndex(global::Android.Provider.OpenableColumns.Size);
                        if (si >= 0 && !c.IsNull(si)) size = c.GetLong(si);
                    }
                }
                finally { c.Close(); }
            }
            return (name, size);
        }
        catch { return ("archivo.bin", 0); }
    }

    public static Stream Open(string uri)
    {
        var cr = MainActivity.Current?.ContentResolver;
        if (cr == null) throw new IOException("ContentResolver no disponible");
        try
        {
            var u = global::Android.Net.Uri.Parse(uri);
            if (u == null) throw new IOException("URI invalida: " + uri);
            var s = cr.OpenInputStream(u);
            // Antes se devolvia Stream.Null: la copia terminaba "bien" con 0 bytes copiados.
            if (s == null) throw new IOException("No se pudo abrir: " + uri);
            return s;
        }
        catch (IOException) { throw; }
        catch (Exception ex) { throw new IOException("No se pudo abrir: " + uri, ex); }
    }

    /// <summary>Nombre visible del documento. Nunca devuelve la docId codificada.</summary>
    public static string Name(string uri) => Info(uri).Name;
}

public static class DroidDir
{
    const string MimeDir = "vnd.android.document/directory";
    const string MimeBin = "application/octet-stream";

    /// <summary>
    /// Crea un archivo NUEVO dentro de la carpeta SAF con el primer nombre libre
    /// ("foto.jpg", "foto (1).jpg"...). Se usa al recibir por transferencia: nunca se
    /// debe pisar un archivo que ya estaba.
    /// </summary>
    public static Stream? OpenForWrite(string dirUri, string fileName)
    {
        try
        {
            var cr = MainActivity.Current?.ContentResolver;
            if (cr == null) return null;
            var pu = global::Android.Net.Uri.Parse(dirUri);
            if (pu == null) return null;
            var free = NextFreeName(dirUri, fileName);
            var doc = global::Android.Provider.DocumentsContract.CreateDocument(cr, pu, MimeBin, free);
            if (doc == null) return null;
            return cr.OpenOutputStream(doc, "wt");
        }
        catch { return null; }
    }

    /// <summary>
    /// Abre un archivo dentro de una carpeta SAF para escritura, sobrescribiendo si ya existe.
    /// DocumentsContract.CreateDocument SIEMPRE crea uno nuevo (y el explorador le anade
    /// " (1)" al nombre), asi que primero se busca el documento con ese nombre.
    /// </summary>
    public static Stream? OpenForWriteIn(string dirUri, string fileName)
    {
        try
        {
            var cr = MainActivity.Current?.ContentResolver;
            if (cr == null) return null;
            var pu = global::Android.Net.Uri.Parse(dirUri);
            if (pu == null) return null;
            var exist = Find(pu, fileName, null);
            // "wt" = escribe y trunca: sobrescribe el archivo entero.
            if (exist != null) return cr.OpenOutputStream(exist, "wt");
            var doc = global::Android.Provider.DocumentsContract.CreateDocument(cr, pu, MimeBin, fileName);
            if (doc == null) return null;
            return cr.OpenOutputStream(doc, "wt");
        }
        catch { return null; }
    }

    /// <summary>
    /// Busca un hijo directo por nombre. mime == null = cualquiera, si no exige ese tipo.
    /// Devuelve una URI que CONSERVA el segmento /tree/ (BuildDocumentUri lo perderia y la
    /// carpeta dejaria de poder navegarse).
    /// </summary>
    static global::Android.Net.Uri? Find(global::Android.Net.Uri parent, string name, string? mime)
    {
        var cr = MainActivity.Current?.ContentResolver;
        if (cr == null) return null;
        var treeId = global::Android.Provider.DocumentsContract.GetTreeDocumentId(parent);
        if (treeId == null) return null;
        var kids = global::Android.Provider.DocumentsContract.BuildChildDocumentsUriUsingTree(parent, treeId);
        using var c = cr.Query(kids, null, null, null, null);
        if (c == null) return null;
        int iN = c.GetColumnIndex(global::Android.Provider.DocumentsContract.Document.ColumnDisplayName);
        int iM = c.GetColumnIndex(global::Android.Provider.DocumentsContract.Document.ColumnMimeType);
        int iI = c.GetColumnIndex(global::Android.Provider.DocumentsContract.Document.ColumnDocumentId);
        if (iN < 0 || iI < 0 || !c.MoveToFirst()) return null;
        do
        {
            if (c.GetString(iN) != name) continue;
            if (mime != null && c.GetString(iM) != mime) continue;
            return global::Android.Provider.DocumentsContract
                .BuildDocumentUriUsingTree(parent, c.GetString(iI) ?? "");
        } while (c.MoveToNext());
        return null;
    }

    /// <summary>Carpeta hija existente, o null. No crea nada.</summary>
    public static string? FindDir(string parentDirUri, string name)
    {
        try
        {
            var pu = global::Android.Net.Uri.Parse(parentDirUri);
            if (pu == null) return null;
            return Find(pu, name, MimeDir)?.ToString();
        }
        catch { return null; }
    }

    /// <summary>Busca la carpeta hija y la crea si falta. SAF no tiene CreateDirectory.</summary>
    public static string? EnsureDir(string parentDirUri, string name)
    {
        var found = FindDir(parentDirUri, name);
        if (found != null) return found;
        try
        {
            var cr = MainActivity.Current?.ContentResolver;
            if (cr == null) return null;
            var pu = global::Android.Net.Uri.Parse(parentDirUri);
            if (pu == null) return null;
            var doc = global::Android.Provider.DocumentsContract.CreateDocument(cr, pu, MimeDir, name);
            return doc?.ToString();
        }
        catch { return null; }
    }

    /// <summary>
    /// Crea (o reutiliza) la cadena de carpetas <paramref name="rel"/> dentro de dirUri y
    /// devuelve la URI de la carpeta final. "a/b" -> crea "a" y dentro "b".
    /// </summary>
    public static string? EnsurePath(string dirUri, string rel)
    {
        var cur = dirUri;
        foreach (var seg in rel.Replace('\\', '/').Split('/', StringSplitOptions.RemoveEmptyEntries))
        {
            var next = EnsureDir(cur, seg);
            if (next == null) return null;
            cur = next;
        }
        return cur;
    }

    /// <summary>Existe el archivo (no carpeta) indicado por rel dentro de dirUri.</summary>
    public static bool FileExistsIn(string dirUri, string rel)
    {
        var i = rel.Replace('\\', '/').LastIndexOf('/');
        var dir = i < 0 ? dirUri : EnsurePath(dirUri, rel.Substring(0, i)) ?? dirUri;
        var name = i < 0 ? rel : rel.Substring(i + 1);
        try
        {
            var pu = global::Android.Net.Uri.Parse(dir);
            if (pu == null) return false;
            return Find(pu, name, null) != null;
        }
        catch { return false; }
    }

    /// <summary>Primer nombre libre "archivo (n).ext" dentro de la carpeta SAF.</summary>
    public static string NextFreeName(string dirUri, string rel)
    {
        if (!FileExistsIn(dirUri, rel)) return rel;
        var i = rel.Replace('\\', '/').LastIndexOf('/');
        var name = i < 0 ? rel : rel.Substring(i + 1);
        var dot = name.LastIndexOf('.');
        var stem = dot > 0 ? name.Substring(0, dot) : name;
        var ext = dot > 0 ? name.Substring(dot) : "";
        for (int n = 1; n < 10000; n++)
        {
            var cand = $"{stem} ({n}){ext}";
            var full = i < 0 ? cand : rel.Substring(0, i + 1) + cand;
            if (!FileExistsIn(dirUri, full)) return full;
        }
        return rel;
    }

    /// <summary>
    /// Borra un documento SAF. Necesario para "mover" desde el explorador.
    /// DocumentsContract.RemoveDocument exige tambien el padre, que se deduce de la docId
    /// ("primary:Download/foto.jpg" -> "primary:Download").
    /// </summary>
    public static bool Delete(string uri)
    {
        try
        {
            var cr = MainActivity.Current?.ContentResolver;
            if (cr == null) return false;
            var u = global::Android.Net.Uri.Parse(uri);
            if (u == null) return false;
            try { if (cr.Delete(u, null, null) > 0) return true; } catch { }
            var id = global::Android.Provider.DocumentsContract.GetDocumentId(u);
            if (string.IsNullOrEmpty(id)) return false;
            int i = id.LastIndexOf('/');
            if (i <= 0) return false;
            var parent = global::Android.Provider.DocumentsContract.BuildDocumentUri(
                u.Authority ?? "", id[..i]);
            return global::Android.Provider.DocumentsContract.RemoveDocument(cr, parent, u);
        }
        catch { return false; }
    }

    public static bool Exists(string uri)
    {
        try
        {
            var cr = MainActivity.Current?.ContentResolver;
            if (cr == null) return false;
            var u = global::Android.Net.Uri.Parse(uri);
            if (u == null) return false;
            using var c = cr.Query(u, null, null, null, null);
            if (c == null) return false;
            return c.MoveToFirst();
        }
        catch { return false; }
    }
}

public static class DroidList
{
    public static string Home()
    {
        try
        {
            var d = MainActivity.Current?.GetExternalFilesDir(null);
            if (d != null) return d.AbsolutePath;
            return Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        }
        catch { return Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData); }
    }

    public static string Root(string treeUri)
    {
        try
        {
            var cr = MainActivity.Current?.ContentResolver;
            if (cr == null) return treeUri;
            var tu = global::Android.Net.Uri.Parse(treeUri);
            if (tu == null) return treeUri;
            var treeDoc = global::Android.Provider.DocumentsContract.GetTreeDocumentId(tu);
            return global::Android.Provider.DocumentsContract.BuildDocumentUriUsingTree(tu, treeDoc).ToString();
        }
        catch { return treeUri; }
    }

    // parentUri: un content:// documento; devuelve hijos (documento uri, nombre, esCarpeta, tamaño)
    public static List<(string Uri, string Name, bool IsDir, long Size)> Children(string parentUri)
    {
        var res = new List<(string, string, bool, long)>();
        try
        {
            var cr = MainActivity.Current?.ContentResolver;
            if (cr == null) return res;
            var pu = global::Android.Net.Uri.Parse(parentUri);
            if (pu == null) return res;
            var treeId = global::Android.Provider.DocumentsContract.GetTreeDocumentId(pu);
            if (treeId == null) return res;
            var kidsUri = global::Android.Provider.DocumentsContract.BuildChildDocumentsUriUsingTree(pu, treeId);
            using var c = cr.Query(kidsUri, null, null, null, null);
            if (c == null) return res;
            while (c.MoveToNext())
            {
                int ciName = c.GetColumnIndex(global::Android.Provider.DocumentsContract.Document.ColumnDisplayName);
                string name = ciName >= 0 ? c.GetString(ciName) ?? "" : "";
                int ciMime = c.GetColumnIndex(global::Android.Provider.DocumentsContract.Document.ColumnMimeType);
                string mime = ciMime >= 0 ? c.GetString(ciMime) ?? "" : "";
                int ciId = c.GetColumnIndex(global::Android.Provider.DocumentsContract.Document.ColumnDocumentId);
                string docId = ciId >= 0 ? c.GetString(ciId) ?? "" : "";
                if (name.Length == 0 || docId.Length == 0) continue;
                long size = 0;
                int si = c.GetColumnIndex(global::Android.Provider.DocumentsContract.Document.ColumnSize);
                if (si >= 0 && !c.IsNull(si)) size = c.GetLong(si);
                bool isDir = mime == global::Android.Provider.DocumentsContract.Document.MimeTypeDir;
                // BuildDocumentUriUsingTree, NO BuildDocumentUri: este ultimo genera
                // content://auth/document/xx, sin el segmento /tree/, y GetTreeDocumentId
                // devuelve null -> al entrar en la subcarpeta el listado salia vacio.
                var childUri = global::Android.Provider.DocumentsContract.BuildDocumentUriUsingTree(pu, docId);
                res.Add((childUri.ToString(), name, isDir, size));
            }
        }
        catch { }
        return res;
    }

    public static Stream Open(string uri)
    {
        var cr = MainActivity.Current?.ContentResolver;
        if (cr == null) throw new IOException("ContentResolver no disponible");
        try
        {
            var u = global::Android.Net.Uri.Parse(uri);
            if (u == null) throw new IOException("URI invalida: " + uri);
            var s = cr.OpenInputStream(u);
            if (s == null) throw new IOException("No se pudo abrir: " + uri);
            return s;
        }
        catch (IOException) { throw; }
        catch (Exception ex) { throw new IOException("No se pudo abrir: " + uri, ex); }
    }

    public static void View(string uri)
    {
        try
        {
            var ctx = MainActivity.Current;
            if (ctx == null) return;
            var u = global::Android.Net.Uri.Parse(uri);
            if (u == null) return;
            var i = new Intent(Intent.ActionView);
            i.SetDataAndType(u, "*/*");
            i.AddFlags(ActivityFlags.GrantReadUriPermission);
            ctx.StartActivity(i);
        }
        catch { }
    }
}

public static class DroidPub
{
    public const string Folder = "QBasRecibidos";

    /// <summary>
    /// Copia el archivo recibido a Descargas/QBasRecibidos para que sea visible en el
    /// explorador de archivos y en la galeria. Antes solo funcionaba con rutas del
    /// sistema de archivos y solo en Android 10+, asi que en practica no hacia nada.
    /// src puede ser una ruta normal o una content:// de SAF.
    /// </summary>
    public static bool Publish(string src)
    {
        var cr = MainActivity.Current?.ContentResolver;
        if (cr == null) return false;
        var name = SafeName(src);
        if (name.Length == 0) return false;
        var isSaf = src.StartsWith("content://", StringComparison.OrdinalIgnoreCase);
        bool q = global::Android.OS.Build.VERSION.SdkInt >= global::Android.OS.BuildVersionCodes.Q;

        try
        {
            global::Android.Net.Uri? pending = null;
            if (q)
            {
                var col = new ContentValues();
                col.Put(global::Android.Provider.MediaStore.MediaColumns.DisplayName, name);
                col.Put(global::Android.Provider.MediaStore.MediaColumns.MimeType, MimeOf(name));
                col.Put(global::Android.Provider.MediaStore.MediaColumns.RelativePath,
                        global::Android.OS.Environment.DirectoryDownloads + "/" + Folder);
                col.Put("is_pending", 1);
                pending = cr.Insert(global::Android.Provider.MediaStore.Downloads.ExternalContentUri, col);
                if (pending == null) return false;
            }
            else
            {
                var dir = global::Android.OS.Environment.GetExternalStoragePublicDirectory(
                              global::Android.OS.Environment.DirectoryDownloads) + "/" + Folder;
                Directory.CreateDirectory(dir);
                var full = Path.Combine(dir, name);
                if (File.Exists(full)) File.Delete(full);
                using (var os = new FileStream(full, FileMode.CreateNew, FileAccess.Write))
                using (var ins = OpenSource(src, isSaf)) ins.CopyTo(os);
                return true;
            }

            try
            {
                using (var os = cr.OpenOutputStream(pending!))
                using (var ins = OpenSource(src, isSaf))
                    ins.CopyTo(os);

                // Hay que quitar Is_PENDING o el archivo queda invisible para siempre.
                var done = new ContentValues();
                done.Put("is_pending", 0);
                cr.Update(pending!, done, null, null);
                return true;
            }
            catch
            {
                try { cr.Delete(pending!, null, null); } catch { }
                throw;
            }
        }
        catch (Exception)
        {
            DroidCtx.Toast("No se pudo guardar en Descargas");
            return false;
        }
    }

    static Stream OpenSource(string src, bool isSaf)
    {
        if (isSaf) return DroidList.Open(src);
        return new FileStream(src, FileMode.Open, FileAccess.Read, FileShare.Read);
    }

    /// <summary>Nombre visible del origen, sea ruta o content://.</summary>
    public static string SafeName(string src)
    {
        try
        {
            if (!src.StartsWith("content://", StringComparison.OrdinalIgnoreCase))
            {
                var n = Path.GetFileName(src.TrimEnd('/', '\\'));
                return Clean(n);
            }
            return Clean(DroidFile.Name(src));
        }
        catch { return "recibido.bin"; }
    }

    static string Clean(string n)
    {
        if (string.IsNullOrWhiteSpace(n)) return "recibido.bin";
        foreach (var bad in new[] { '/', '\\', ':', '*', '?', '"', '<', '>', '|', '\0' })
            n = n.Replace(bad, '_');
        return n.Length > 120 ? n[..120] : n;
    }

    static string MimeOf(string name)
    {
        var e = Path.GetExtension(name).ToLowerInvariant();
        return e switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            ".mp4" => "video/mp4",
            ".mkv" => "video/x-matroska",
            ".mp3" => "audio/mpeg",
            ".wav" => "audio/wav",
            ".pdf" => "application/pdf",
            ".txt" => "text/plain",
            ".zip" => "application/zip",
            _ => "application/octet-stream"
        };
    }
}

public static class DroidCtx
{
    public static void Toast(string msg)
    {
        try
        {
            global::Android.Widget.Toast.MakeText(global::Android.App.Application.Context, msg, global::Android.Widget.ToastLength.Short)?.Show();
        }
        catch { }
    }
}

[Activity(Label = "QBasCopier&Transfer", MainLauncher = true, Theme = "@style/MyTheme",
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.ScreenLayout | ConfigChanges.Density)]
public class MainActivity : AvaloniaMainActivity<App>
{
    public static MainActivity? Current;
    private Action<string[]>? _pickerCb;
    private static readonly int ReqPick = 1001;
    private static readonly int ReqCam = 1002;
    private static readonly int ReqTree = 1003;
    private static readonly int ReqCamPerm = 1004;

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        // AvaloniaMainActivity deriva de AppCompatActivity: hay que fijar el tema ANTES de
        // base.OnCreate, porque AppCompat valida windowActionBar en onPostCreate y sin esto
        // revienta con "You need to use a Theme.AppCompat theme (or descendant) with this activity".
        try { SetTheme(Resource.Style.MyTheme); } catch { }

        Current = this;
        QBasCopier.TransferHost.OpenDoc = DroidList.Open;
        QBasCopier.TransferHost.DocInfo = DroidFile.Info;
        QBasCopier.TransferHost.ListDoc = DroidList.Children;
        QBasCopier.TransferHost.WriteDoc = DroidDir.OpenForWrite;
        base.OnCreate(savedInstanceState);
    }

    protected override void OnDestroy()
    {
        if (Current == this) Current = null;
        ReleaseLocks();
        base.OnDestroy();
    }

    PowerManager? _pm;
    PowerManager.WakeLock? _wake;

    /// <summary>
    /// Mantiene la CPU viva mientras hay una copia o una transferencia.
    /// Sin esto, con la pantalla apagada Android congela la app a mitad de un archivo
    /// de 2 GB y la copia se queda parada sin error visible.
    /// </summary>
    public void KeepAwake()
    {
        try
        {
            _pm ??= (PowerManager?)GetSystemService(PowerService);
            if (_pm == null) return;
            if (_wake == null)
            {
                // 1 = PARTIAL_WAKE_LOCK. Se usa el literal porque el nombre simbolico
                // no existe en el binding de .NET para Android.
                _wake = _pm.NewWakeLock(1, "QBasCopier:transfer");
                _wake.SetReferenceCounted(false);
            }
            if (!_wake.IsHeld) _wake.Acquire(6L * 60 * 60 * 1000);   // 6 h
        }
        catch { }
    }

    public void ReleaseLocks()
    {
        try { if (_wake != null && _wake.IsHeld) _wake.Release(); } catch { }
    }

    public void PickFiles(Action<string[]> done)
    {
        _pickerCb = done;
        RunOnUiThread(() =>
        {
            try
            {
                var i = new Intent(Intent.ActionOpenDocument);
                i.AddCategory(Intent.CategoryOpenable);
                i.PutExtra(Intent.ExtraAllowMultiple, true);
                i.SetType("*/*");
                StartActivityForResult(i, ReqPick);
            }
            catch { _pickerCb?.Invoke(Array.Empty<string>()); }
        });
    }

    /// <summary>Lo registra la UI para decidir si el boton atras se consume o no.</summary>
    public static Func<bool>? BackHandler;

    /// <summary>Cierre real de la app. MainWindow es un UserControl y no tiene Window que cerrar.</summary>
    public void FinishApp()
    {
        try { Settings.Flush(); } catch { }
        try { QBasCopier.TransferHost.Key = ""; } catch { }
        RunOnUiThread(() =>
        {
            try { ReleaseLocks(); } catch { }
            try { Finish(); } catch { }
            try { global::Android.OS.Process.KillProcess(global::Android.OS.Process.MyPid()); } catch { }
        });
    }

    public override void OnBackPressed()
    {
        try
        {
            var h = BackHandler;
            if (h != null && h()) return;
        }
        catch { }
        base.OnBackPressed();
    }

    public Action<string>? TreeCb;

    public void PickTree(Action<string> done)
    {
        TreeCb = done;
        RunOnUiThread(() =>
        {
            try { StartActivityForResult(new Intent(Intent.ActionOpenDocumentTree), ReqTree); }
            catch { TreeCb?.Invoke(""); }
        });
    }

    protected override void OnActivityResult(int requestCode, Result resultCode, Intent? data)
    {
        base.OnActivityResult(requestCode, resultCode, data);
        if (requestCode == ReqTree && resultCode == Result.Ok && data?.Data != null)
        {
            var uri = data.Data!;
            try
            {
                ContentResolver!.TakePersistableUriPermission(uri,
                    ActivityFlags.GrantReadUriPermission | ActivityFlags.GrantWriteUriPermission);
            }
            catch { }
            var cb = TreeCb;
            TreeCb = null;
            cb?.Invoke(uri.ToString());
            return;
        }
        if (requestCode == ReqPick && resultCode == Result.Ok && _pickerCb != null)
        {
            var uris = new List<string>();
            var clips = data?.ClipData;
            if (clips != null)
                for (int i = 0; i < clips.ItemCount; i++)
                    if (clips.GetItemAt(i).Uri != null)
                        uris.Add(clips.GetItemAt(i).Uri.ToString());
            else if (data?.Data != null)
                uris.Add(data.Data.ToString());
            foreach (var u in uris)
                try
                {
                    ContentResolver!.TakePersistableUriPermission(global::Android.Net.Uri.Parse(u), ActivityFlags.GrantReadUriPermission);
                }
                catch { }
            _pickerCb(uris.ToArray());
        }
        else if (requestCode == ReqCam && resultCode == Result.Ok && data != null)
        {
            var txt = data.GetStringExtra("qb_result") ?? "";
            ScanActivity.Deliver(txt);
        }
    }

    public void OpenHotspotSettings()
    {
        RunOnUiThread(() =>
        {
            try
            {
                var ctx = this;
                if (Build.VERSION.SdkInt >= BuildVersionCodes.Q)
                {
                    var i = new Intent("android.settings.WIFI_TETHERING_SETTINGS");
                    StartActivity(i);
                }
                else
                {
                    StartActivity(new Intent(global::Android.Provider.Settings.ActionWirelessSettings));
                }
            }
            catch { }
        });
    }

    public void StartOwnNetwork(string ssid, string key, Action<bool, string> done)
    {
        RunOnUiThread(() =>
        {
            try
            {
                var i = new Intent(global::Android.Provider.Settings.ActionWirelessSettings);
                i.AddFlags(global::Android.Content.ActivityFlags.NewTask);
                StartActivity(i);
                done(true, "");
            }
            catch (Exception ex) { done(false, ex.Message ?? "no se pudieron abrir los ajustes"); }
        });
    }

    public void StartScan()
    {
        if (Build.VERSION.SdkInt < BuildVersionCodes.M ||
            CheckSelfPermission(global::Android.Manifest.Permission.Camera) == Permission.Granted)
        {
            ScanActivity.Launch(this);
            return;
        }
        RequestPermissions(new[] { global::Android.Manifest.Permission.Camera }, ReqCamPerm);
    }

    public override void OnRequestPermissionsResult(int requestCode, string?[]? permissions, Permission[]? grantResults)
    {
        base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        if (requestCode == ReqCamPerm && grantResults is { Length: > 0 } && grantResults[0] == Permission.Granted)
            ScanActivity.Launch(this);
    }
}

[Activity(Theme = "@style/MyTheme",
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.ScreenLayout | ConfigChanges.Density)]
public class ScanActivity : Activity, TextureView.ISurfaceTextureListener, global::Android.Hardware.Camera.IPreviewCallback
{
    private global::Android.Hardware.Camera? _cam;
    private TextureView? _view;
    private bool _done;

    public static void Launch(Activity from)
    {
        try
        {
            from.StartActivityForResult(new Intent(from, typeof(ScanActivity)), 1002);
        }
        catch { }
    }

    public static void Deliver(string text)
    {
        var cb = Callback;
        Callback = null;
        cb?.Invoke(text);
    }

    public static Action<string>? Callback { get; set; }

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        _view = new TextureView(this);
        _view.SurfaceTextureListener = this;
        SetContentView(_view);
    }

    public void OnSurfaceTextureAvailable(SurfaceTexture surface, int width, int height)
    {
        try
        {
            _cam = global::Android.Hardware.Camera.Open();
            _cam.SetPreviewTexture(surface);
            var p = _cam.GetParameters();
            var sizes = p.SupportedPreviewSizes;
            if (sizes.Count > 0) p.SetPreviewSize(sizes[sizes.Count - 1].Width, sizes[sizes.Count - 1].Height);
            _cam.SetParameters(p);
            _cam.SetPreviewCallback(this);
            _cam.StartPreview();
        }
        catch { Finish(); }
    }

    public bool OnSurfaceTextureDestroyed(SurfaceTexture surface)
    {
        Close();
        return true;
    }

    public void OnSurfaceTextureSizeChanged(SurfaceTexture surface, int width, int height) { }
    public void OnSurfaceTextureUpdated(SurfaceTexture surface) { }

    private void Close()
    {
        try { if (_cam != null) { _cam.StopPreview(); _cam.Release(); _cam = null; } } catch { }
    }

    protected override void OnDestroy()
    {
        Close();
        base.OnDestroy();
    }

    public void OnPreviewFrame(byte[] data, global::Android.Hardware.Camera camera)
    {
        if (_done || data == null || data.Length == 0) return;
        var p = camera.GetParameters();
        var w = p.PreviewSize.Width;
        var h = p.PreviewSize.Height;
        var txt = Decode(data, w, h);
        if (txt == null) return;
        _done = true;
        RunOnUiThread(() =>
        {
            Close();
            var i = new Intent();
            i.PutExtra("qb_result", txt);
            SetResult(Result.Ok, i);
            Finish();
        });
    }

    private static string? Decode(byte[] nv21, int w, int h)
    {
        try
        {
            var src = new ZXing.PlanarYUVLuminanceSource(nv21, w, h, 0, 0, w, h, false);
            var bmp = new ZXing.BinaryBitmap(new ZXing.Common.HybridBinarizer(src));
            var reader = new ZXing.MultiFormatReader();
            var res = reader.decodeWithState(bmp);
            return res?.Text;
        }
        catch { return null; }
    }
}