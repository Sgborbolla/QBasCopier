using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using System.Diagnostics;
using System.Globalization;
using System.IO;

namespace QBasCopier;

public sealed partial class MainWindow : Window
{
    public static Settings S = new();

    // ---------- fábricas ----------
    private static IBrush B(string hex) => new SolidColorBrush(Color.Parse(hex));
    private static readonly IBrush BgPanel = B("#060D24");
    private static readonly IBrush BgDeep = B("#08122F");
    private static readonly IBrush TextMain = B("#F6F8FF");
    private static readonly IBrush TextSoft = B("#9FB3E8");
    private static readonly IBrush Gold = B("#FBBF24");
    private static readonly IBrush Red = B("#CF142B");
    private static readonly IBrush Line = B("#1F3B8C");

    private readonly List<(string key, Action set)> _texts = new();
    private readonly List<CopyItem> _queue = new();
    private readonly List<string> _errLines = new();
    private readonly List<HistoryEntry> _history = new();
    private readonly Stopwatch _batchSw = new();

    private CopyEngine? _engine;
    private double _lastRate, _lastDone;
    private long _lastTickTicks;
    private bool _running;
    private bool _forceClose;
    private TrayIcon? _tray;
    private DispatcherTimer? _ticker;

    private static Bitmap? _logoBmp;
#if !ANDROID
    private static WindowIcon? _logo;
#endif

    private TextBlock _lblFrom = null!, _lblProg = null!, _lblRate = null!, _lblTime = null!, _lblCur = null!, _lblStatus = null!;
    private TextBox _tbFrom = null!, _tbTo = null!;
    private ProgressBar _ggBar = null!, _miniBar = null!;
    private TextBlock _miniLbl = null!;
    private TabControl _tabs = null!;
    private Border _mini = null!;
    private Button _bCopy = null!, _bMove = null!, _bPause = null!, _bResume = null!, _bSkip = null!, _bCancel = null!, _bClear = null!;
    private Button _bIntegrate = null!, _bFold = null!, _bQuit = null!;
    private ListBox _lbQueue = null!, _lbErrs = null!, _lbHist = null!;
    private FilePane _left = null!, _right = null!;
    private ComboBox _cmbLang = null!, _cmbLangQuick = null!, _cmbAfter = null!, _cmbEngine = null!, _cmbCollision = null!, _cmbError = null!, _cmbPriority = null!, _cmbBuffer = null!, _cmbSizeUnit = null!, _cmbAddWhen = null!;
    private TextBox _tbBuffer = null!, _tbRetry = null!, _tbSpeed = null!, _tbUpdate = null!, _tbAvg = null!, _tbThrottle = null!, _tbWarn = null!, _tbNewPat = null!;
    private Slider _sldThreads = null!, _sldSpeed = null!;
    private TextBlock _lblThreads = null!, _lblSpeed = null!;
    private CheckBox _chkTray = null!, _chkStart = null!, _chkAttrib = null!, _chkSec = null!, _chkDel = null!, _chkKeep = null!, _chkRO = null!, _chkHidden = null!, _chkTitle = null!, _chkLimit = null!, _chkVerify = null!, _chkActivate = null!, _chkAskAdd = null!, _chkLog = null!;

    private const string TabExplorer = "explorer", TabQueue = "queue", TabErrors = "errors", TabOptions = "options", TabHistory = "history";
    private const string AboutText =
        "QBasCopier crece de un sueño: el de QBaswing Designer, una pequeña compañía independiente " +
        "que nació de las manos del Dr. Sergio Grabiel Borbolla Verdecia. Desde Cuba, con el corazón " +
        "lleno de amor por la medicina y por el mundo digital, cada línea se escribe con esfuerzo y " +
        "esperanza, aunque a veces la tecnología no alcance. Este es un pequeño homenaje a la idea de " +
        "que con dedicación se cumplen sueños y se entregan al mundo obras útiles y hermosas. " +
        "Gracias por formar parte de él.\n\n— SGBV";

    public MainWindow()
    {
        InitializeComponent();
#if !ANDROID
        _logo = LoadLogo();
        Icon = _logo;
#endif
        Title = "QBasCopier";
        BuildWindow();
        _ticker = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(Math.Clamp(S.WindowUpdateMs, 50, 1000)) };
        _ticker.Tick += (_, _) => Tick();
        _ticker.Start();
    }

    #if !ANDROID
    private static WindowIcon? LoadLogo()
    {
        try
        {
            using var s = AssetLoader.Open(new Uri("avares://QBasCopier/Assets/logo.png"));
            var bmp = new Bitmap(s);
            _logoBmp = bmp;
            return new WindowIcon(bmp);
        }
        catch { return null; }
    }
#else
    private static Bitmap? LoadLogo()
    {
        try
        {
            using var s = AssetLoader.Open(new Uri("avares://QBasCopier/Assets/logo.png"));
            var bmp = new Bitmap(s);
            _logoBmp = bmp;
            return bmp;
        }
        catch { return null; }
    }
#endif

    public void InitialBoot()
    {
        var args = Program.StartupArgs;
        S = Settings.Load();
        if (string.IsNullOrEmpty(S.Lang)) S.Lang = Code(DetectLikely());
        L.SetLanguage(S.Lang);

        var paths = new List<string>();
        string? dest = null;
        bool move = false, hidden = args.Contains("--hidden"), after = false;

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--lang" when i + 1 < args.Length:
                    S.Lang = Code(args[++i]); L.SetLanguage(S.Lang); break;
                case "--install-integration":
#if !ANDROID
                    if (OperatingSystem.IsWindows()) ExplorerIntegration.Install();
#endif
                    break;
                case "--uninstall-integration":
#if !ANDROID
                    if (OperatingSystem.IsWindows()) ExplorerIntegration.Uninstall();
#endif
                    break;
                case "--start-with-windows":
                    S.StartWithWindows = true;
#if !ANDROID
                    if (OperatingSystem.IsWindows()) ExplorerIntegration.SetStartWithWindows(true);
#endif
                    break;
                case "--move": move = true; break;
                case "--copy": after = true; break;
                case "--dest" when i + 1 < args.Length:
                    dest = args[++i];
                    if (Directory.Exists(dest) && dest.Length > 0 && dest[^1] != '/' && dest[^1] != '\\') dest += Path.DirectorySeparatorChar;
                    break;
                default:
                    if (after && !args[i].StartsWith("--")) paths.Add(args[i]);
                    break;
            }
        }

        ReloadTexts();
        BuildTray();
        if (paths.Count > 0) AddFiles(paths.ToArray());
        if (!string.IsNullOrEmpty(dest)) _tbTo.Text = dest;

        if (hidden && paths.Count == 0)
        {
#if !ANDROID
            Hide();
#endif
            return;
        }
#if !ANDROID
        Show();
        Activate();
#endif
        if (paths.Count > 0 && !string.IsNullOrEmpty(dest)) _ = Task.Delay(120).ContinueWith(_ => Dispatcher.UIThread.Post(() => _ = StartCopy(move)));
    }

    public static string Code(string name)
    {
        var c = name.ToLowerInvariant();
        var known = Ex.Codes;
        if (known.Contains(c)) return c;
        if (c.StartsWith("es")) return "es";
        if (c.StartsWith("pt")) return "pt";
        if (c.StartsWith("zh")) return "zh";
        if (c.StartsWith("ja")) return "ja";
        if (c.StartsWith("ko")) return "ko";
        if (c.StartsWith("ar")) return "ar";
        if (c.StartsWith("ru")) return "ru";
        if (c.StartsWith("uk")) return "uk";
        if (c.Length >= 2) return c[..2];
        return "en";
    }

    private static string DetectLikely() => CultureInfo.CurrentUICulture.Name;

    // ---------------------------------------------------------------- build
    private void BuildWindow()
    {
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        var root = this.FindControl<Grid>("Root");
        var main = new Grid
        {
            RowDefinitions = { new(GridLength.Auto), new(GridLength.Star), new(GridLength.Auto) }
        };
        main.Children.Add(BuildToolbar());
        main.Children.Add(BuildBody());
        main.Children.Add(BuildStatusbar());
        Grid.SetRow(main.Children[0] as Control, 0);
        Grid.SetRow(main.Children[1] as Control, 1);
        Grid.SetRow(main.Children[2] as Control, 2);
        root.Children.Add(main);
    }

    private Control BuildToolbar()
    {
        var bar = new Grid
        {
            ColumnDefinitions = { new(GridLength.Auto), new(GridLength.Star), new(GridLength.Auto), new(GridLength.Auto), new(GridLength.Auto) },
            Margin = new Thickness(8, 4)
        };
        var brand = new TextBlock { Text = "QBasCopier", FontSize = 18, FontWeight = FontWeight.Bold, Foreground = Gold, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(2, 0, 8, 0) };
        Grid.SetColumn(brand, 0);
        bar.Children.Add(brand);

        _cmbLangQuick = new ComboBox { ItemsSource = Ex.LangNames(), SelectedIndex = Ex.IndexOf(S.Lang), Width = 170, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 10, 0) };
        _cmbLangQuick.SelectionChanged += (s, e) => { if (Ex.Codes.Length > (s as ComboBox)?.SelectedIndex) ApplyLang(Ex.Codes[((ComboBox)s!).SelectedIndex]); };
        Grid.SetColumn(_cmbLangQuick, 1);
        bar.Children.Add(_cmbLangQuick);

        _bIntegrate = Mk("integrate", ToggleIntegration);
        Grid.SetColumn(_bIntegrate, 2);
        bar.Children.Add(_bIntegrate);

        _bFold = Mk("fold", ToggleFold);
        Grid.SetColumn(_bFold, 3);
        bar.Children.Add(_bFold);

        var bQuit = Mk("quit", DoQuit);
        bQuit.Classes.Add("primary");
        _bQuit = bQuit;
        Grid.SetColumn(bQuit, 4);
        bar.Children.Add(bQuit);
        return bar;
    }

    private Control BuildBody()
    {
        var body = new Grid { RowDefinitions = { new(GridLength.Star) }, Margin = new Thickness(4, 2, 4, 2) };
        _tabs = new TabControl();
        _tabs.Items.Add(MkTab(TabExplorer, BuildExplorerTab()));
        _tabs.Items.Add(MkTab(TabQueue, BuildQueueBody()));
        _tabs.Items.Add(MkTab(TabErrors, MakeErrBody()));
        _tabs.Items.Add(MkTab(TabOptions, new ScrollViewer { Content = BuildOptsGrid() }));
        _tabs.Items.Add(MkTab(TabHistory, MakeHistBody()));
        Grid.SetRow(_tabs, 0);
        body.Children.Add(_tabs);

        _mini = new Border
        {
            Background = BgPanel,
            BorderBrush = Line,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(12, 8),
            IsVisible = false,
            Child = new StackPanel
            {
                Spacing = 8,
                Children = { (_miniLbl = MkLbl("", 14)), (_miniBar = new ProgressBar { Minimum = 0, Maximum = 100, Height = 16 }) }
            }
        };
        Grid.SetRow(_mini, 0);
        body.Children.Add(_mini);
        return body;
    }

    private Control BuildExplorerTab()
    {
        _left = new FilePane();
        _right = new FilePane();
        _left.FilesDropped += paths => AddWithDest(paths, _right.CurrentPath);
        _right.FilesDropped += paths => AddWithDest(paths, _left.CurrentPath);

        var mid = new StackPanel { Spacing = 6, VerticalAlignment = VerticalAlignment.Center, MinWidth = 56 };
        mid.Children.Add(Mk("⇒", () => PaneCopy(false)));
        mid.Children.Add(Mk("⇐", () => PaneCopy(true)));
        mid.Children.Add(Mk("⇉", () => PaneMove(false)));
        mid.Children.Add(Mk("⇇", () => PaneMove(true)));

        var grid = new Grid
        {
            ColumnDefinitions = { new(GridLength.Star), new(GridLength.Auto), new(GridLength.Star) }
        };
        Grid.SetColumn(_left, 0);
        grid.Children.Add(_left);
        Grid.SetColumn(mid, 1);
        grid.Children.Add(mid);
        Grid.SetColumn(_right, 2);
        grid.Children.Add(_right);
        return grid;
    }

    private Control BuildQueueBody()
    {
        var panel = new StackPanel { Spacing = 6 };

        var fromRow = new Grid { ColumnDefinitions = { new(GridLength.Auto), new(GridLength.Star), new(GridLength.Auto), new(GridLength.Auto) } };
        _lblFrom = MkLbl("", 13);
        Grid.SetColumn(_lblFrom, 0); fromRow.Children.Add(_lblFrom);
        _tbFrom = new TextBox { Margin = new Thickness(4, 0, 4, 0) };
        Grid.SetColumn(_tbFrom, 1); fromRow.Children.Add(_tbFrom);
        var bFrom = Mk("…", () => PickDirInto(_tbFrom));
        Grid.SetColumn(bFrom, 2); fromRow.Children.Add(bFrom);
        var bAdd = Mk("+", () => PickFiles());
        Grid.SetColumn(bAdd, 3); fromRow.Children.Add(bAdd);

        var toRow = new Grid { ColumnDefinitions = { new(GridLength.Auto), new(GridLength.Star), new(GridLength.Auto) } };
        var lblTo = MkLbl("", 13);
        Grid.SetColumn(lblTo, 0); toRow.Children.Add(lblTo);
        _tbTo = new TextBox { Margin = new Thickness(4, 0, 4, 0) };
        Grid.SetColumn(_tbTo, 1); toRow.Children.Add(_tbTo);
        var bTo = Mk("…", () => PickDirInto(_tbTo));
        Grid.SetColumn(bTo, 2); toRow.Children.Add(bTo);

        _lbQueue = new ListBox { SelectionMode = SelectionMode.Multiple, MinHeight = 190 };
        DragDrop.SetAllowDrop(_lbQueue, true);
        _lbQueue.AddHandler(DragDrop.DragOverEvent, (EventHandler<DragEventArgs>)((s, e) => { if (e.Data.Contains(DataFormats.Files)) e.DragEffects = DragDropEffects.Copy; }));
        _lbQueue.AddHandler(DragDrop.DropEvent, (EventHandler<DragEventArgs>)((s, e) =>
        {
            if (!e.Data.Contains(DataFormats.Files)) return;
            var fl = e.Data.GetFiles()?.Select(x => x.Path.LocalPath).ToArray();
            if (fl is { Length: > 0 }) AddFiles(fl);
        }));
        _lbQueue.ItemTemplate = new FuncDataTemplate<CopyItem>((it, _ns) => BuildQueueRow(it));

        var gl = new Grid { ColumnDefinitions = { new(GridLength.Auto), new(GridLength.Auto), new(GridLength.Star) } };
        _lblProg = MkLbl("0%", 13);
        Grid.SetColumn(_lblProg, 0); gl.Children.Add(_lblProg);
        _lblRate = MkLbl("", 13);
        Grid.SetColumn(_lblRate, 1); gl.Children.Add(_lblRate);
        _lblTime = MkLbl("", 13);
        _lblTime.HorizontalAlignment = HorizontalAlignment.Right;
        Grid.SetColumn(_lblTime, 2); gl.Children.Add(_lblTime);

        _ggBar = new ProgressBar { Minimum = 0, Maximum = 100, Height = 16 };
        _lblCur = MkLbl("", 13);

        var btns = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Margin = new Thickness(0, 4, 0, 0) };
        btns.Children.Add(_bCopy = Mk("copy", () => _ = StartCopy(false)));
        _bCopy.Classes.Add("primary");
        btns.Children.Add(_bMove = Mk("move", () => _ = StartCopy(true)));
        btns.Children.Add(_bPause = Mk("pause", Pause));
        btns.Children.Add(_bResume = Mk("resume", Resume));
        btns.Children.Add(_bSkip = Mk("skip", SkipRest));
        btns.Children.Add(_bCancel = Mk("cancel", () => _engine?.Cancel()));
        btns.Children.Add(_bClear = Mk("clear", Clear));

        panel.Children.Add(fromRow);
        panel.Children.Add(toRow);
        panel.Children.Add(_lbQueue);
        panel.Children.Add(gl);
        panel.Children.Add(_ggBar);
        panel.Children.Add(_lblCur);
        panel.Children.Add(btns);

        Bind(_lblFrom, "histFrom");
        Bind(lblTo, "histTo");
        Bind(_bCopy, "copy");
        Bind(_bMove, "move");
        Bind(_bPause, "pause");
        Bind(_bResume, "resume");
        Bind(_bSkip, "skip");
        Bind(_bCancel, "cancel");
        Bind(_bClear, "clear");
        return panel;
    }

    private static Control BuildQueueRow(CopyItem it)
    {
        var grid = new Grid { ColumnDefinitions = { new ColumnDefinition(new GridLength(200)), new ColumnDefinition(new GridLength(90)), new ColumnDefinition(GridLength.Star), new ColumnDefinition(new GridLength(150)), new ColumnDefinition(new GridLength(120)) } };
        var name = new TextBlock { TextTrimming = TextTrimming.CharacterEllipsis, VerticalAlignment = VerticalAlignment.Center, FontWeight = it.IsDirectory ? FontWeight.SemiBold : FontWeight.Normal };
        name.Bind(TextBlock.TextProperty, new Binding("Name"));
        Grid.SetColumn(name, 0); grid.Children.Add(name);
        var size = new TextBlock { HorizontalAlignment = HorizontalAlignment.Right };
        size.Bind(TextBlock.TextProperty, new Binding("SizeText"));
        Grid.SetColumn(size, 1); grid.Children.Add(size);
        var bar = new ProgressBar { Minimum = 0, Maximum = 100, Height = 12 };
        bar.Bind(ProgressBar.ValueProperty, new Binding("Percent"));
        Grid.SetColumn(bar, 2); grid.Children.Add(bar);
        var state = new TextBlock { Foreground = TextSoft, TextTrimming = TextTrimming.CharacterEllipsis };
        state.Bind(TextBlock.TextProperty, new Binding("StateText"));
        Grid.SetColumn(state, 3); grid.Children.Add(state);
        var dst = new TextBlock { Foreground = TextSoft, TextTrimming = TextTrimming.CharacterEllipsis };
        dst.Bind(TextBlock.TextProperty, new Binding("DestName"));
        Grid.SetColumn(dst, 4); grid.Children.Add(dst);
        return grid;
    }

    private Control MakeErrBody()
    {
        var panel = new StackPanel { Spacing = 6 };
        _lbErrs = new ListBox { MinHeight = 240 };
        _lbErrs.ItemTemplate = new FuncDataTemplate<string>((s, _ns) => new TextBlock { Text = s, TextWrapping = TextWrapping.Wrap, Foreground = TextSoft });
        panel.Children.Add(_lbErrs);
        var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        var bClr = Mk("clear", () => { _errLines.Clear(); _lbErrs.ItemsSource = null; });
        row.Children.Add(bClr);
        panel.Children.Add(row);
        return panel;
    }

    private Control BuildOptsGrid()
    {
        var g = new Grid
        {
            ColumnDefinitions = { new(GridLength.Star), new(GridLength.Auto) },
            Margin = new Thickness(10)
        };
        var col = new StackPanel { Spacing = 8 };

        var afterCodes = new[] { "close", "keep", "keepIfErrors" };
        _cmbAfter = new ComboBox { ItemsSource = new[] { L.Get("afterClose"), L.Get("afterKeep"), L.Get("afterKeepErr") }, SelectedIndex = Math.Max(0, Array.IndexOf(afterCodes, S.AfterDone)) };
        _cmbAfter.SelectionChanged += (s, e) => { S.AfterDone = afterCodes[Math.Clamp(_cmbAfter.SelectedIndex, 0, 2)]; S.Save(); };

        _cmbLang = new ComboBox { ItemsSource = Ex.LangNames(), SelectedIndex = Ex.IndexOf(S.Lang) };
        _cmbLang.SelectionChanged += (s, e) => { if (_cmbLang.SelectedIndex >= 0 && _cmbLang.SelectedIndex < Ex.Codes.Length) ApplyLang(Ex.Codes[_cmbLang.SelectedIndex]); };

        _cmbEngine = new ComboBox { ItemsSource = new[] { "native", "buffered" }, SelectedItem = S.Engine };
        _cmbEngine.SelectionChanged += (s, e) => { S.Engine = (_cmbEngine.SelectedItem?.ToString() ?? "native") == "buffered" ? "buffered" : "native"; S.Save(); };

        _cmbPriority = new ComboBox { ItemsSource = new[] { "idle", "normal", "high" }, SelectedItem = S.Priority };
        _cmbPriority.SelectionChanged += (s, e) => { S.Priority = _cmbPriority.SelectedItem?.ToString() ?? "normal"; S.Save(); };

        _sldThreads = new Slider { Minimum = 1, Maximum = 32, TickFrequency = 1, IsSnapToTickEnabled = true, Value = S.Threads };
        _lblThreads = MkLbl(S.Threads.ToString(), 13);
        _sldThreads.ValueChanged += (s, e) => { S.Threads = (int)e.NewValue; _lblThreads.Text = ((int)e.NewValue).ToString(); S.Save(); };

        _tbBuffer = new TextBox { Text = (S.BufferBytes / 1024).ToString(), Width = 100 };
        _tbBuffer.TextChanged += (s, e) => { if (long.TryParse(_tbBuffer.Text, out var v) && v >= 16) { S.BufferBytes = v * 1024; S.Save(); } };
        _tbRetry = new TextBox { Text = S.RetryIntervalMs.ToString(), Width = 100 };
        _tbRetry.TextChanged += (s, e) => { if (int.TryParse(_tbRetry.Text, out var v) && v > 0) { S.RetryIntervalMs = v; S.Save(); } };
        _tbSpeed = new TextBox { Text = S.SpeedLimitKb.ToString(), Width = 110 };
        _tbSpeed.TextChanged += (s, e) => { if (long.TryParse(_tbSpeed.Text, out var v)) { S.SpeedLimitKb = v; S.Save(); } };
        _chkLimit = MkChk("", S.SpeedLimitEnabled, b => { S.SpeedLimitEnabled = b; S.Save(); });

        var bufPresets = new long[] { 64, 256, 1024, 4096, 16384, 65536 };
        _cmbBuffer = new ComboBox { Width = 120 };
        _cmbBuffer.ItemsSource = bufPresets.Select(x => $"{x} KB").ToArray();
        int bufIdx = Math.Max(0, Array.IndexOf(bufPresets, Math.Max(64, S.BufferBytes / 1024)));
        _cmbBuffer.SelectedIndex = bufIdx;
        _cmbBuffer.SelectionChanged += (s, e) => { if (_cmbBuffer.SelectedIndex >= 0) { S.BufferBytes = bufPresets[_cmbBuffer.SelectedIndex] * 1024; _tbBuffer.Text = (S.BufferBytes / 1024).ToString(); S.Save(); } };

        var sizeNames = new[] { "B", "KB", "MB", "GB", "TB", "auto" };
        var su = string.IsNullOrEmpty(S.SizeUnit) ? "auto" : S.SizeUnit.ToUpperInvariant();
        if (!sizeNames.Contains(su)) su = "auto";
        _cmbSizeUnit = new ComboBox { ItemsSource = sizeNames, SelectedItem = su };
        _cmbSizeUnit.SelectionChanged += (s, e) => { var v = _cmbSizeUnit.SelectedItem?.ToString() ?? "auto"; S.SizeUnit = v == "auto" ? "" : v; FilePane.SizeUnit = S.SizeUnit; S.Save(); };

        var addCodes = new[] { "never", "always", "sameSource", "sameDest", "both", "either" };
        _cmbAddWhen = new ComboBox();
        _cmbAddWhen.SelectedIndex = Math.Max(0, Array.IndexOf(addCodes, S.AddListsWhen));
        _cmbAddWhen.SelectionChanged += (s, e) => { S.AddListsWhen = addCodes[Math.Clamp(_cmbAddWhen.SelectedIndex, 0, addCodes.Length - 1)]; S.Save(); };

        _sldSpeed = new Slider { Minimum = 0, Maximum = 100, TickFrequency = 1, IsSnapToTickEnabled = true };
        _lblSpeed = MkLbl("", 12);
        _sldSpeed.Value = Math.Clamp((int)Math.Round(S.SpeedLimitKb / 1024.0), 0, 100);
        _lblSpeed.Text = ((int)_sldSpeed.Value).ToString() + " MB/s";
        _sldSpeed.ValueChanged += (s, e) => { _lblSpeed.Text = ((long)e.NewValue).ToString() + " MB/s"; _tbSpeed.Text = ((long)e.NewValue * 1024).ToString(); S.SpeedLimitKb = (long)e.NewValue * 1024; S.Save(); };

        _tbUpdate = new TextBox { Text = S.WindowUpdateMs.ToString(), Width = 100 };
        _tbUpdate.TextChanged += (s, e) => { if (int.TryParse(_tbUpdate.Text, out var v) && v >= 50) { S.WindowUpdateMs = v; _ticker.Interval = TimeSpan.FromMilliseconds(v); S.Save(); } };
        _tbAvg = new TextBox { Text = S.SpeedAvgMs.ToString(), Width = 100 };
        _tbAvg.TextChanged += (s, e) => { if (int.TryParse(_tbAvg.Text, out var v) && v >= 100) { S.SpeedAvgMs = v; S.Save(); Tick(); } };
        _tbThrottle = new TextBox { Text = S.ThrottleMs.ToString(), Width = 100 };
        _tbThrottle.TextChanged += (s, e) => { if (int.TryParse(_tbThrottle.Text, out var v) && v >= 0) { S.ThrottleMs = v; S.Save(); } };
        _tbWarn = new TextBox { Text = S.DiskWarnMb.ToString(), Width = 110 };
        _tbWarn.TextChanged += (s, e) => { if (long.TryParse(_tbWarn.Text, out var v) && v >= 0) { S.DiskWarnMb = v; S.Save(); } };
        _tbNewPat = new TextBox { Text = S.RenameNewPattern, Width = 240 };
        _tbNewPat.TextChanged += (s, e) => { S.RenameNewPattern = string.IsNullOrWhiteSpace(_tbNewPat.Text) ? "%NAME% (%COPY%)%EXT%" : _tbNewPat.Text; S.Save(); };

        _chkVerify = MkChk(Ex.Get("verify"), S.VerifyChecksum, b => { S.VerifyChecksum = b; S.Save(); });
        _chkActivate = MkChk(Ex.Get("activateOnStart"), S.ActivateOnStart, b => { S.ActivateOnStart = b; S.Save(); });
        _chkAskAdd = MkChk(Ex.Get("confirmAdd"), S.AskConfirm, b => { S.AskConfirm = b; S.Save(); });
        _chkLog = MkChk(Ex.Get("autoSaveLog"), S.SaveLog, b => { S.SaveLog = b; S.Save(); });

        var collCodes = new[] { "ask", "cancel", "skip", "resume", "overwrite", "overwriteIfDifferent", "renameNew", "renameOld" };
        var collNames = collCodes.Select(c => c switch { "ask" => L.Get("colAsk"), "cancel" => L.Get("colCancelAll"), "skip" => L.Get("skip"), "resume" => L.Get("resume"), "overwrite" => L.Get("overwrite"), "overwriteIfDifferent" => L.Get("overwriteDiff"), "renameNew" => L.Get("rename"), _ => L.Get("rename") }).ToArray();
        _cmbCollision = new ComboBox { ItemsSource = collNames, SelectedIndex = Math.Max(0, Array.IndexOf(collCodes, S.CollisionDefault)) };
        _cmbCollision.SelectionChanged += (s, e) => { S.CollisionDefault = collCodes[Math.Clamp(_cmbCollision.SelectedIndex, 0, collCodes.Length - 1)]; S.Save(); };

        var errCodes = new[] { "ask", "cancel", "skip", "retry", "bottom" };
        var errNames = errCodes.Select(c => c switch { "ask" => L.Get("errAsk"), "cancel" => L.Get("errCancelD"), "skip" => L.Get("skip"), "retry" => L.Get("retry"), _ => L.Get("retry") }).ToArray();
        _cmbError = new ComboBox { ItemsSource = errNames, SelectedIndex = Math.Max(0, Array.IndexOf(errCodes, S.ErrorDefault)) };
        _cmbError.SelectionChanged += (s, e) => { S.ErrorDefault = errCodes[Math.Clamp(_cmbError.SelectedIndex, 0, errCodes.Length - 1)]; S.Save(); };

        void Add(string key, Control c)
        {
            var lb = MkLbl(L.Get(key), 13);
            var row = new Grid { ColumnDefinitions = { new(GridLength.Auto), new(GridLength.Star) }, Margin = new Thickness(0, 2, 0, 2) };
            Grid.SetColumn(lb, 0);
            Grid.SetColumn(c, 1);
            row.Children.Add(lb);
            row.Children.Add(c);
            col.Children.Add(row);
            _texts.Add((key, () => { try { lb.Text = L.Get(key); } catch { } }));
        }

        void Sec(string key)
        {
            var t = MkLbl(L.Get(key), 13);
            t.FontWeight = FontWeight.Bold;
            t.Foreground = Gold;
            col.Children.Add(t);
            _texts.Add((key, () => { try { t.Text = L.Get(key); } catch { } }));
        }

        void AddEx(string key, Control c)
        {
            var lb = MkLbl(Ex.Get(key), 13);
            var row = new Grid { ColumnDefinitions = { new(GridLength.Auto), new(GridLength.Star) }, Margin = new Thickness(0, 2, 0, 2) };
            Grid.SetColumn(lb, 0);
            Grid.SetColumn(c, 1);
            row.Children.Add(lb);
            row.Children.Add(c);
            col.Children.Add(row);
            _texts.Add((key, () => { try { lb.Text = Ex.Get(key); } catch { } }));
        }

        void AddChk(CheckBox cb, string key)
        {
            col.Children.Add(cb);
            _texts.Add((key, () => { try { cb.Content = Ex.Get(key); } catch { } }));
        }

        Add("sLanguage", _cmbLang);
        Add("units", _cmbSizeUnit);
        Sec("afterDone"); col.Children.Add(_cmbAfter);
        Sec("speedLimit"); col.Children.Add(new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6, Children = { _chkLimit, _sldSpeed, _tbSpeed, _lblSpeed } });
        Sec("collisionDefault"); col.Children.Add(_cmbCollision);
        Sec("errorDefault"); col.Children.Add(_cmbError); Add("retryInterval", _tbRetry);
        Sec("addListsWhen"); col.Children.Add(_cmbAddWhen); AddChk(_chkAskAdd, "confirmAdd");
        Sec("rename"); col.Children.Add(_tbNewPat);
        Sec("sAdvanced");
        Add("engineType", _cmbEngine);
        Add("threads", new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6, Children = { _sldThreads, _lblThreads } });
        Add("buffer", new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6, Children = { _cmbBuffer, _tbBuffer } });
        Add("interval", _tbUpdate);
        Add("speedAvg", _tbAvg);
        Add("throttle", _tbThrottle);
        Add("priority", _cmbPriority);
        AddEx("diskWarn", _tbWarn);
        AddChk(_chkVerify, "verify");

        var checks = new StackPanel { Spacing = 4 };
        var chkTray = MkChk("", S.TrayIcon, b => { S.TrayIcon = b; S.Save(); BuildTray(); });
        var chkStart = MkChk("", S.StartWithWindows, b => { S.StartWithWindows = b; S.Save(); if (!OperatingSystem.IsAndroid()) ApplyStartWithWindows(b); });
        var chkAttrib = MkChk("", S.CopyAttributes, b => { S.CopyAttributes = b; S.Save(); });
        var chkSec = MkChk("", S.CopySecurity, b => { S.CopySecurity = b; S.Save(); });
        var chkDel = MkChk("", S.DeleteUnfinished, b => { S.DeleteUnfinished = b; S.Save(); });
        var chkKeep = MkChk("", S.KeepOnError, b => { S.KeepOnError = b; S.Save(); });
        var chkRO = MkChk("", S.OverwriteReadOnly, b => { S.OverwriteReadOnly = b; S.Save(); });
        var chkHidden = MkChk("", S.SkipHiddenSystem, b => { S.SkipHiddenSystem = b; S.Save(); });
        var chkTitle = MkChk("", S.ShowInTitle, b => { S.ShowInTitle = b; S.Save(); });
        void SecC(string key)
        {
            var t = MkLbl(L.Get(key), 13);
            t.FontWeight = FontWeight.Bold;
            t.Foreground = Gold;
            checks.Children.Add(t);
            _texts.Add((key, () => { try { t.Text = L.Get(key); } catch { } }));
        }
        void BindEx(CheckBox cb, string key)
        {
            checks.Children.Add(cb);
            _texts.Add((key, () => { try { cb.Content = Ex.Get(key); } catch { } }));
        }
        SecC("sStartup");
        Bind(chkStart, "startWithWindows");
        BindEx(_chkActivate, "activateOnStart");
        SecC("sUI");
        Bind(chkTray, "minToTray");
        Bind(chkTitle, "showInTitle");
        SecC("copyAttribs");
        Bind(chkAttrib, "copyAttribs");
        Bind(chkSec, "copySecurity");
        Bind(chkRO, "overwriteRO");
        Bind(chkHidden, "skipHidden");
        SecC("delUnfinished");
        Bind(chkDel, "delUnfinished");
        Bind(chkKeep, "keepOnError");
        SecC("sLog");
        BindEx(_chkLog, "autoSaveLog");
        Bind(_chkLimit, "enabled");

        var bottom = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Margin = new Thickness(0, 10, 0, 0) };
        var bSave = Mk("apply", () => { S.Save(); _lblStatus.Text = L.Get("ok"); });
        var bDef = Mk("sDefaults", () => { S = Settings.Load(); S.Save(); ReloadTexts(); });
        bottom.Children.Add(bSave);
        bottom.Children.Add(bDef);

        col.Children.Add(bottom);

        var about = new StackPanel { Spacing = 6, Margin = new Thickness(0, 16, 0, 0) };
        var aboutT = MkLbl("Acerca de QBasCopier", 13);
        aboutT.FontWeight = FontWeight.Bold;
        aboutT.Foreground = Gold;
        about.Children.Add(aboutT);
        try
        {
            using var s = AssetLoader.Open(new Uri("avares://QBasCopier/Assets/logo.png"));
            about.Children.Add(new Image { Source = new Bitmap(s), Width = 96, Height = 96, Stretch = Stretch.Uniform, Margin = new Thickness(0, 2, 0, 0) });
        }
        catch { }
        var aboutBody = MkLbl(AboutText, 12);
        aboutBody.TextWrapping = TextWrapping.Wrap;
        aboutBody.MaxWidth = 460;
        aboutBody.Tint(TextSoft);
        about.Children.Add(aboutBody);
        col.Children.Add(about);

        Grid.SetColumn(col, 0);
        g.Children.Add(col);
        Grid.SetColumn(checks, 1);
        g.Children.Add(checks);
        return g;
    }

    private Control MakeHistBody()
    {
        var panel = new StackPanel { Spacing = 6 };
        _lbHist = new ListBox { MinHeight = 240 };
        _lbHist.ItemTemplate = new FuncDataTemplate<HistoryEntry>((h, _ns) =>
        {
            var tp = new StackPanel { Spacing = 2 };
            tp.Children.Add(MkLbl($"{h.Time}  {h.Result}   {h.Source} → {h.Dest}", 12));
            tp.Children.Add(MkLbl(FilePane.Human(h.DoneBytes), 12).Tint(TextSoft));
            tp.Children.Add(new Border { Background = Line, Height = 1, Margin = new Thickness(0, 2, 0, 0) });
            return tp;
        });
        panel.Children.Add(_lbHist);
        var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        var bClr = Mk("histClear", async () => { await HistoryStore.ClearAsync(); RefreshHistory(); });
        var bRef = Mk("⟳", RefreshHistory);
        row.Children.Add(bClr);
        row.Children.Add(bRef);
        panel.Children.Add(row);
        return panel;
    }

    private static void WriteLog(string line)
    {
        try { File.AppendAllText(Path.Combine(Settings.Dir, "errors.log"), line + Environment.NewLine); } catch { }
    }

    private async void RefreshHistory()
    {
        _history.Clear();
        _history.AddRange(await HistoryStore.LoadAsync());
        _lbHist.ItemsSource = null;
        _lbHist.ItemsSource = _history;
    }

    private Control BuildStatusbar()
    {
        var bar = new Grid { ColumnDefinitions = { new(GridLength.Star), new(GridLength.Auto) }, Margin = new Thickness(8, 0, 8, 3) };
        _lblStatus = MkLbl("QBasCopier 1.0.0", 12);
        Grid.SetColumn(_lblStatus, 0);
        bar.Children.Add(_lblStatus);
        var ver = MkLbl("© 2026", 12);
        ver.HorizontalAlignment = HorizontalAlignment.Right;
        Grid.SetColumn(ver, 1);
        bar.Children.Add(ver);
        return bar;
    }

    // ------------------------------------------------------------------ bind/tex
    private TabItem MkTab(string tag, Control c) => new() { Tag = tag, Header = "…", Content = c };

    private void Bind(Control c, string key)
    {
        if (c is Button b) _texts.Add((key, () => { try { b.Content = L.Get(key); } catch { } }));
        else if (c is TextBlock t) _texts.Add((key, () => { try { t.Text = L.Get(key); } catch { } }));
        else if (c is CheckBox ch) _texts.Add((key, () => { try { ch.Content = L.Get(key); } catch { } }));
    }

    private void ApplyLang(string code)
    {
        S.Lang = Code(code);
        L.SetLanguage(S.Lang);
        S.Save();
        ReloadTexts();
    }

    public void ReloadTexts()
    {
        foreach (var (_, set) in _texts) { try { set(); } catch { } }
        if (_tabs != null)
            foreach (var o in _tabs.Items)
                if (o is TabItem ti)
                    ti.Header = ti.Tag?.ToString() switch
                    {
                        TabExplorer => Ex.Get("tabExplorer"),
                        TabQueue => L.Get("tabCopyList"),
                        TabErrors => L.Get("tabErrors"),
                        TabOptions => L.Get("tabInterface"),
                        TabHistory => L.Get("tabHistory"),
                        _ => ti.Header
                    };
        if (_lbErrs != null) { _lbErrs.ItemsSource = null; _lbErrs.ItemsSource = _errLines; }
        RefreshIntegrationButton();
        if (_bFold != null) _bFold.Content = Ex.Get("fold");
        if (_bQuit != null) _bQuit.Content = L.Get("quit");
        if (_lblStatus != null) _lblStatus.Text = StatusText();
        if (_cmbLang != null) _cmbLang.SelectedIndex = Ex.IndexOf(S.Lang);
        if (_cmbLangQuick != null) _cmbLangQuick.SelectedIndex = Ex.IndexOf(S.Lang);
        FilePane.SizeUnit = S.SizeUnit;
        RebuildOptionLists();
    }

    private void RebuildOptionLists()
    {
        try
        {
            if (_cmbAfter != null)
            {
                int i = _cmbAfter.SelectedIndex;
                _cmbAfter.ItemsSource = new[] { L.Get("afterClose"), L.Get("afterKeep"), L.Get("afterKeepErr") };
                _cmbAfter.SelectedIndex = i;
            }
            if (_cmbCollision != null)
            {
                int i = _cmbCollision.SelectedIndex;
                _cmbCollision.ItemsSource = new[] { L.Get("colAsk"), L.Get("colCancelAll"), L.Get("skip"), L.Get("resume"), L.Get("overwrite"), L.Get("overwriteDiff"), L.Get("rename"), L.Get("rename") };
                _cmbCollision.SelectedIndex = i;
            }
            if (_cmbError != null)
            {
                int i = _cmbError.SelectedIndex;
                _cmbError.ItemsSource = new[] { L.Get("errAsk"), L.Get("errCancelD"), L.Get("skip"), L.Get("retry"), L.Get("retry") };
                _cmbError.SelectedIndex = i;
            }
            if (_cmbAddWhen != null)
            {
                int i = _cmbAddWhen.SelectedIndex;
                _cmbAddWhen.ItemsSource = new[] { L.Get("lstNever"), L.Get("lstAlways"), L.Get("lstSameSource"), L.Get("lstSameDest"), L.Get("lstBoth"), L.Get("lstEither") };
                if (i < 0) i = Math.Max(0, Array.IndexOf(new[] { "never", "always", "sameSource", "sameDest", "both", "either" }, S.AddListsWhen));
                _cmbAddWhen.SelectedIndex = i;
            }
            if (_cmbBuffer != null)
            {
                int i = _cmbBuffer.SelectedIndex;
                _cmbBuffer.ItemsSource = new long[] { 64, 256, 1024, 4096, 16384, 65536 }.Select(x => $"{x} KB").ToArray();
                if (i >= 0) _cmbBuffer.SelectedIndex = i;
            }
        }
        catch { }
    }

    private void RefreshIntegrationButton()
    {
        if (_bIntegrate == null) return;
#if !ANDROID
        bool on = OperatingSystem.IsWindows() && ExplorerIntegration.IsInstalled;
#else
        bool on = false;
#endif
        _bIntegrate.Content = on ? Ex.Get("unintegrate") : Ex.Get("integrate");
    }

    private void ToggleIntegration()
    {
#if ANDROID
        return;
#else
        if (!OperatingSystem.IsWindows())
        {
            _lblStatus.Text = Ex.Get("integrate") + " (Linux/macOS)";
            return;
        }
        if (ExplorerIntegration.IsInstalled) ExplorerIntegration.Uninstall();
        else ExplorerIntegration.Install();
        RefreshIntegrationButton();
        _lblStatus.Text = L.Get("ok");
#endif
    }

    private static void ApplyStartWithWindows(bool on)
    {
#if !ANDROID
        if (OperatingSystem.IsWindows()) ExplorerIntegration.SetStartWithWindows(on);
#endif
    }

    private void ToggleFold()
    {
        if (_mini == null || _tabs == null) return;
        bool t = _tabs.IsVisible;
        _tabs.IsVisible = !t;
        _mini.IsVisible = !t;
        _bFold.Content = t ? Ex.Get("fold") : Ex.Get("fold");
    }

    private string StatusText()
    {
        var src = _tbFrom?.Text ?? "";
        var dst = _tbTo?.Text ?? "";
        var run = _running ? " · " + L.Get("done") : "";
        return $"{src} → {dst}{run}".Trim();
    }

    // ------------------------------------------------------------------ engine
    private void Tick()
    {
        if (_engine != null && (_engine.IsBusy || _running))
        {
            long done = _engine.DoneBytes, total = _engine.TotalBytes;
            long now = Environment.TickCount64;
            if (_lastTickTicks != 0)
            {
                long dt = now - _lastTickTicks;
                if (dt > 400)
                {
                    double inst = (done - _lastDone) * 1000.0 / dt;
                    _lastRate = _lastRate <= 0 ? inst : _lastRate * 0.6 + inst * 0.4;
                    _lastDone = done;
                }
            }
            _lastTickTicks = now;
            double pct = total > 0 ? Math.Min(100, done * 100.0 / total) : 0;
            _ggBar.Maximum = Math.Max(100, total);
            _ggBar.Value = done;
            _lblProg.Text = pct.ToString("0.0") + "%";
            _lblRate.Text = L.Get("speed") + ": " + FilePane.Human((long)_lastRate) + "/s";
            double left = _lastRate > 0 ? (total - done) / _lastRate : 0;
            _lblTime.Text = L.Get("remaining") + ": " + FmtTime(left) + " · " + L.Get("elapsed") + ": " + FmtTime(_batchSw.Elapsed.TotalSeconds);
            var cur = _engine.Items.FirstOrDefault(x => x.State == ItemState.Copying);
            _lblCur.Text = cur != null ? $"{L.Get("currentFile")}: {cur.Name}" : "";
            _miniLbl.Text = _lblProg.Text + "  " + _lblRate.Text;
            _miniBar.Value = pct;
            if (S.ShowInTitle) Title = $"QBasCopier · {pct:0.#}%";
        }
        else
        {
            _lastTickTicks = 0;
            if (S.ShowInTitle) Title = "QBasCopier";
        }

        var cmd = Program.WaitCommand(0);
        if (cmd != null) ProcessForwarded(cmd);
    }

    private static string FmtTime(double sec)
    {
        sec = Math.Max(0, sec);
        var ts = TimeSpan.FromSeconds(sec);
        return ts.TotalHours >= 1 ? $"{(int)ts.TotalHours}h {ts.Minutes}m {ts.Seconds}s" : $"{ts.Minutes}m {ts.Seconds}s";
    }

    // ------------------------------------------------------------------ cola
    private void AddFiles(string[] paths) => Enqueue(paths, _tbTo?.Text ?? "", false);
    private void AddWithDest(string[] paths, string dest) { AddFiles(paths); if (!string.IsNullOrEmpty(dest)) _tbTo.Text = dest; _tabs.SelectedIndex = 1; }

    private void Enqueue(string[] paths, string dest, bool move)
    {
        bool busy = _engine != null && _engine.IsBusy;
        if (busy && !MayAddWhileBusy(paths, dest))
        {
            if (S.AskConfirm) { _ = ConfirmAddAsync(paths, dest, move); return; }
            _lblStatus.Text = L.Get("busy");
            return;
        }
        if (_engine == null || !_engine.IsBusy) _engine = null;
        _tbFrom.Text = string.Join("; ", paths.Take(5)) + (paths.Length > 5 ? " …" : "");
        if (!string.IsNullOrEmpty(dest)) _tbTo.Text = dest;
        foreach (var p in paths)
        {
            try
            {
                bool isDir = Directory.Exists(p) && !File.Exists(p);
                if (isDir || File.Exists(p))
                    _queue.Add(new CopyItem { SourcePath = p, IsDirectory = isDir });
            }
            catch { }
        }
        _queue.Sort((a, b) => b.IsDirectory.CompareTo(a.IsDirectory));
    }

    private bool MayAddWhileBusy(string[] srcs, string dest)
    {
        switch (S.AddListsWhen)
        {
            case "never": return false;
            case "always": return true;
            case "sameSource": return srcs.All(s => SameRoot(s, _tbFrom.Text));
            case "sameDest": return SameRoot(dest, _tbTo.Text);
            case "both": return SameRoot(dest, _tbTo.Text) && srcs.All(s => SameRoot(s, _tbFrom.Text));
            case "either": return SameRoot(dest, _tbTo.Text) || srcs.All(s => SameRoot(s, _tbFrom.Text));
        }
        return true;
    }

    private static bool SameRoot(string a, string b)
    {
        try
        {
            return Path.GetPathRoot(a ?? "")?.TrimEnd('\\', '/') == Path.GetPathRoot(b ?? "")?.TrimEnd('\\', '/');
        }
        catch { return (a ?? "") == (b ?? ""); }
    }

    private async Task ConfirmAddAsync(string[] paths, string dest, bool move)
    {
        var bOk = new Button { Content = L.Get("ok") };
        var bNo = new Button { Content = L.Get("cancel") };
        var w = new Window
        {
            Width = 380, Height = 170, CanResize = false,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Title = L.Get("addListsWhen"),
            Content = new StackPanel
            {
                Margin = new Thickness(16), Spacing = 14,
                Children =
                {
                    new TextBlock { Text = L.Get("askConfirm"), TextWrapping = TextWrapping.Wrap, FontSize = 13 },
                    new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, HorizontalAlignment = HorizontalAlignment.Right, Children = { bOk, bNo } }
                }
            }
        };
        bOk.Click += (_, _) => w.Close(true);
        bNo.Click += (_, _) => w.Close(false);
        var res = await w.ShowDialog<bool>(this);
        if (res) Enqueue(paths, dest, move);
    }

    private void RefreshQueueUi()
    {
        if (_engine == null) return;
        foreach (var it in _queue)
            if (_engine.Items.Contains(it) == false)
                _engine.Items.Add(it);
        _lbQueue.ItemsSource = _queue;
        _lblStatus.Text = StatusText();
    }

    private void Pause() { _engine?.Pause(); _bPause.IsEnabled = false; _bResume.IsEnabled = true; }
    private void Resume() { _engine?.Resume(); _bPause.IsEnabled = true; _bResume.IsEnabled = false; }

    private void SkipRest()
    {
        if (_engine == null) return;
        foreach (var it in _engine.Items)
            if (it.State == ItemState.Ready) { it.State = ItemState.Skipped; it.StateText = L.Get("stateSkipped"); }
    }

    private void Clear()
    {
        if (_engine?.IsBusy == true) return;
        _queue.Clear();
        _engine?.Items.Clear();
        _lbQueue.ItemsSource = null;
        _ggBar.Value = 0;
        _lblProg.Text = "0%";
        _lblRate.Text = _lblTime.Text = _lblCur.Text = "";
    }

    private async Task StartCopy(bool move)
    {
        if (_queue.Count == 0) { _lblStatus.Text = L.Get("histEmpty"); return; }
        var dest = _tbTo.Text?.Trim();
        if (string.IsNullOrEmpty(dest))
        {
            _lblStatus.Text = L.Get("destination") + "?";
            return;
        }
        try { Directory.CreateDirectory(dest); } catch { _lblStatus.Text = L.Get("errTitle"); return; }

        if (S.DiskWarnMb > 0)
        {
            try
            {
                long needed = 0;
                foreach (var q in _queue)
                    if (!q.IsDirectory && File.Exists(q.SourcePath))
                        needed += new FileInfo(q.SourcePath).Length;
                var drv = new DriveInfo(Path.GetPathRoot(Path.GetFullPath(dest).TrimEnd('/', '\\')) ?? dest);
                if (drv.AvailableFreeSpace - needed < S.DiskWarnMb * 1024L * 1024L)
                    _lblStatus.Text = Ex.Get("warnSpace");
            }
            catch { }
        }

        if (_engine == null || _engine.IsBusy)
        {
            _engine = new CopyEngine(S);
            _engine.AskCollision = PickCollisionAsync;
            _engine.AskError = PickErrorAsync;
            _engine.LogMessage += line => _ = Dispatcher.UIThread.InvokeAsync(() => { _errLines.Add(line); _lbErrs.ItemsSource = null; _lbErrs.ItemsSource = _errLines; if (S.SaveLog) WriteLog(line); });
            _engine.BatchEnd += (ok, err, cancelled, paused) => _ = Dispatcher.UIThread.InvokeAsync(() => OnBatchEnd(ok, err, cancelled));
        }
        RefreshQueueUi();

        foreach (var it in _queue)
            it.DestPath = Path.Combine(dest, Path.GetFileName(it.SourcePath.TrimEnd('/', '\\')));

        _engine.Move = move;
        _batchSw.Restart();
        _lastDone = 0;
        _lastRate = 0;
        _lastTickTicks = 0;
        _running = true;
        _bCopy.IsEnabled = _bMove.IsEnabled = false;
        _bPause.IsEnabled = true;
        _bResume.IsEnabled = false;
        _bSkip.IsEnabled = _bCancel.IsEnabled = true;
        _tabs.SelectedIndex = 1;

        try { await _engine.RunAsync(); }
        catch { }

        _running = false;
        _bCopy.IsEnabled = _bMove.IsEnabled = true;
        _bPause.IsEnabled = _bResume.IsEnabled = false;
        _bSkip.IsEnabled = _bCancel.IsEnabled = false;
        _lblStatus.Text = L.Get("done");
    }

    private async void OnBatchEnd(int ok, int err, bool cancelled)
    {
        var done = _engine?.DoneBytes ?? 0;
        _engine = null;
        await HistoryStore.AppendAsync(_tbFrom.Text ?? "", _tbTo.Text ?? "", cancelled ? L.Get("histCancelled") : err > 0 ? L.Get("histErrors") : L.Get("histOk"), done);
        RefreshHistory();
        if (S.AfterDone == "close" || (S.AfterDone == "keepIfErrors" && err == 0)) DoQuit();
        else BuildTray();
    }

    private void DoQuit()
    {
        _forceClose = true;
        try { S.Save(); } catch { }
        Close();
    }

    // ----------------------------------------------------------- diálogos
    private async Task<CollisionDecision> PickCollisionAsync(CopyItem it, string target)
    {
        var res = new CollisionDecision(CopyAction.Overwrite, false);
        var w = new Window { Width = 640, Height = 360, Title = L.Get("colTitle"), Background = BgPanel, WindowStartupLocation = WindowStartupLocation.CenterOwner, ShowInTaskbar = false };
        var sp = new StackPanel { Margin = new Thickness(14), Spacing = 8 };
        sp.Children.Add(MkLbl(L.Get("colTitle") + ":", 15));
        sp.Children.Add(MkLbl(it.SourcePath, 12).Tint(TextSoft));
        sp.Children.Add(MkLbl("→", 14).Tint(Gold));
        sp.Children.Add(MkLbl(target, 12).Tint(TextSoft));
        var chkAll = MkChk("", false, null);
        Bind(chkAll, "allfiles");
        sp.Children.Add(chkAll);
        var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6, Margin = new Thickness(0, 10, 0, 0) };
        foreach (var (a, k) in new (CopyAction, string)[] { (CopyAction.Overwrite, "overwrite"), (CopyAction.OverwriteIfDifferent, "overwriteDiff"), (CopyAction.Resume, "resume"), (CopyAction.Rename, "rename"), (CopyAction.Skip, "skip"), (CopyAction.CancelAll, "cancel") })
        {
            var b = Mk("…", () => { res = new CollisionDecision(a, chkAll.IsChecked == true); w.Close(); });
            b.Content = L.Get(k);
            row.Children.Add(b);
        }
        sp.Children.Add(row);
        w.Content = new Border { BorderBrush = Line, BorderThickness = new Thickness(1), Child = sp };
        await Dispatcher.UIThread.InvokeAsync(() => w.ShowDialog(this));
        return res;
    }

    private async Task<ErrorDecision> PickErrorAsync(CopyItem it, string msg)
    {
        var res = new ErrorDecision(CopyAction.Skip, false);
        var w = new Window { Width = 640, Height = 320, Title = L.Get("errTitle"), Background = BgPanel, WindowStartupLocation = WindowStartupLocation.CenterOwner, ShowInTaskbar = false };
        var sp = new StackPanel { Margin = new Thickness(14), Spacing = 8 };
        sp.Children.Add(MkLbl(it.SourcePath, 12).Tint(TextSoft));
        sp.Children.Add(MkLbl(msg, 12).Tint(Red));
        var chkAll = MkChk("", false, null);
        Bind(chkAll, "allfiles");
        sp.Children.Add(chkAll);
        var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6, Margin = new Thickness(0, 10, 0, 0) };
        foreach (var (a, k) in new (CopyAction, string)[] { (CopyAction.Retry, "retry"), (CopyAction.Skip, "skip"), (CopyAction.CancelAll, "cancel") })
        {
            var b = Mk("…", () => { res = new ErrorDecision(a, chkAll.IsChecked == true); w.Close(); });
            b.Content = L.Get(k);
            row.Children.Add(b);
        }
        sp.Children.Add(row);
        w.Content = new Border { BorderBrush = Line, BorderThickness = new Thickness(1), Child = sp };
        await Dispatcher.UIThread.InvokeAsync(() => w.ShowDialog(this));
        return res;
    }

    // ----------------------------------------------------------- explorador
    private void PaneCopy(bool toLeft) => Transfer((toLeft ? _right : _left).SelectedPaths, (toLeft ? _left : _right).CurrentPath, false);
    private void PaneMove(bool toLeft) => Transfer((toLeft ? _right : _left).SelectedPaths, (toLeft ? _left : _right).CurrentPath, true);

    private void Transfer(List<string> paths, string dest, bool move)
    {
        if (paths.Count == 0 || string.IsNullOrEmpty(dest)) return;
        _tabs.SelectedIndex = 1;
        Enqueue(paths.ToArray(), dest, move);
    }

    private void ProcessForwarded(string args)
    {
        var parts = args.Split('\n').Where(x => x.Length > 0).ToArray();
        var paths = new List<string>();
        string? dest = null;
        bool after = false, move = false;
        for (int i = 0; i < parts.Length; i++)
        {
            if (parts[i] == "--copy") after = true;
            else if (parts[i] == "--move") move = true;
            else if (parts[i] == "--dest" && i + 1 < parts.Length) dest = parts[++i];
            else if (after && !parts[i].StartsWith("--")) paths.Add(parts[i]);
        }
        if (paths.Count > 0) Enqueue(paths.ToArray(), dest ?? "", move);
        if (!string.IsNullOrEmpty(dest)) _tbTo.Text = dest;
    }

    // ----------------------------------------------------------- bandeja
    private void BuildTray()
    {
#if !ANDROID
        try
        {
            if (_tray != null) { _tray.IsVisible = false; _tray.Dispose(); _tray = null; }
            if (!S.TrayIcon || _logoBmp == null) return;
            _tray = new TrayIcon { Icon = new WindowIcon(_logoBmp), ToolTipText = "QBasCopier", IsVisible = true };
            _tray.Menu = new NativeMenu();
            var mOpen = new NativeMenuItem("QBasCopier");
            mOpen.Click += (s, e) => { Show(); Activate(); };
            var mQuit = new NativeMenuItem(L.Get("quit"));
            mQuit.Click += (s, e) => DoQuit();
            _tray.Menu.Items.Add(mOpen);
            _tray.Menu.Items.Add(new NativeMenuItemSeparator());
            _tray.Menu.Items.Add(mQuit);
        }
        catch { }
#endif
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        if (!_forceClose && S.MinimizeTo == "tray" && _tray != null)
        {
            e.Cancel = true;
            Hide();
            return;
        }
        try { S.Save(); } catch { }
        base.OnClosing(e);
    }

    // ----------------------------------------------------------- util
    private static Button Mk(string text, Action? act)
    {
        var b = new Button { Content = L.Get(text), FontSize = 12, Margin = new Thickness(2) };
        if (act != null) b.Click += (s, e) => act();
        return b;
    }

    private static TextBlock MkLbl(string t, int size) => new() { Text = t, FontSize = size, Foreground = TextMain, VerticalAlignment = VerticalAlignment.Center };

    private static CheckBox MkChk(string content, bool init, Action<bool>? on)
    {
        var cb = new CheckBox { Content = content, IsChecked = init, VerticalAlignment = VerticalAlignment.Center };
        if (on != null) cb.Click += (s, e) => on(cb.IsChecked == true);
        return cb;
    }

    private void PickDirInto(TextBox tb) => _ = PickDirAsync(tb);
    private async Task PickDirAsync(TextBox tb)
    {
        var dlg = new OpenFolderDialog { Title = L.Get("browse") };
        var dir = await dlg.ShowAsync(this);
        if (!string.IsNullOrEmpty(dir)) tb.Text = dir;
    }

    private void PickFiles() => _ = PickFilesAsync();
    private async Task PickFilesAsync()
    {
        var dlg = new OpenFileDialog { Title = L.Get("addFiles"), AllowMultiple = true };
        var files = await dlg.ShowAsync(this);
        if (files != null && files.Length > 0) AddFiles(files);
    }
}

public static class Ext
{
    public static T Also<T>(this T o, Action<T> a) { a(o); return o; }
    public static TextBlock Tint(this TextBlock t, IBrush b) { t.Foreground = b; return t; }
}

// Textos adicionales de la interfaz (20 idiomas)
public static class Ex
{
    public static readonly string[] Codes =
    {
        "es", "en", "fr", "pt", "it", "de", "nl", "ru", "uk", "pl",
        "tr", "cs", "ro", "hi", "ar", "zh", "ja", "ko", "id", "vi"
    };

    // 0 tabExplorer, 1 from, 2 to, 3 fold, 4 integrate, 5 unintegrate,
    // 6 verify, 7 activateOnStart, 8 diskWarn, 9 confirmAdd, 10 autoSaveLog, 11 warnSpace
    private static readonly string[][] Rows =
    {
        new[] { "Explorador", "Origen", "Destino", "Plegar", "Integrar en el sistema", "Quitar del sistema", "Verificar integridad (SHA-256)", "Activar ventana al iniciar", "Aviso de espacio mínimo (MB)", "Preguntar antes de añadir", "Guardar log de errores automático", "Poco espacio libre en el disco" },
        new[] { "Browser", "From", "To", "Collapse", "Integrate into system", "Remove from system", "Verify integrity (SHA-256)", "Activate window on startup", "Minimum free-space warning (MB)", "Ask before adding", "Save error log automatically", "Low free disk space" },
        new[] { "Explorateur", "Source", "Destination", "Replier", "Intégrer au système", "Retirer du système", "Vérifier l'intégrité (SHA-256)", "Activer la fenêtre au démarrage", "Avertissement d'espace libre (Mo)", "Demander avant d'ajouter", "Enregistrer le journal des erreurs", "Espace disque libre faible" },
        new[] { "Explorador", "Origem", "Destino", "Recolher", "Integrar ao sistema", "Remover do sistema", "Verificar integridade (SHA-256)", "Ativar janela ao iniciar", "Aviso de espaço mínimo (MB)", "Perguntar antes de adicionar", "Salvar log de erros automaticamente", "Pouco espaço livre no disco" },
        new[] { "Browser", "Origine", "Destinazione", "Comprimi", "Integra nel sistema", "Rimuovi dal sistema", "Verifica integrità (SHA-256)", "Attiva finestra all'avvio", "Avviso spazio minimo (MB)", "Chiedi prima di aggiungere", "Salva log errori automaticamente", "Spazio su disco basso" },
        new[] { "Explorer", "Quelle", "Ziel", "Einklappen", "In System integrieren", "Aus System entfernen", "Integrität prüfen (SHA-256)", "Fenster beim Start aktivieren", "Mindestplatz-Warnung (MB)", "Vor dem Hinzufügen fragen", "Fehlerlog automatisch speichern", "Wenig freier Speicherplatz" },
        new[] { "Verkenner", "Bron", "Doel", "Inklappen", "Integreren in systeem", "Uit systeem verwijderen", "Integriteit controleren (SHA-256)", "Venster activeren bij start", "Waarschuwing minimale ruimte (MB)", "Vragen voor toevoegen", "Foutenlog automatisch opslaan", "Weinig vrije schijfruimte" },
        new[] { "Проводник", "Источник", "Назначение", "Свернуть", "Интегрировать в систему", "Убрать из системы", "Проверять целостность (SHA-256)", "Показывать окно при запуске", "Предупреждение о свободном месте (МБ)", "Спрашивать перед добавлением", "Автосохранение журнала ошибок", "Мало свободного места на диске" },
        new[] { "Провідник", "Джерело", "Призначення", "Згорнути", "Інтегрувати в систему", "Прибрати з системи", "Перевіряти цілісність (SHA-256)", "Показувати вікно при запуску", "Попередження про вільне місце (МБ)", "Питати перед додаванням", "Автозбереження журналу помилок", "Мало вільного місця на диску" },
        new[] { "Eksplorator", "Źródło", "Cel", "Zwiń", "Zintegruj z systemem", "Usuń z systemu", "Sprawdzaj integralność (SHA-256)", "Aktywuj okno przy starcie", "Ostrzeżenie o wolnej przestrzeni (MB)", "Pytaj przed dodaniem", "Automatycznie zapisuj log błędów", "Mało wolnego miejsca na dysku" },
        new[] { "Gezgin", "Kaynak", "Hedef", "Daralt", "Sisteme entegre et", "Sistemden kaldır", "Bütünlüğü doğrula (SHA-256)", "Başlangıçta pencereyi etkinleştir", "Minimum boş alan uyarısı (MB)", "Eklerken sor", "Hata günlüğünü otomatik kaydet", "Diski az yer kaldı" },
        new[] { "Průzkumník", "Zdroj", "Cíl", "Sbalit", "Integrovat do systému", "Odebrat ze systému", "Ověřit integritu (SHA-256)", "Aktivovat okno při startu", "Upozornění na volné místo (MB)", "Zeptat se před přidáním", "Uložit log chyb automaticky", "Málo volného místa na disku" },
        new[] { "Explorator", "Sursă", "Destinație", "Restrânge", "Integrează în sistem", "Elimină din sistem", "Verifică integritatea (SHA-256)", "Activează fereastra la pornire", "Avertisment spațiu minim (MB)", "Întreabă înainte de adăugare", "Salvează automat logul de erori", "Puțin spațiu liber pe disc" },
        new[] { "ब्राउज़र", "स्रोत", "गंतव्य", "संक्षिप्त करें", "सिस्टम में एकीकृत करें", "सिस्टम से हटाएँ", "अखंडता सत्यापित करें (SHA-256)", "प्रारंभ पर विंडो सक्रिय करें", "न्यूनतम स्थान चेतावनी (MB)", "जोड़ने से पहले पूछें", "त्रुटि लॉग स्वतः सहेजें", "डिस्क में कम जगह" },
        new[] { "المتصفح", "المصدر", "الوجهة", "طي", "دمج في النظام", "إزالة من النظام", "التحقق من السلامة (SHA-256)", "تنشيط النافذة عند بدء التشغيل", "تحذير المساحة الحرة (MB)", "اسأل قبل الإضافة", "حفظ سجل الأخطاء تلقائيًا", "مساحة قرص منخفضة" },
        new[] { "浏览器", "源", "目标", "折叠", "集成到系统", "从系统移除", "验证完整性 (SHA-256)", "启动时激活窗口", "最小剩余空间警告 (MB)", "添加前询问", "自动保存错误日志", "磁盘可用空间不足" },
        new[] { "エクスプローラー", "送信元", "宛先", "折りたたむ", "システムに統合", "システムから削除", "整合性を検証 (SHA-256)", "起動時にウィンドウを表示", "空き容量警告 (MB)", "追加前に確認", "エラーログを自動保存", "ディスク空き容量が少ない" },
        new[] { "탐색기", "소스", "대상", "접기", "시스템에 통합", "시스템에서 제거", "무결성 검증 (SHA-256)", "시작 시 창 활성화", "최소 여유 공간 경고 (MB)", "추가하기 전에 물어보기", "오류 로그 자동 저장", "디스크 여유 공간 부족" },
        new[] { "Penjelajah", "Sumber", "Tujuan", "Ciutkan", "Integrasikan ke sistem", "Hapus dari sistem", "Verifikasi integritas (SHA-256)", "Aktifkan jendela saat mulai", "Peringatan ruang minimum (MB)", "Tanya sebelum menambah", "Simpan log kesalahan otomatis", "Ruang disk rendah" },
        new[] { "Trình duyệt", "Nguồn", "Đích", "Thu gọn", "Tích hợp vào hệ thống", "Xóa khỏi hệ thống", "Xác minh tính toàn vẹn (SHA-256)", "Kích hoạt cửa sổ khi khởi động", "Cảnh báo dung lượng tối thiểu (MB)", "Hỏi trước khi thêm", "Tự động lưu nhật ký lỗi", "Ít dung lượng ổ đĩa trống" },
    };

    public static int IndexOf(string code)
    {
        var i = Array.IndexOf(Codes, code.ToLowerInvariant());
        return i < 0 ? 0 : i;
    }

    public static string[] LangNames() => Rows.Select(r => r[0]).ToArray();

    public static string Get(string key)
    {
        var idx = key switch
        {
            "tabExplorer" => 0,
            "from" => 1,
            "to" => 2,
            "fold" => 3,
            "integrate" => 4,
            "unintegrate" => 5,
            "verify" => 6,
            "activateOnStart" => 7,
            "diskWarn" => 8,
            "confirmAdd" => 9,
            "autoSaveLog" => 10,
            "warnSpace" => 11,
            _ => -1
        };
        if (idx < 0) return key;
        var r = Rows[Math.Clamp(L.Current, 0, Rows.Length - 1)];
        return idx < r.Length ? r[idx] : key;
    }
}