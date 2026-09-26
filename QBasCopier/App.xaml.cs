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
#if ANDROID
            else if (ApplicationLifetime is ISingleViewApplicationLifetime singleView)
            {
                // En Android el lifetime es un SingleViewLifetime y la activity hace
                // SetContentView(lifetime.MainView). Sin esto no hay nada que mostrar:
                // lienzo blanco, sin crash. Es lo que pasaba.
                CrashLog.Info("creating MainView (Android)");
                singleView.MainView = new MainView();
                CrashLog.Info("MainView assigned");
            }
#endif
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
                Background = Brushes.DarkSlateGray,
                Margin = new Thickness(12)
            };
#if ANDROID
            // En Android una Window nueva no se muestra: hay que poner el error en el
            // MainView, si no vuelve a quedar una pantalla en blanco sin explicacion.
            if (Application.Current?.ApplicationLifetime is ISingleViewApplicationLifetime sv)
            {
                sv.MainView = new ScrollViewer { Content = tb };
                return;
            }
#endif
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