using System.Text.Json;
using System.Text.Json.Serialization;

namespace QBasCopier;

public sealed class Settings
{
    public string Lang { get; set; } = "es";
    public bool TrayIcon { get; set; } = true;
    public bool StartWithWindows { get; set; }
    public string MinimizeTo { get; set; } = "tray";

    public int Threads { get; set; } = 8;
    public string Engine { get; set; } = "native";
    public long BufferBytes { get; set; } = 1024 * 1024;

    public string AfterDone { get; set; } = "close";
    public bool SpeedLimitEnabled { get; set; }
    public long SpeedLimitKb { get; set; }

    public string CollisionDefault { get; set; } = "ask";
    public string ErrorDefault { get; set; } = "ask";
    public int RetryIntervalMs { get; set; } = 1500;

    public bool CopyAttributes { get; set; } = true;
    public bool CopySecurity { get; set; }
    public bool DeleteUnfinished { get; set; } = true;
    public bool KeepOnError { get; set; } = true;
    public bool OverwriteReadOnly { get; set; }
    public bool SkipHiddenSystem { get; set; }
    public bool VerifyChecksum { get; set; }

    public bool ShowInTitle { get; set; } = true;
    public int WindowUpdateMs { get; set; } = 200;
    public int SpeedAvgMs { get; set; } = 1500;
    public int ThrottleMs { get; set; } = 100;
    public string Priority { get; set; } = "normal";

    // SuperCopier-style options
    public bool ActivateOnStart { get; set; } = true;
    public string SizeUnit { get; set; } = "";
    public string AddListsWhen { get; set; } = "always";
    public bool AskConfirm { get; set; }
    public string RenameNewPattern { get; set; } = "%NAME% (%COPY%)%EXT%";
    public string RenameOldPattern { get; set; } = "%NAME%; %NAME% (%COPY%)%EXT%";
    public bool SaveLog { get; set; }
    public long DiskWarnMb { get; set; }

    // Transferir (QBasCopier&Transfer)
    public bool TransferOn { get; set; }
    public int TransferPort { get; set; } = 9527;
    public string TransferNet { get; set; } = "QBasWing-Transfer";
    public string TransferKey { get; set; } = "QBas2026";
    public string TransferInbox { get; set; } = "";
    public bool TransferAuto { get; set; }
    public string DeviceName { get; set; } = "";

    [JsonIgnore] public static string Dir =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "QBasCopier");

    [JsonIgnore] public static string ConfigPath => Path.Combine(Dir, "settings.json");

    public static Settings Load()
    {
        try
        {
            if (!File.Exists(ConfigPath)) return new Settings();
            var s = JsonSerializer.Deserialize<Settings>(File.ReadAllText(ConfigPath));
            return s ?? new Settings();
        }
        catch { return new Settings(); }
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(Dir);
            File.WriteAllText(ConfigPath, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch { }
    }
}