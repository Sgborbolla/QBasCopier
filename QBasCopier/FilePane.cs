using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;

namespace QBasCopier;

public sealed class PaneEntry
{
    public string Name { get; set; } = "";
    public string FullPath { get; set; } = "";
    public bool IsDir { get; set; }
    public long Size { get; set; }
    public string SizeText { get; set; } = "";
    public string Modified { get; set; } = "";
    public string Type { get; set; } = "";
}

public sealed class FilePane : UserControl
{
    public event Action? PathChanged;
    public event Action? SelectionChanged;
    public event Action<string[]>? FilesDropped;

    public string CurrentPath { get; private set; } = "";
    public bool IsFocusedPane { get; set; }

    private static readonly IBrush DirBrush = BrushesFrom("#9FB3E8");
    private static readonly IBrush DirBrushBack = BrushesFrom("#6FB1FC");
    private static readonly IBrush TextMain = BrushesFrom("#F6F8FF");
    private static readonly IBrush Gold = BrushesFrom("#FBBF24");
    private static readonly IBrush TextBlue = BrushesFrom("#6FB1FC");
    private static readonly IBrush BgDeep = BrushesFrom("#08122F");
    private static readonly IBrush BgHeader = BrushesFrom("#0B1740");

    private static SolidColorBrush BrushesFrom(string hex) => new(Color.Parse(hex));

    private readonly StackPanel _driveBar = new() { Orientation = Orientation.Horizontal };
    private readonly ListBox _lv = new();
    private readonly TextBox _pathTb = new();
    private readonly List<PaneEntry> _entries = new();
    private readonly List<string> _back = new(), _fwd = new();
    private Border? _host;

    public FilePane()
    {
        Build();
        RefreshDrives();
        Navigate(HomePath());
    }

    private static string HomePath()
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (Directory.Exists(home)) return home;
        try
        {
            var f = DriveInfo.GetDrives().FirstOrDefault(d => d.IsReady && d.DriveType == DriveType.Fixed);
            if (f != null) return f.RootDirectory.FullName;
        }
        catch { }
        return Directory.GetCurrentDirectory();
    }

    private void Build()
    {
        Background = BgDeep;
        var grid = new Grid
        {
            RowDefinitions =
            {
                new RowDefinition(GridLength.Auto),
                new RowDefinition(GridLength.Auto),
                new RowDefinition(GridLength.Star)
            }
        };

        var dbw = new Border { Background = BgHeader, Padding = new Thickness(4, 2, 4, 2), Child = _driveBar };
        Grid.SetRow(dbw, 0);
        grid.Children.Add(dbw);

        var nav = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 2, 0, 2) };
        nav.Children.Add(MkSm("◀", Back));
        nav.Children.Add(MkSm("▶", Forward));
        nav.Children.Add(MkSm("↑", Up));
        _pathTb.Margin = new Thickness(4, 0, 4, 0);
        _pathTb.Watermark = "ruta…";
        _pathTb.KeyDown += (s, e) => { if (e.Key == Key.Enter) Navigate(_pathTb.Text ?? ""); };
        nav.Children.Add(_pathTb);
        nav.Children.Add(MkSm("⟳", () => { RefreshDrives(); Navigate(CurrentPath); }));
        Grid.SetRow(nav, 1);
        grid.Children.Add(nav);

        _lv.ItemsPanel = new FuncTemplate<Panel>(() => new StackPanel());
        _lv.ItemTemplate = new FuncDataTemplate<PaneEntry>(BuildRow);
        _lv.SelectionChanged += (s, e) => SelectionChanged?.Invoke();
        _lv.DoubleTapped += OnDouble;
        DragDrop.SetAllowDrop(_lv, true);
        _lv.AddHandler(DragDrop.DragOverEvent, (EventHandler<DragEventArgs>)((s, e) =>
        {
            if (e.Data.Contains(DataFormats.Files)) e.DragEffects = DragDropEffects.Copy;
        }));
        _lv.AddHandler(DragDrop.DropEvent, (EventHandler<DragEventArgs>)((s, e) =>
        {
            if (!e.Data.Contains(DataFormats.Files)) return;
            var files = e.Data.GetFiles()?.Select(x => x.Path.LocalPath).ToArray();
            if (files != null && files.Length > 0) FilesDropped?.Invoke(files);
        }));
        _host = new Border { Background = BgDeep, BorderBrush = BrushesFrom("#1F3B8C"), BorderThickness = new Thickness(1), Padding = new Thickness(2), Child = _lv };
        Grid.SetRow(_host, 2);
        grid.Children.Add(_host);

        PointerPressed += (s, e) => IsFocusedPane = true;
        Content = grid;
    }

    private Control BuildRow(PaneEntry e)
    {
        var grid = new Grid
        {
            ColumnDefinitions = { new ColumnDefinition(new GridLength(28)), new ColumnDefinition(GridLength.Star), new ColumnDefinition(new GridLength(90)), new ColumnDefinition(new GridLength(130)), new ColumnDefinition(new GridLength(110)) }
        };
        var glyph = new TextBlock { Text = e.IsDir ? "▸" : "", VerticalAlignment = VerticalAlignment.Center };
        glyph.Foreground = e.IsDir ? DirBrush : BrushesFrom("#3352A8");
        Grid.SetColumn(glyph, 0);
        grid.Children.Add(glyph);

        var name = new TextBlock
        {
            Text = e.Name,
            TextTrimming = TextTrimming.CharacterEllipsis,
            Foreground = e.IsDir ? DirBrushBack : TextMain,
            FontWeight = e.IsDir ? FontWeight.Bold : FontWeight.Normal,
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(name, 1);
        grid.Children.Add(name);

        var size = new TextBlock { Text = e.SizeText, Foreground = TextMain, HorizontalAlignment = HorizontalAlignment.Right };
        Grid.SetColumn(size, 2);
        grid.Children.Add(size);

        var mod = new TextBlock { Text = e.Modified, Foreground = BrushesFrom("#9FB3E8") };
        Grid.SetColumn(mod, 3);
        grid.Children.Add(mod);

        var type = new TextBlock { Text = e.Type, Foreground = BrushesFrom("#9FB3E8") };
        Grid.SetColumn(type, 4);
        grid.Children.Add(type);
        return grid;
    }

    private Button MkSm(string txt, Action act)
    {
        var b = new Button
        {
            Content = txt,
            FontSize = 13,
            Width = 34,
            Padding = new Thickness(3, 2, 3, 2),
            Margin = new Thickness(1)
        };
        b.Click += (s, e) => act();
        return b;
    }

    public void RefreshDrives()
    {
        _driveBar.Children.Clear();
        _driveBar.Children.Add(MkSm("⌂", () => Navigate(HomePath())));
        try
        {
            foreach (var d in DriveInfo.GetDrives())
            {
                if (!d.IsReady) continue;
                var b = new Button
                {
                    Content = $"[{d.Name.TrimEnd('\\', '/')}]",
                    Margin = new Thickness(1),
                    Padding = new Thickness(6, 1, 6, 1),
                    Foreground = d.DriveType == DriveType.Removable ? Gold : TextBlue
                };
                ToolTip.SetTip(b, d.VolumeLabel.Length > 0 ? $"{d.Name} {d.VolumeLabel}" : d.Name);
                var letter = d.Name;
                b.Click += (s, e) =>
                {
                    try { Navigate(new DriveInfo(letter).RootDirectory.FullName); }
                    catch { }
                };
                _driveBar.Children.Add(b);
            }
        }
        catch { }
    }

    public void Navigate(string path)
    {
        try
        {
            if (Directory.Exists(path))
            {
                if (CurrentPath.Length > 0) _back.Add(CurrentPath);
                _fwd.Clear();
                var full = Path.GetFullPath(path);
                LoadDir(full, Path.GetPathRoot(full) ?? full);
            }
        }
        catch { }
    }

    private void LoadDir(string dir, string root)
    {
        CurrentPath = dir;
        _pathTb.Text = dir;
        _entries.Clear();
        try
        {
            foreach (var d in Directory.EnumerateDirectories(dir))
                _entries.Add(new PaneEntry
                {
                    Name = Path.GetFileName(d),
                    FullPath = d,
                    IsDir = true,
                    Modified = Directory.GetLastWriteTime(d).ToString("g")
                });
            foreach (var f in Directory.EnumerateFiles(dir))
            {
                var fi = new FileInfo(f);
                _entries.Add(new PaneEntry
                {
                    Name = fi.Name,
                    FullPath = f,
                    IsDir = false,
                    Size = fi.Length,
                    SizeText = Human(fi.Length),
                    Modified = fi.LastWriteTime.ToString("g")
                });
            }
        }
        catch { }
        _entries.Sort((a, b) => b.IsDir.CompareTo(a.IsDir) != 0 ? b.IsDir.CompareTo(a.IsDir) : string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
        _lv.ItemsSource = null;
        _lv.ItemsSource = _entries;
        PathChanged?.Invoke();
    }

    public static string SizeUnit = "";
    public static string Human(long bytes)
    {
        var force = (SizeUnit ?? "").Trim().ToUpperInvariant();
        if (force.Length > 0)
        {
            string[] ul = { "KB", "MB", "GB", "TB" };
            int fi = Array.IndexOf(ul, force);
            if (fi >= 0)
            {
                double v = bytes;
                for (int i = 0; i <= fi; i++) v /= 1024.0;
                return $"{v:0.##} {force}";
            }
        }
        string[] u = { "B", "KB", "MB", "GB", "TB" };
        double va = bytes; int ia = 0;
        while (va >= 1024 && ia < u.Length - 1) { va /= 1024; ia++; }
        return $"{va:0.##} {u[ia]}";
    }

    public void Up()
    {
        var full = CurrentPath;
        if (full.Length == 0) return;
        var up = Path.GetDirectoryName(full.TrimEnd('\\', '/'));
        if (up == null || up == full) return;
        Navigate(up);
    }

    public void Back()
    {
        if (_back.Count > 0)
        {
            _fwd.Add(CurrentPath);
            var p = _back[^1];
            _back.RemoveAt(_back.Count - 1);
            NavigateNoHist(p);
        }
    }

    public void Forward()
    {
        if (_fwd.Count > 0)
        {
            _back.Add(CurrentPath);
            var p = _fwd[^1];
            _fwd.RemoveAt(_fwd.Count - 1);
            NavigateNoHist(p);
        }
    }

    private void NavigateNoHist(string p)
    {
        try
        {
            if (Directory.Exists(p)) LoadDir(p, Path.GetPathRoot(p) ?? p);
        }
        catch { }
    }

    private void OnDouble(object? sender, TappedEventArgs e)
    {
        if (_lv.SelectedItem is PaneEntry pe)
        {
            if (pe.IsDir) Navigate(pe.FullPath);
            else OpenFile(pe.FullPath);
        }
    }

    private static void OpenFile(string path)
    {
        try
        {
            var psi = new System.Diagnostics.ProcessStartInfo { FileName = path, UseShellExecute = true };
            System.Diagnostics.Process.Start(psi);
        }
        catch { }
    }

    public List<string> SelectedPaths
    {
        get
        {
            var list = new List<string>();
            foreach (var o in _lv.SelectedItems)
                if (o is PaneEntry pe) list.Add(pe.FullPath);
            return list;
        }
    }

    public void SelectPath(string p)
    {
        var it = _entries.FirstOrDefault(x => x.FullPath == p);
        if (it != null) _lv.SelectedItem = it;
    }
}

public static class Util
{
    public static string Joined(this IEnumerable<string> e, string sep) => string.Join(sep, e);
}