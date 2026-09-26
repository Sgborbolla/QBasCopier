using Microsoft.Win32;
using QBasCopierSetup;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace QBasCopierSetup;

public partial class MainWindow : Window
{
    private readonly ComboBox _cmb = new();
    private readonly TextBox _tbDest = new();
    private readonly CheckBox _chkIntegrate = new(), _chkStartup = new(), _chkDesktop = new();
    private readonly ProgressBar _pb = new();
    private readonly TextBlock _lblStep = new();
    private readonly Button _btnMain = new(), _btnDone = new();
    private readonly System.Windows.Media.MediaPlayer _music = new();
    private bool _finished;
    private string? _musicFile;

    public MainWindow()
    {
        InitializeComponent();
        Build();
        Loaded += (s, e) => PlayMusic();
        Closed += (s, e) => StopMusic();
    }

    private void Build()
    {
        var grid = new Grid { Background = Brushes("#05091C") };
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });

        var header = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(16, 12, 16, 6) };
        var img = new Image { Width = 72, Height = 72, Stretch = Stretch.Uniform };
        try
        {
            using var s = Assembly.GetExecutingAssembly().GetManifestResourceStream("QBasCopierSetup.Assets.logo.png");
            if (s != null)
            {
                var bmp = new BitmapImage();
                bmp.BeginInit(); bmp.StreamSource = s; bmp.EndInit();
                bmp.Freeze();
                img.Source = bmp;
            }
        }
        catch { }
        header.Children.Add(img);
        var title = new StackPanel { Margin = new Thickness(14, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center };
        title.Children.Add(new TextBlock { Text = "QBasCopier", FontSize = 26, FontWeight = FontWeights.Bold, Foreground = Brushes("#FBBF24") });
        title.Children.Add(new TextBlock { Text = "© 2026", FontSize = 13, Foreground = Brushes("#9FB3E8") });
        header.Children.Add(title);
        Grid.SetRow(header, 0);
        grid.Children.Add(header);

        var panel = new StackPanel { Margin = new Thickness(18, 6, 18, 6) };
        _cmb.Width = 320; _cmb.HorizontalAlignment = HorizontalAlignment.Left;
        panel.Children.Add(MakeLabel("idioma", 0));
        panel.Children.Add(_cmb);
        panel.Children.Add(MakeLabel("destino", 0));
        var destRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 2, 0, 0) };
        _tbDest.Text = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "QBasCopier");
        _tbDest.Width = 440;
        var bBrowse = new Button { Content = "…", Width = 36, VerticalAlignment = VerticalAlignment.Center };
        bBrowse.Click += (s, e) =>
        {
            var d = new OpenFolderDialog { Title = "…" };
            if (d.ShowDialog(this) == true) _tbDest.Text = d.FolderName;
        };
        destRow.Children.Add(_tbDest);
        destRow.Children.Add(bBrowse);
        panel.Children.Add(destRow);

        _chkIntegrate.IsChecked = true; _chkStartup.IsChecked = false; _chkDesktop.IsChecked = true;
        panel.Children.Add(MakeLabel("opciones", 0));
        foreach (var c in new[] { _chkIntegrate, _chkStartup, _chkDesktop }) { c.Foreground = Brushes("#F6F8FF"); panel.Children.Add(c); }

        _pb.Height = 18; _pb.Margin = new Thickness(0, 10, 0, 4);
        panel.Children.Add(_pb);
        _lblStep.Foreground = Brushes("#9FB3E8"); _lblStep.TextWrapping = TextWrapping.Wrap;
        panel.Children.Add(_lblStep);

        var btns = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 10, 0, 0) };
        _btnMain.Content = "install"; _btnMain.Width = 150; _btnMain.Background = Brushes("#CF142B"); _btnMain.Foreground = Brushes("#FFFFFF");
        _btnMain.Click += async (s, e) => await Install();
        _btnDone.Content = "launch"; _btnDone.Width = 150; _btnDone.IsEnabled = false; _btnDone.Visibility = Visibility.Collapsed;
        _btnDone.Background = Brushes("#FBBF24"); _btnDone.Foreground = Brushes("#05091C");
        _btnDone.Click += (s, e) => { try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(_destExe) { UseShellExecute = true }); } catch { } Close(); };
        btns.Children.Add(_btnMain);
        btns.Children.Add(_btnDone);
        panel.Children.Add(btns);
        Grid.SetRow(panel, 1);
        grid.Children.Add(panel);

        var footer = new TextBlock { Text = MachineText(), Foreground = Brushes("#9FB3E8"), Margin = new Thickness(16, 0, 16, 8) };
        Grid.SetRow(footer, 2);
        grid.Children.Add(footer);

        // marca de agua
        var wm = new TextBlock { Text = "QBasCopier", FontSize = 96, FontWeight = FontWeights.Bold, Foreground = Brushes("#6FB1FC"), Opacity = 0.05, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, IsHitTestVisible = false };
        Grid.SetRow(wm, 1);
        grid.Children.Add(wm);

        Root.Children.Add(grid);
        ApplyTexts();
    }

    private string? _destExe;

    private static string MachineText()
    {
        var n = System.Environment.MachineName.Trim();
        return $"QBasCopier · {n} · © 2026";
    }

    private TextBlock MakeLabel(string key, int idx)
    {
        var t = new TextBlock { Foreground = Brushes("#F6F8FF"), FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 10, 0, 2) };
        _labelKeys.Add((t, key));
        return t;
    }

    private readonly List<(TextBlock tb, string key)> _labelKeys = new();

    private static SolidColorBrush Brushes(string hex) => new((Color)ColorConverter.ConvertFromString(hex));

    private void ApplyTexts()
    {
        var i = SetupLoc.Current;
        _cmb.ItemsSource = SetupLoc.Native;
        _cmb.SelectedIndex = i;
        foreach (var (tb, key) in _labelKeys)
            tb.Text = key switch { "idioma" => SetupLoc.Get(1), "destino" => SetupLoc.Get(2), "opciones" => SetupLoc.Get(4), _ => key };
        _btnMain.Content = SetupLoc.Get(5);
        _lblStep.Text = SetupLoc.Get(13);
        _chkIntegrate.Content = SetupLoc.Get(10);
        _chkStartup.Content = SetupLoc.Get(11);
        _chkDesktop.Content = SetupLoc.Get(12);
        Title = SetupLoc.Get(0);
    }

    private void PlayMusic()
    {
        try
        {
            var rs = Assembly.GetExecutingAssembly().GetManifestResourceStream("QBasCopierSetup.Assets.music.mp3");
            if (rs == null) return;
            _musicFile = Path.Combine(Path.GetTempPath(), "qbas-copier-music.mp3");
            if (!File.Exists(_musicFile))
            {
                using var fs = File.Create(_musicFile);
                rs.CopyTo(fs);
            }
            _music.MediaEnded += (s, e) => { _music.Position = TimeSpan.Zero; _music.Play(); };
            _music.Open(new Uri(_musicFile));
            _music.Volume = 0.7;
            _music.Play();
        }
        catch { }
    }

    private void StopMusic()
    {
        try { _music.Stop(); _music.Close(); } catch { }
        try { if (_musicFile != null && File.Exists(_musicFile)) File.Delete(_musicFile); } catch { }
    }

    private void SetUi(int pct, string step)
    {
        Dispatcher.Invoke(() => { _pb.Value = pct; _lblStep.Text = step; });
    }

    private async Task Install()
    {
        if (_finished) return;
        _btnMain.IsEnabled = false;
        try
        {
            var dest = _tbDest.Text.Trim();
            Directory.CreateDirectory(dest);
            var code = SetupLoc.Codes[Math.Max(0, _cmb.SelectedIndex)];

            var settingsDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "QBasCopier");
            Directory.CreateDirectory(settingsDir);
            File.WriteAllText(Path.Combine(settingsDir, "settings.json"),
                JsonSerializer.Serialize(new { Lang = code }, new JsonSerializerOptions { WriteIndented = true }));

            _destExe = Path.Combine(dest, "QBasCopier&Transfer.exe");
            SetUi(15, SetupLoc.Get(6));
            await Task.Run(() =>
            {
                using var rs = Assembly.GetExecutingAssembly().GetManifestResourceStream("QBasCopierSetup.Assets.app.exe")
                    ?? throw new IOException("app.exe missing");
                using var fs = new FileStream(_destExe, FileMode.Create);
                rs.CopyTo(fs);
            });
            SetUi(55, SetupLoc.Get(6));

            if (_chkDesktop.IsChecked == true) CreateDesktopShortcut();
            SetUi(70, SetupLoc.Get(6));

            if (_chkIntegrate.IsChecked == true || _chkStartup.IsChecked == true)
            {
                var args = $"{( _chkIntegrate.IsChecked == true ? "--install-integration " : "")}{(_chkStartup.IsChecked == true ? "--start-with-windows " : "")}--lang {code}";
                try
                {
                    using var p = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(_destExe!, args) { UseShellExecute = false, CreateNoWindow = true });
                    if (p != null) { p.WaitForInputIdle(800); try { p.Kill(); } catch { } }
                }
                catch { }
            }
            SetUi(100, SetupLoc.Get(7));
            _finished = true;
            _btnMain.Visibility = Visibility.Collapsed;
            _btnDone.Visibility = Visibility.Visible;
            _btnDone.IsEnabled = true;
            _btnDone.Content = SetupLoc.Get(8);
        }
        catch (Exception ex)
        {
            _lblStep.Text = SetupLoc.Get(7) + "\n" + ex.Message;
            _btnMain.IsEnabled = true;
        }
    }

    private void CreateDesktopShortcut()
    {
        try
        {
            var desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            var lnk = Path.Combine(desktop, "QBasCopier.lnk");
            var t = Type.GetTypeFromProgID("WScript.Shell");
            if (t == null) return;
            dynamic shell = Activator.CreateInstance(t)!;
            dynamic sc = shell.CreateShortcut(lnk);
            sc.TargetPath = _destExe!;
            sc.IconLocation = $"{_destExe},0";
            sc.Save();
        }
        catch { }
    }
}