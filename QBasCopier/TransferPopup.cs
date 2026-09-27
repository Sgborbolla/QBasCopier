#if !ANDROID
using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace QBasCopier;

/// <summary>
/// La ventanita de Transferir que sale desde la bandeja.
///
/// Lleva las dos mitades de una transferencia y nada mas: enseñar mi QR para que el
/// otro equipo me mande archivos, y escribir o pegar su codigo para mandarselos yo.
/// Es lo que se usa el 90% de las veces, asi que para eso no hace falta abrir la
/// ventana principal. Lo que se usa de vez en cuando (carpeta de destino, puerto,
/// nombre del equipo) esta en "Mas opciones", que abre la ventana en la pestaña de
/// Transferir.
///
/// Es el mismo <see cref="TransferPanel"/> de la app, no una copia: lo que se enciende
/// o se apaga aqui es exactamente lo mismo que se ve alla.
/// </summary>
public sealed class TransferPopup : Window
{
    private static readonly IBrush Panel = new SolidColorBrush(Color.Parse("#060D24"));
    private static readonly IBrush Linea = new SolidColorBrush(Color.Parse("#1F3B8C"));
    private static readonly IBrush Tinta = new SolidColorBrush(Color.Parse("#F6F8FF"));
    private static readonly IBrush TintaSuave = new SolidColorBrush(Color.Parse("#9FB3E8"));

    private readonly MainWindow _owner;
    private readonly TransferPanel _panel;

    public TransferPopup(MainWindow owner, string cual)
    {
        _owner = owner;

        Title = L.Get("sTransfer");
        Width = 470; Height = 560;
        MinWidth = 380; MinHeight = 420;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        Background = Panel;
        ShowInTaskbar = false;
        CanResize = true;

        _panel = new TransferPanel(owner.TrDeviceName);
        _panel.ToggleTransfer += on => owner.TrSetOn(on);
        _panel.JoinWithCode += code => owner.TrJoinCode(code, _panel);
        _panel.CreateHotspot += () => owner.TrHotspot();
        _panel.PickFiles += () => owner.TrPickForPeer();

        var botonera = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 10, 0, 0)
        };
        var mas = new Button { Content = L.Get("trMore"), MinHeight = 36, Padding = new Thickness(14, 0, 14, 0) };
        mas.Click += (_, _) =>
        {
            // Lo que se usa de verdad (destino, puerto, nombre) vive en la ventana.
            _owner.AbrirTransferir();
            Close();
        };
        var cerrar = new Button { Content = "✕", MinHeight = 36, Width = 44, Padding = new Thickness(0) };
        cerrar.Click += (_, _) => Close();
        botonera.Children.Add(mas);
        botonera.Children.Add(cerrar);

        var sp = new StackPanel { Spacing = 8, Margin = new Thickness(12) };
        sp.Children.Add(new TextBlock
        {
            Text = L.Get("sTransfer"),
            FontSize = 15,
            FontWeight = FontWeight.SemiBold,
            Foreground = Tinta
        });
        sp.Children.Add(new ScrollViewer
        {
            Content = _panel,
            VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
            MaxHeight = 400
        });
        sp.Children.Add(botonera);

        Content = new Border { BorderBrush = Linea, BorderThickness = new Thickness(1), Child = sp };

        // La ventanita y la ventana principal cuentan lo mismo, no dos versiones.
        owner.TrSyncPanel(_panel);
        _panel.FocusCard(cual);
    }

    /// <summary>Marca que se ha pulsado Crear o Unirse (por atajo o desde la bandeja).</summary>
    public void TransferirFocus(string cual) => _panel.FocusCard(cual);

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        // Al abrirse, se pone al dia con el estado real del servidor (por si entre
        // que se abria se encendio o apago desde la otra ventana).
        _owner.TrSyncPanel(_panel);
    }
}
#endif
