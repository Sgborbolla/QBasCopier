#if !ANDROID
using Avalonia.Controls;
using Avalonia.Media.Imaging;

namespace QBasCopier;

/// En escritorio la UI (MainWindow, un UserControl) se muestra dentro de una ventana
/// real. Android no puede construir un Window, asi que ahi el control va directo.
/// Los valores de ventana son los que tenia antes en MainWindow.axaml, para que el
/// .exe se vea exactamente igual que la version anterior.
public sealed class DesktopWindow : Window
{
    public DesktopWindow()
    {
        Title = "QBasWing Shuttle · QBasCopier y Transfer";
        SystemDecorations = SystemDecorations.Full;
        Width = 1220; Height = 760;
        MinWidth = 900; MinHeight = 520;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        var view = new MainWindow();
        Content = view;
        MainWindow.Host = this;
        var bmp = MainWindow.LogoBmp;
        if (bmp != null) Icon = new WindowIcon(bmp);
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        // minimizar a la bandeja, como SuperCopier / TeraCopy / NovaCopy
        var view = Content as MainWindow;
        if (view != null && !view.ForceClose && MainWindow.S.MinimizeTo == "tray" && view.TrayVisible)
        {
            e.Cancel = true;
            Hide();
            return;
        }
        try { MainWindow.S.Save(); } catch { }
        base.OnClosing(e);
    }
}
#endif
