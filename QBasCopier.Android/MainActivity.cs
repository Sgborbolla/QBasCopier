using System;
using System.Collections.Generic;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Graphics;
using Android.OS;
using Android.Views;
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
            var u = Android.Net.Uri.Parse(uri);
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
                        int ni = c.GetColumnIndex(Android.Provider.OpenableColumns.DisplayName);
                        if (ni >= 0) name = c.GetString(ni) ?? name;
                        int si = c.GetColumnIndex(Android.Provider.OpenableColumns.Size);
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
        if (cr == null) return Stream.Null;
        try
        {
            var u = Android.Net.Uri.Parse(uri);
            return u == null ? Stream.Null : (cr.OpenInputStream(u) ?? Stream.Null);
        }
        catch { return Stream.Null; }
    }
}

public static class DroidDir
{
    public static Stream? OpenForWrite(string treeUri, string fileName)
    {
        try
        {
            var cr = MainActivity.Current?.ContentResolver;
            if (cr == null) return null;
            var tu = Android.Net.Uri.Parse(treeUri);
            if (tu == null) return null;
            var doc = Android.Provider.DocumentsContract.CreateDocument(
                cr, tu, "application/octet-stream", fileName);
            if (doc == null) return null;
            return cr.OpenOutputStream(doc);
        }
        catch { return null; }
    }

    public static string SaveToTree(string treeUri, string fileName, Stream src)
    {
        try
        {
            var cr = MainActivity.Current?.ContentResolver;
            if (cr == null) return "";
            var tu = Android.Net.Uri.Parse(treeUri);
            if (tu == null) return "";
            var doc = Android.Provider.DocumentsContract.CreateDocument(
                cr, tu, "application/octet-stream", fileName);
            if (doc == null) return "";
            using var os = cr.OpenOutputStream(doc);
            src.CopyTo(os);
            return fileName;
        }
        catch { return ""; }
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
            var tu = Android.Net.Uri.Parse(treeUri);
            if (tu == null) return treeUri;
            var treeDoc = Android.Provider.DocumentsContract.GetTreeDocumentId(tu);
            return Android.Provider.DocumentsContract.BuildDocumentUriUsingTree(tu, treeDoc).ToString();
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
            var pu = Android.Net.Uri.Parse(parentUri);
            if (pu == null) return res;
            using var c = Android.Provider.DocumentsContract.QueryChildDocuments(cr, pu, null);
            if (c == null) return res;
            while (c.MoveToNext())
            {
                int ciName = c.GetColumnIndex(Android.Provider.DocumentsContract.Document.ColumnDisplayName);
                string name = ciName >= 0 ? c.GetString(ciName) ?? "" : "";
                int ciMime = c.GetColumnIndex(Android.Provider.DocumentsContract.Document.ColumnMimeType);
                string mime = ciMime >= 0 ? c.GetString(ciMime) ?? "" : "";
                int ciId = c.GetColumnIndex(Android.Provider.DocumentsContract.Document.ColumnDocumentId);
                string docId = ciId >= 0 ? c.GetString(ciId) ?? "" : "";
                if (name.Length == 0 || docId.Length == 0) continue;
                long size = 0;
                int si = c.GetColumnIndex(Android.Provider.DocumentsContract.Document.ColumnSize);
                if (si >= 0 && !c.IsNull(si)) size = c.GetLong(si);
                bool isDir = mime == Android.Provider.DocumentsContract.Document.MimeTypeDir;
                var childUri = Android.Provider.DocumentsContract.BuildDocumentUri(pu.Authority, docId);
                res.Add((childUri.ToString(), name, isDir, size));
            }
        }
        catch { }
        return res;
    }

    public static Stream Open(string uri)
    {
        var cr = MainActivity.Current?.ContentResolver;
        if (cr == null) return Stream.Null;
        try
        {
            var u = Android.Net.Uri.Parse(uri);
            return u == null ? Stream.Null : (cr.OpenInputStream(u) ?? Stream.Null);
        }
        catch { return Stream.Null; }
    }

    public static void View(string uri)
    {
        try
        {
            var ctx = MainActivity.Current;
            if (ctx == null) return;
            var u = Android.Net.Uri.Parse(uri);
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
    public static void Publish(string filePath)
    {
        try
        {
            var cr = MainActivity.Current?.ContentResolver;
            if (cr == null) return;
            var name = Path.GetFileName(filePath);
            var col = new ContentValues();
            col.Put(Android.Provider.MediaStore.MediaColumns.DisplayName, name);
            col.Put(Android.Provider.MediaStore.MediaColumns.MimeType, "application/octet-stream");
            col.Put(Android.Provider.MediaStore.MediaColumns.RelativePath, Environment.Download + "/QBasRecibidos");
            var uri = cr.Insert(Android.Provider.MediaStore.Downloads.ExternalContentUri, col);
            if (uri == null) return;
            using var os = cr.OpenOutputStream(uri);
            using var rd = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            rd.CopyTo(os);
        }
        catch { }
    }
}

public static class DroidCtx
{
    public static void Toast(string msg)
    {
        try
        {
            Android.Widget.Toast.MakeText(Android.App.Application.Context, msg, Android.Widget.ToastLength.Short)?.Show();
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
    private Action<string>? _treeCb;
    private static readonly int ReqPick = 1001;
    private static readonly int ReqCam = 1002;
    private static readonly int ReqTree = 1003;
    private static readonly int ReqCamPerm = 1004;

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        Current = this;
        QBasCopier.TransferHost.ExternalSink = DroidDir.SaveToTree;
        QBasCopier.TransferHost.OpenDoc = DroidList.Open;
        QBasCopier.TransferHost.DocInfo = DroidFile.Info;
        QBasCopier.TransferHost.ListDoc = DroidList.Children;
        QBasCopier.TransferHost.WriteDoc = DroidDir.OpenForWrite;
        base.OnCreate(savedInstanceState);
    }

    protected override void OnDestroy()
    {
        if (Current == this) Current = null;
        base.OnDestroy();
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
                    ContentResolver!.TakePersistableUriPermission(Android.Net.Uri.Parse(u), ActivityFlags.GrantReadUriPermission);
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
                    StartActivity(new Intent(Android.Provider.Settings.ActionWirelessSettings));
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
                var i = new Intent(Android.Provider.Settings.ActionWirelessSettings);
                i.AddFlags(Android.Content.ActivityFlags.NewTask);
                StartActivity(i);
                done(true, "");
            }
            catch (Exception ex) { done(false, ex.Message ?? "no se pudieron abrir los ajustes"); }
        });
    }

    public void StartScan()
    {
        if (Build.VERSION.SdkInt < BuildVersionCodes.M ||
            CheckSelfPermission(Android.Manifest.Permission.Camera) == Permission.Granted)
        {
            ScanActivity.Launch(this);
            return;
        }
        RequestPermissions(new[] { Android.Manifest.Permission.Camera }, ReqCamPerm);
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
public class ScanActivity : Activity, TextureView.ISurfaceTextureListener, Android.Hardware.Camera.IPreviewCallback
{
    private Android.Hardware.Camera? _cam;
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
            _cam = Android.Hardware.Camera.Open();
            _cam.SetPreviewTexture(surface);
            var p = _cam.Parameters;
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

    public void OnPreviewFrame(byte[] data, Android.Hardware.Camera camera)
    {
        if (_done || data == null || data.Length == 0) return;
        var p = camera.Parameters;
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