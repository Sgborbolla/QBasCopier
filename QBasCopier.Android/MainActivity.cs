using Android.App;
using Android.Content.PM;
using Avalonia;
using Avalonia.Android;

namespace QBasCopier.Android;

[Activity(Label = "QBasCopier", MainLauncher = true, Theme = "@style/MyTheme",
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.ScreenLayout | ConfigChanges.Density)]
public class MainActivity : AvaloniaMainActivity<App>
{
    protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
        => base.CustomizeAppBuilder(builder).UseAndroid();
}