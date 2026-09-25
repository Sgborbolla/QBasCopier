using System.Diagnostics;
using System.Threading;

namespace QBasCopier;

public static class Program
{
    public static string[] StartupArgs = [];
    private static Mutex? _mutex;
    private static EventWaitHandle? _cmdEvent;

    #if !ANDROID
    [STAThread]
    public static void Main(string[] args)
    {
        StartupArgs = args;
        _mutex = new Mutex(true, "QBasCopier_v1_Mutex", out bool first);
        _cmdEvent = new EventWaitHandle(false, EventResetMode.AutoReset, "QBasCopier_v1_Cmd");
        if (!first)
        {
            ForwardCommand(args);
            _mutex.Dispose();
            _mutex = null;
            return;
        }
        try
        {
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }
        finally
        {
            _mutex?.ReleaseMutex();
            _mutex?.Dispose();
        }
    }

    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>().UsePlatformDetect().LogToTrace();
#endif

    public static void ForwardCommand(string[] args)
    {
        try
        {
            var f = Path.Combine(Path.GetTempPath(), "qbaswin-cmd.json");
            var box = new { Data = string.Join('\n', args) };
            File.WriteAllText(f, System.Text.Json.JsonSerializer.Serialize(box));
        }
        catch { }
        try { _cmdEvent?.Set(); } catch { }
    }

    public static string? WaitCommand(int timeoutMs)
    {
        try
        {
            if (_cmdEvent == null || !_cmdEvent.WaitOne(timeoutMs)) return null;
            var f = Path.Combine(Path.GetTempPath(), "qbaswin-cmd.json");
            if (!File.Exists(f)) return null;
            using var doc = System.Text.Json.JsonDocument.Parse(File.ReadAllText(f));
            return doc.RootElement.TryGetProperty("Data", out var el) ? el.GetString() : null;
        }
        catch { return null; }
    }
}