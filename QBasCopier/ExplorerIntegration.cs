using Microsoft.Win32;

namespace QBasCopier;

public static class ExplorerIntegration
{
    public static string ExePath
    {
        get
        {
            var p = Process.GetCurrentProcess().MainModule?.FileName;
            return p ?? System.Reflection.Assembly.GetEntryAssembly()?.Location ?? "";
        }
    }

    private static readonly string KeyFiles = @"Software\Classes\*\shell\QBasCopier";
    private static readonly string KeyFilesMove = @"Software\Classes\*\shell\QBasCopierMove";
    private static readonly string KeyDir = @"Software\Classes\Directory\shell\QBasCopier";
    private static readonly string KeyDirMove = @"Software\Classes\Directory\shell\QBasCopierMove";
    private static readonly string KeyBg = @"Software\Classes\Directory\Background\shell\QBasCopierHere";
    private static readonly string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";

    public static void SetVerb(RegistryKey root, string path, string name, string args)
    {
        using var verb = root.CreateSubKey(path);
        verb.SetValue(null, name);
        verb.SetValue("Icon", $"\"{ExePath}\",0");
        using var cmd = verb.CreateSubKey("command");
        cmd.SetValue(null, $"\"{ExePath}\" {args}");
    }

    public static void Install()
    {
        using var hkcu = Registry.CurrentUser;
        SetVerb(hkcu, KeyFiles, "Copiar con QBasCopier&Transfer", "--copy -- \"%1\"");
        SetVerb(hkcu, KeyFilesMove, "Mover con QBasCopier&Transfer", "--move -- \"%1\"");
        SetVerb(hkcu, KeyDir, "Copiar con QBasCopier&Transfer", "--copy -- \"%1\"");
        SetVerb(hkcu, KeyDirMove, "Mover con QBasCopier&Transfer", "--move -- \"%1\"");
        SetVerb(hkcu, KeyBg, "Copiar aquí con QBasCopier&Transfer…", "--copy -- \"%V\"");

        CreateSendTo();
    }

    public static void Uninstall()
    {
        using var hkcu = Registry.CurrentUser;
        foreach (var k in new[] { KeyFiles, KeyFilesMove, KeyDir, KeyDirMove, KeyBg })
            try { hkcu.DeleteSubKeyTree(k, false); } catch { }
        try
        {
            var sendTo = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"Microsoft\Windows\SendTo");
            var lnk = Path.Combine(sendTo, "QBasCopier&Transfer.lnk");
            if (File.Exists(lnk)) File.Delete(lnk);
        }
        catch { }
    }

    public static bool IsInstalled
    {
        get
        {
            using var hkcu = Registry.CurrentUser;
            return hkcu.OpenSubKey(KeyFiles) != null;
        }
    }

    public static void CreateSendTo()
    {
        try
        {
            var sendTo = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"Microsoft\Windows\SendTo");
            Directory.CreateDirectory(sendTo);
            var lnk = Path.Combine(sendTo, "QBasCopier&Transfer.lnk");
            if (File.Exists(lnk)) return;
            var t = Type.GetTypeFromProgID("WScript.Shell");
            if (t == null) return;
            dynamic shell = Activator.CreateInstance(t)!;
            dynamic sc = shell.CreateShortcut(lnk);
            sc.TargetPath = ExePath;
            sc.Arguments = "--copy -- \"%1\""; // SendTo rellena los archivos seleccionados
            sc.IconLocation = $"{ExePath},0";
            sc.Description = "Copiar con QBasCopier&Transfer";
            sc.Save();
        }
        catch { }
    }

    public static void SetStartWithWindows(bool on)
    {
        try
        {
            using var rk = Registry.CurrentUser.OpenSubKey(RunKey, true);
            if (on) rk?.SetValue("QBasCopier&Transfer", $"\"{ExePath}\" --hidden");
            else rk?.DeleteValue("QBasCopier&Transfer", false);
        }
        catch { }
    }

    public static void OpenResultsFolder(string dir)
    {
        try
        {
            Process.Start(new ProcessStartInfo { FileName = dir, UseShellExecute = true });
        }
        catch { }
    }
}