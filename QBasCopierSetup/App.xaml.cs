using System.Windows;

namespace QBasCopierSetup;

public partial class App : Application
{
    private void OnStartup(object sender, StartupEventArgs e)
    {
        var w = new MainWindow();
        w.Show();
    }
}