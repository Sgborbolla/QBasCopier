using System;
using System.IO;

namespace QBasCopier;

public static class CrashLog
{
    private static readonly object Lock = new();
    private static string? _file;

    static CrashLog()
    {
        AppDomain.CurrentDomain.UnhandledException += (_, e) => Save("UNHANDLED " + (e.ExceptionObject?.ToString() ?? "?"));
        System.Threading.Tasks.TaskScheduler.UnobservedTaskException += (_, e) => Save("TASK " + e.Exception);
#if ANDROID
        Android.Runtime.AndroidEnvironment.UnhandledExceptionRaiser += (_, e) => Save("JAVA " + e.Exception);
#endif
    }

    public static void Info(string msg) => Save("INFO " + msg);

    public static void Save(string msg)
    {
        try
        {
            lock (Lock)
            {
                var line = DateTime.Now.ToString("HH:mm:ss ") + msg + "\n";
                Console.Error.Write(line);
#if ANDROID
                try
                {
                    var ctx = Android.App.Application.Context;
                    var cd = ctx?.GetExternalFilesDir(null) ?? ctx?.CacheDir;
                    if (cd != null)
                    {
                        var dir = new Java.IO.File(cd, "log");
                        if (!dir.Exists()) dir.Mkdirs();
                        _file ??= Path.Combine(dir.AbsolutePath, "crash.txt");
                        File.AppendAllText(_file, line);
                    }
                }
                catch { }
                try
                {
                    var m = msg.Length > 160 ? msg[..160] : msg;
                    Android.Widget.Toast.MakeText(Android.App.Application.Context, m, Android.Widget.ToastLength.Long)?.Show();
                }
                catch { }
#else
                try
                {
                    _file ??= Path.Combine(Path.GetTempPath(), "qbaswin-crash.log");
                    File.AppendAllText(_file, line);
                }
                catch { }
#endif
            }
        }
        catch { }
    }
}