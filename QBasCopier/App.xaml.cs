using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Media;

namespace QBasCopier;

public partial class App : Application
{
    public override void Initialize()
    {
        CrashLog.Info("App.Initialize");
        AvaloniaXamlLoader.Load(this);
        CrashLog.Info("App.Initialize XAML ok");
    }

    public override void OnFrameworkInitializationCompleted()
    {
        try
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;
                CrashLog.Info("creating MainWindow");
                var w = new MainWindow();
                CrashLog.Info("MainWindow created");
                w.Closed += (_, _) => desktop.Shutdown();
                desktop.MainWindow = w;
                w.InitialBoot();
                CrashLog.Info("InitialBoot done");
            }
        }
        catch (Exception ex)
        {
            CrashLog.Save("FATAL " + ex);
            ShowFatal(ex);
        }
        base.OnFrameworkInitializationCompleted();
    }

    private static void ShowFatal(Exception ex)
    {
        try
        {
            var tb = new TextBlock
            {
                Text = ex.ToString(),
                TextWrapping = TextWrapping.Wrap,
                Foreground = Brushes.White,
                Margin = new Thickness(12)
            };
            var win = new Window
            {
                Title = "QBasCopier - ERROR de arranque",
                Width = 560,
                Height = 700,
                Content = new ScrollViewer { Content = tb }
            };
            win.Show();
        }
        catch { }
    }
}