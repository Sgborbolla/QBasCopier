using System;
using System.IO;

namespace QBasCopier;

public static class CrashLog
{
    private static readonly object Lock = new();
    private static string? _file;

    static CrashLog()
    {
        AppDomain.CurrentDomain.UnhandledException += (_, e) => Report("UNHANDLED " + (e.ExceptionObject?.ToString() ?? "?"));
        System.Threading.Tasks.TaskScheduler.UnobservedTaskException += (_, e) => Report("TASK " + e.Exception);
#if ANDROID
        global::Android.Runtime.AndroidEnvironment.UnhandledExceptionRaiser += (s, e) =>
        {
            var m = "JAVA " + e.Exception;
            Report(m);
        };
#endif
    }

    public static void Info(string msg) => Save("INFO " + msg);

    private static void Report(string msg)
    {
        Save(msg);
        ShowDialog(msg);
    }

    private static void ShowDialog(string msg)
    {
        try
        {
#if ANDROID
            global::Android.App.AlertDialog.Builder b = new(global::Android.App.Application.Context);
            b.SetTitle("QBasCopier - error");
            b.SetMessage(msg.Length > 6000 ? msg[..6000] : msg);
            b.SetPositiveButton("OK", (s, e) => { });
            b.Show();
#endif
        }
        catch { }
    }

    public static void Save(string msg)
    {
        try
        {
            lock (Lock)
            {
                var full = DateTime.Now.ToString("HH:mm:ss ") + msg + "\n";
                Console.Error.WriteLine("QBasCrash " + full);
#if ANDROID
                try
                {
                    var ctx = global::Android.App.Application.Context;
                    var cd = ctx?.GetExternalFilesDir(null) ?? ctx?.CacheDir;
                    if (cd != null)
                    {
                        var dir = new Java.IO.File(cd, "log");
                        if (!dir.Exists()) dir.Mkdirs();
                        _file ??= Path.Combine(dir.AbsolutePath, "crash.txt");
                        File.AppendAllText(_file, full);
                    }
                }
                catch { }
                try
                {
                    var cr = global::Android.App.Application.Context.ContentResolver!;
                    var col = new global::Android.Content.ContentValues();
                    col.Put(global::Android.Provider.MediaStore.MediaColumns.DisplayName, "QBasCopier-crash.txt");
                    col.Put(global::Android.Provider.MediaStore.MediaColumns.MimeType, "text/plain");
                    col.Put(global::Android.Provider.MediaStore.MediaColumns.RelativePath, "Download");
                    var uri = cr.Insert(global::Android.Provider.MediaStore.Downloads.ExternalContentUri, col);
                    if (uri != null)
                        using (var os = cr.OpenOutputStream(uri))
                        using (var w = new StreamWriter(os))
                            w.Write(full);
                }
                catch { }
#else
                try
                {
                    _file ??= Path.Combine(Path.GetTempPath(), "qbaswin-crash.log");
                    File.AppendAllText(_file, full);
                }
                catch { }
#endif
            }
        }
        catch { }
    }
}