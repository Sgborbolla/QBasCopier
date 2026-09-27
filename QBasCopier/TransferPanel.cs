using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace QBasCopier;

/// <summary>
/// El panel de Transferir: la pantalla principal de la app.
///
/// Cuando la ventana principal esta minimizada en la bandeja y eliges
/// Transferir, lo que tiene que salir es esto: una ventana pequena e
/// independiente con las dos mitades de una transferencia, sin tener que
/// volver a la ventana grande.
///
///   CREAR  -> levanta el servidor, enseña el QR con la direccion exacta y
///             un token, y ofrece encender un hotspot para que no haga falta
///             router. El otro equipo apunta la camara y ya esta.
///   UNIRSE -> escribir o pegar el codigo del otro equipo y mandarle archivos.
///
/// En Android no se puede abrir una Window suelta (ahi la UI va dentro de la
/// Activity), asi que el mismo panel se muestra a pantalla completa. No es un
/// capricho: es que el sistema no deja.
///
/// A proposito NO se guarda ningun archivo de la sesion ni se manda nada por
/// internet: va todo por la red local. Para eso existe el token del QR.
/// </summary>
public sealed class TransferPanel : UserControl
{
    private static IBrush B(string hex) => new SolidColorBrush(Color.Parse(hex));
    private static readonly IBrush BgPanel = B("#060D24");
    private static readonly IBrush BgCard = B("#0C1B4A");
    private static readonly IBrush TextMain = B("#F6F8FF");
    private static readonly IBrush TextSoft = B("#9FB3E8");
    private static readonly IBrush Gold = B("#FBBF24");
    private static readonly IBrush Line = B("#1F3B8C");
    private static readonly IBrush Red = B("#CF142B");

    private readonly TextBlock _device;
    private readonly TextBlock _status;
    private readonly TextBlock _url;
    private readonly Image _qr;
    private readonly Border _createCard;
    private readonly Border _joinCard;
    private readonly TextBox _code;
    private readonly TextBlock _joinHint;

    /// <summary>Le pide al anfitrion que encienda o apague el modo Transferir.</summary>
    public event Action<bool>? ToggleTransfer;
    /// <summary>Codigo escrito a mano: hay que conectarse a ese equipo.</summary>
    public event Action<string>? JoinWithCode;
    /// <summary>Enciende un hotspot para no depender del router (solo Windows).</summary>
    public event Action? CreateHotspot;
    /// <summary>Abre el buscador de archivos para mandarle cosas a ese equipo.</summary>
    public event Action? PickFiles;
    /// <summary>
    /// Abre el selector de CARPETAS para mandar un arbol entero. No es lo mismo que
    /// PickFiles: una carpeta llega en el otro equipo como carpeta, con su estructura
    /// y su contenido, sin comprimir y sin temporales.
    /// </summary>
    public event Action? PickFolder;

    public TransferPanel(string deviceName)
    {
        _device = new TextBlock
        {
            Text = deviceName,
            FontSize = 16,
            FontWeight = FontWeight.Bold,
            Foreground = TextMain,
            TextTrimming = TextTrimming.CharacterEllipsis
        };

        _status = new TextBlock
        {
            Text = L.Get("trOff"),
            FontSize = 12.5,
            Foreground = TextSoft,
            TextWrapping = TextWrapping.Wrap
        };

        // --- CREAR -----------------------------------------------------------
        _qr = new Image
        {
            Stretch = Stretch.Uniform,
            Width = 190,
            Height = 190,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        _url = new TextBlock
        {
            Text = "-",
            FontSize = 12,
            Foreground = TextSoft,
            TextTrimming = TextTrimming.CharacterEllipsis,
            TextAlignment = TextAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };

        var createBtn = Big(L.Get("trCreate"), () => ToggleTransfer?.Invoke(true), Gold);
        var createOff = Big(L.Get("trTurnOff"), () => ToggleTransfer?.Invoke(false), Red);

#if ANDROID
        // Android no expone un API publico para crear hotspot de forma fiable,
        // y el sistema pide permisos que no valen la pena. Se explica en vez de
        // dejar un boton que a veces no hace nada.
        var hot = Note(L.Get("trHotspotAndroid"));
#else
        var hot = Big(L.Get("trHotspot"), () => CreateHotspot?.Invoke(), TextMain);
#endif

        _createCard = Card(L.Get("trCreateTitle"), "plus", new Control[]
        {
            _qr,
            _url,
            createBtn,
            createOff,
            hot
        });

        // --- UNIRSE ----------------------------------------------------------
        _code = new TextBox
        {
            Watermark = L.Get("trCodeHint"),
            FontSize = 13.5,
            MinHeight = 40,
            Margin = new Thickness(0, 4, 0, 4)
        };
        var joinBtn = Big(L.Get("trJoin"), () =>
        {
            var t = (_code.Text ?? "").Trim();
            if (t.Length == 0) { _joinHint.Text = L.Get("trCodeEmpty"); return; }
            JoinWithCode?.Invoke(t);
        }, TextMain);

        _joinHint = Note(L.Get("trJoinHint"));

        // Elegir y enviar. En el PC esto abre el buscador de la propia app (se
        // ven las carpetas, se eligen varias cosas y se sueltan encima); en movil
        // abre el selector del sistema, que ya sabe filtrar por tipo.
        var pick = Big(L.Get("addFiles"), () => PickFiles?.Invoke(), TextSoft);

        // Carpeta entera, aparte: es lo que mas se manda y no cabe en el selector de
        // archivos del sistema. Va en su propio boton para que no quepa la duda de
        // que se puede mandar un arbol entero y no solo archivos sueltos.
        var pickDir = Big(L.Get("trSendFolder"), () => PickFolder?.Invoke(), TextSoft);

        var joinBody = new List<Control> { _code };
#if !ANDROID
        // En el PC no hay camara, y escribir una direccion a mano es un coñazo:
        // el codigo llega copiado desde el otro equipo y se pega de un clic.
        var paste = new Button
        {
            Content = new TextBlock { Text = L.Get("trPaste"), FontSize = 12.5, Foreground = TextSoft },
            Background = BgCard,
            BorderBrush = Line,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            MinHeight = 36,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Margin = new Thickness(0, 2, 0, 0)
        };
        paste.Click += async (_, _) =>
        {
            try
            {
                var clip = TopLevel.GetTopLevel(this)?.Clipboard;
                if (clip == null) { _joinHint.Text = L.Get("trPasteFail"); return; }
                var t = await clip.GetTextAsync();
                if (string.IsNullOrWhiteSpace(t)) { _joinHint.Text = L.Get("trPasteFail"); return; }
                _code.Text = t.Trim();
                _joinHint.Text = L.Get("trJoinHint");
            }
            catch { _joinHint.Text = L.Get("trPasteFail"); }
        };
        joinBody.Add(paste);
#endif
        joinBody.Add(joinBtn);
        joinBody.Add(pick);
        joinBody.Add(pickDir);
        joinBody.Add(_joinHint);

        _joinCard = Card(L.Get("trJoinTitle"), "link", joinBody.ToArray());

        var head = new StackPanel
        {
            Spacing = 2,
            Margin = new Thickness(0, 0, 0, 10),
            Children =
            {
                new TextBlock
                {
                    Text = L.Get("tabTransfer"),
                    FontSize = 12,
                    Foreground = Gold,
                    FontWeight = FontWeight.Bold
                },
                _device,
                _status
            }
        };

        // En un movil se apilan y en PC caben en dos columnas: lo importante
        // esta arriba, sin tener que hacer scroll.
        var cards = new WrapPanel { Orientation = Orientation.Horizontal, ItemWidth = 300 };
        _createCard.Width = 290;
        _joinCard.Width = 290;
        cards.Children.Add(_createCard);
        cards.Children.Add(_joinCard);

        Content = new StackPanel
        {
            Spacing = 8,
            Margin = new Thickness(4, 2, 4, 2),
            Children = { head, cards }
        };

        SetRunning(false, "", "");
    }

    /// <summary>
    /// Refleja lo que esta haciendo el servidor. Si no esta encendido, el QR se
    /// apaga: un QR apagado no engaña a nadie.
    /// </summary>
    public void SetRunning(bool on, string url, string note)
    {
        _createCard.BorderBrush = on ? Gold : Line;
        _qr.Opacity = on ? 1 : 0.25;
        _url.Text = on ? url : "-";
        _status.Text = note.Length > 0
            ? note
            : on ? L.Get("trOn") : L.Get("trOff");
        _status.Foreground = on ? Gold : TextSoft;
    }

    public void SetQr(string payload)
    {
        try { _qr.Source = Qr.Make(payload, 420); }
        catch { _qr.Source = null; }
    }

    public void SetJoinHint(string msg) => _joinHint.Text = msg;

    public void PrefillCode(string code) => _code.Text = code;

    /// <summary>
    /// Deja marcada la tarjeta a la que se refiere una accion del menu (crear o
    /// unirse), para que se sepa de que botón se ha pulsado en la bandeja.
    /// </summary>
    public void FocusCard(string which)
    {
        var crear = which == "crear";
        _createCard.BorderThickness = new Thickness(crear ? 2 : 1);
        _joinCard.BorderThickness = new Thickness(crear ? 1 : 2);
        _createCard.BorderBrush = crear ? Gold : Line;
        _joinCard.BorderBrush = crear ? Line : Gold;
    }

    private static TextBlock Note(string t) => new()
    {
        Text = t,
        FontSize = 11.5,
        Foreground = TextSoft,
        TextWrapping = TextWrapping.Wrap
    };

    private static Button Big(string text, Action go, IBrush tint)
    {
        var b = new Button
        {
            Content = new TextBlock { Text = text, FontSize = 13, Foreground = tint, FontWeight = FontWeight.Bold },
            Background = BgCard,
            BorderBrush = Line,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            MinHeight = 44,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            HorizontalContentAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 3, 0, 0)
        };
        b.Click += (_, _) => go();
        return b;
    }

    private static Border Card(string title, string ico, Control[] body)
    {
        var stack = new StackPanel { Spacing = 6 };
        var head = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 7,
            Margin = new Thickness(0, 0, 0, 2),
            Children =
            {
                Ico.Get(ico, 18, Gold),
                new TextBlock { Text = title, FontSize = 13.5, FontWeight = FontWeight.Bold, Foreground = TextMain }
            }
        };
        stack.Children.Add(head);
        foreach (var c in body) stack.Children.Add(c);
        return new Border
        {
            Background = BgPanel,
            BorderBrush = Line,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(12, 10, 12, 12),
            Margin = new Thickness(0, 0, 8, 8),
            Child = stack
        };
    }
}
