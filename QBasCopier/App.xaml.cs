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
#if !ANDROID
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;
                CrashLog.Info("creating DesktopWindow");
                var w = new DesktopWindow();
                CrashLog.Info("DesktopWindow created");
                w.Closed += (_, _) => desktop.Shutdown();
                desktop.MainWindow = w;
                w.Content.As<MainWindow>()?.InitialBoot();
                CrashLog.Info("InitialBoot done");
            }
#else
            if (ApplicationLifetime is ISingleViewApplicationLifetime singleView)
            {
                // En Android el lifetime es un SingleViewLifetime y la activity hace
                // SetContentView(lifetime.MainView). Sin esto no hay nada que mostrar:
                // lienzo blanco, sin crash.
                CrashLog.Info("creating MainWindow (Android)");
                var view = new MainWindow();
                view.InitialBoot();
                singleView.MainView = view;
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
                Title = "QBasWing Shuttle · QBasCopier y Transfer - ERROR de arranque",
                Width = 560,
                Height = 700,
                Content = new ScrollViewer { Content = tb }
            };
            win.Show();
        }
        catch { }
    }
}