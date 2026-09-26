#if ANDROID
using Avalonia.Controls;

namespace QBasCopier;

/// En Android Avalonia no entrega IClassicDesktopStyleApplicationLifetime sino un
/// SingleViewLifetime: la activity hace SetContentView con lifetime.MainView. Si
/// MainView no se asigna, la activity se queda con un lienzo vacio -> pantalla en
/// blanco, sin excepcion ni crash, que es exactamente lo que pasaba.
///
/// Para no duplicar los ~90 kB de construccion de la interfaz, esta vista crea la
/// MainWindow igual que en escritorio, le traspasa el contenido de su Root y aloja
/// aqui el resultado. La ventana nunca se muestra (Show() es #if !ANDROID).
public partial class MainView : UserControl
{
    private readonly MainWindow _win;

    public MainView()
    {
        InitializeComponent();
        _win = MainWindow.CreateEmbedded(this);
    }
}
#endif
