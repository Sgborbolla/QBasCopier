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
        SetVerb(hkcu, KeyFiles, "Copiar con QBasWing Shuttle · QBasCopier y Transfer", "--copy -- \"%1\"");
        SetVerb(hkcu, KeyFilesMove, "Mover con QBasWing Shuttle · QBasCopier y Transfer", "--move -- \"%1\"");
        SetVerb(hkcu, KeyDir, "Copiar con QBasWing Shuttle · QBasCopier y Transfer", "--copy -- \"%1\"");
        SetVerb(hkcu, KeyDirMove, "Mover con QBasWing Shuttle · QBasCopier y Transfer", "--move -- \"%1\"");
        SetVerb(hkcu, KeyBg, "Copiar aquí con QBasWing Shuttle · QBasCopier y Transfer…", "--copy -- \"%V\"");

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
            var lnk = Path.Combine(sendTo, "QBasCopier-y-Transfer.lnk");
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
            var lnk = Path.Combine(sendTo, "QBasWing-Shuttle.lnk");
            if (File.Exists(lnk)) return;
            var t = Type.GetTypeFromProgID("WScript.Shell");
            if (t == null) return;
            dynamic shell = Activator.CreateInstance(t)!;
            dynamic sc = shell.CreateShortcut(lnk);
            sc.TargetPath = ExePath;
            sc.Arguments = "--copy -- \"%1\""; // SendTo rellena los archivos seleccionados
            sc.IconLocation = $"{ExePath},0";
            sc.Description = "Copiar con QBasWing Shuttle · QBasCopier y Transfer";
            sc.Save();
        }
        catch { }
    }

    // ------------------------------------------------------------------
    //  Modo "copiador del sistema"
    // ------------------------------------------------------------------
    //  Windows tiene un verbo interno llamado "Windows.copy" que es el que usan
    //  el Copiar del clic derecho, el Ctrl+C y el Ctrl+V de los archivos. Si se
    //  reemplaza ese verbo por el nuestro, la copia pasa por aqui sin tocar el
    //  Explorador ni instalar drivers, y todo se deshace volviendo a poner el
    //  valor original.
    //
    //  Antes de tocar nada se guarda el valor que habia en una clave propia, para
    //  que al quitar la integracion se devuelva exactamente lo que habia y no se
    //  rompa el Copiar de Windows.
    //
    //  Arrastrar y soltar no se puedetomar sin una extension del shell (una DLL
    //  COM registrada), asi que eso no se toca: el arrastre sigue usando el motor
    //  del Explorador. Lo que si queda es Ctrl+C, Ctrl+V y el clic derecho.

    private const string BackupKey = @"Software\QBasWing\Shuttle\VerbBackup";

    private static readonly string[] CopyVerbKeys =
    {
        @"Software\Classes\*\shell\Windows.copy\command",
        @"Software\Classes\AllFilesystemObjects\shell\Windows.copy\command",
        @"Software\Classes\Directory\shell\Windows.copy\command",
        @"Software\Classes\Directory\Background\shell\Windows.copy\command",
    };

    public static bool IsDefaultCopier
    {
        get
        {
            try
            {
                using var hkcu = Registry.CurrentUser;
                using var k = hkcu.OpenSubKey(CopyVerbKeys[0] + @"\ShuttleOwned");
                return k?.GetValue(null)?.ToString() == "1";
            }
            catch { return false; }
        }
    }

    /// <summary>Deja la copia del sistema en manos de la app. Devuelve false si el registro no deja.</summary>
    public static bool SetAsDefaultCopier(bool on)
    {
        try
        {
            using var hkcu = Registry.CurrentUser;
            if (on)
            {
                // Se guarda lo que hay ahora, una sola vez, para poder restaurarlo.
                using (var back = hkcu.CreateSubKey(BackupKey))
                {
                    foreach (var k in CopyVerbKeys)
                    {
                        using var cur = hkcu.OpenSubKey(k);
                        back.SetValue(k, cur?.GetValue(null)?.ToString() ?? "", RegistryValueKind.String);
                    }
                }
                foreach (var k in CopyVerbKeys)
                {
                    using var cmd = hkcu.CreateSubKey(k);
                    cmd.SetValue(null, $"\"{ExePath}\" --copy -- \"%1\"");
                    // El ExplorerCache son las claves espejo de 32 bits; sin esto el
                    // cambio no se ve en el explorador hasta reiniciar sesion.
                    using var owned = cmd.CreateSubKey("ShuttleOwned");
                    owned.SetValue(null, "1");
                }
                TryNotify();
                return true;
            }

            foreach (var k in CopyVerbKeys)
            {
                string original;
                using (var back = hkcu.OpenSubKey(BackupKey))
                    original = back?.GetValue(k)?.ToString() ?? "";
                using (var cmd = hkcu.CreateSubKey(k))
                {
                    if (string.IsNullOrEmpty(original)) cmd.DeleteValue(null, false);
                    else cmd.SetValue(null, original);
                    cmd.DeleteSubKeyTree("ShuttleOwned", false);
                }
            }
            try { hkcu.DeleteSubKeyTree(BackupKey, false); } catch { }
            TryNotify();
            return true;
        }
        catch { return false; }
    }

    /// <summary>Le dice al Explorador que lea de nuevo el registro sin reiniciar sesion.</summary>
    private static void TryNotify()
    {
        try
        {
            if (!OperatingSystem.IsWindows()) return;
            SHChangeNotify(0x08000000, 0x0000, IntPtr.Zero, IntPtr.Zero); // SHCNE_ASSOCCHANGED
        }
        catch { }
    }

    [System.Runtime.InteropServices.DllImport("shell32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
    private static extern void SHChangeNotify(int eventId, uint flags, IntPtr item1, IntPtr item2);

    public static void SetStartWithWindows(bool on)
    {
        try
        {
            using var rk = Registry.CurrentUser.OpenSubKey(RunKey, true);
            if (on) rk?.SetValue("QBasCopier y Transfer", $"\"{ExePath}\" --hidden");
            else rk?.DeleteValue("QBasCopier y Transfer", false);
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