using System.Text.Json;

namespace QBasCopier;

public sealed class HistoryEntry
{
    public string Time { get; set; } = "";
    public string Source { get; set; } = "";
    public string Dest { get; set; } = "";
    public string Result { get; set; } = "";
    public long DoneBytes { get; set; }
}

public static class HistoryStore
{
    private static readonly JsonSerializerOptions Opt = new() { WriteIndented = false };

    [System.Text.Json.Serialization.JsonIgnore]
    public static string PathDir =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "QBasCopier");

    public static string PathFile => Path.Combine(PathDir, "history.json");

    public static async Task<List<HistoryEntry>> LoadAsync()
    {
        try
        {
            if (!File.Exists(PathFile)) return new List<HistoryEntry>();
            var txt = await File.ReadAllTextAsync(PathFile);
            return JsonSerializer.Deserialize<List<HistoryEntry>>(txt) ?? new List<HistoryEntry>();
        }
        catch { return new List<HistoryEntry>(); }
    }

    public static async Task AppendAsync(string src, string dst, string result, long done)
    {
        try
        {
            Directory.CreateDirectory(PathDir);
            List<HistoryEntry> list;
            if (File.Exists(PathFile))
                list = JsonSerializer.Deserialize<List<HistoryEntry>>(await File.ReadAllTextAsync(PathFile)) ?? new List<HistoryEntry>();
            else
                list = new List<HistoryEntry>();

            list.Add(new HistoryEntry
            {
                Time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                Source = src,
                Dest = dst,
                Result = result,
                DoneBytes = done
            });

            if (list.Count > 2000) list.RemoveRange(0, list.Count - 2000);
            await File.WriteAllTextAsync(PathFile, JsonSerializer.Serialize(list, Opt));
        }
        catch { }
    }

    public static async Task ClearAsync()
    {
        try
        {
            if (File.Exists(PathFile))
            {
                await using var _ = new FileStream(PathFile, FileMode.Create).ConfigureAwait(false);
            }
        }
        catch { }
    }
}