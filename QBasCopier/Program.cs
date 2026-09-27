using Avalonia;
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
            // Ya hay una copia corriendo en otra ventana. Se le pasa el comando
            // entero, pero antes se pregunta al Explorador cual es la seleccion
            // marcada: el "%1" de Windows solo es el primer archivo, y es esta la
            // unica occasion de leer la seleccion completa, porque es aqui donde el
            // Explorador sigue siendo la ventana de delante.
            ForwardCommand(Amplia(args));
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

    /// <summary>
    /// Convierte "--copy --from-shell -- ruta" en la lista completa de archivos
    /// marcados en el Explorador y su carpeta de destino. Si no viene del Explorador,
    /// la linea de ordenes se queda como estaba.
    /// </summary>
    public static string[] Amplia(string[] args)
    {
        if (!args.Contains("--from-shell")) return args;
        var paths = new List<string>();
        string? dest = null;
        bool move = false, after = false;
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "--copy") after = true;
            else if (args[i] == "--move") { move = true; after = true; }
            else if (args[i] == "--dest" && i + 1 < args.Length) dest = args[++i];
            else if (after && !args[i].StartsWith("--")) paths.Add(args[i]);
        }
        ShellSelection.Completa(paths, ref dest, move);
        var salida = new List<string> { move ? "--move" : "--copy", "--" };
        salida.AddRange(paths);
        if (!string.IsNullOrEmpty(dest)) { salida.Add("--dest"); salida.Add(dest); }
        return salida.ToArray();
    }

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