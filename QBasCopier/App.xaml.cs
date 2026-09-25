using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

namespace QBasCopier;

public partial class App : Application
{
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            var w = new MainWindow();
            w.Closed += (_, _) => desktop.Shutdown();
            desktop.MainWindow = w;
            w.InitialBoot();
        }
        base.OnFrameworkInitializationCompleted();
    }
}