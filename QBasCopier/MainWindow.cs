using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using System.Diagnostics;
using System.Globalization;
using System.IO;

namespace QBasCopier;

public sealed partial class MainWindow : UserControl
{
    public static Settings S = new();

#if !ANDROID
    /// En escritorio la UI se muestra dentro de una ventana real. Android no puede
    /// ni construir un Window, asi que ahi este control va directo al MainView.
    public static Window? Host { get; set; }
    public static Bitmap? LogoBmp => _logoBmp;
#endif

    // ---------- fábricas ----------
    private static IBrush B(string hex) => new SolidColorBrush(Color.Parse(hex));
    private static readonly IBrush BgPanel = B("#060D24");
    private static readonly IBrush BgDeep = B("#08122F");
    private static readonly IBrush TextMain = B("#F6F8FF");
    private static readonly IBrush TextSoft = B("#9FB3E8");
    private static readonly IBrush Gold = B("#FBBF24");
    private static readonly IBrush Red = B("#CF142B");
    private static readonly IBrush Line = B("#1F3B8C");

    private readonly List<(string key, Action set)> _texts = new();
    private readonly List<CopyItem> _queue = new();
    private readonly List<string> _errLines = new();
    private readonly List<HistoryEntry> _history = new();
    private readonly Stopwatch _batchSw = new();

    private CopyEngine? _engine;
    private double _lastRate, _lastDone;
    private long _lastTickTicks;
    private bool _running;
    private bool _forceClose;
    private TrayIcon? _tray;
    private DispatcherTimer? _ticker;

    private static Bitmap? _logoBmp;
#if !ANDROID
    private static WindowIcon? _logo;
#endif

    private TextBlock _lblFrom = null!, _lblProg = null!, _lblRate = null!, _lblTime = null!, _lblCur = null!, _lblStatus = null!;
    private TextBox _tbFrom = null!, _tbTo = null!;
    private ProgressBar _ggBar = null!, _miniBar = null!;
    private TextBlock _miniLbl = null!;
    private TabControl _tabs = null!;
    private Border _mini = null!;
    private Button _bCopy = null!, _bMove = null!, _bPause = null!, _bResume = null!, _bSkip = null!, _bCancel = null!, _bClear = null!;
    private Button _bIntegrate = null!, _bFold = null!, _bQuit = null!;
    private ListBox _lbQueue = null!, _lbErrs = null!, _lbHist = null!;
    private FilePane _left = null!, _right = null!;
    private ComboBox _cmbLang = null!, _cmbLangQuick = null!, _cmbAfter = null!, _cmbEngine = null!, _cmbCollision = null!, _cmbError = null!, _cmbPriority = null!, _cmbBuffer = null!, _cmbSizeUnit = null!, _cmbAddWhen = null!;
    private TextBox _tbBuffer = null!, _tbRetry = null!, _tbSpeed = null!, _tbUpdate = null!, _tbAvg = null!, _tbThrottle = null!, _tbWarn = null!, _tbNewPat = null!;
    private Slider _sldThreads = null!, _sldSpeed = null!;
    private TextBlock _lblThreads = null!, _lblSpeed = null!;
    private CheckBox _chkTray = null!, _chkStart = null!, _chkAttrib = null!, _chkSec = null!, _chkDel = null!, _chkKeep = null!, _chkRO = null!, _chkHidden = null!, _chkTitle = null!, _chkLimit = null!, _chkVerify = null!, _chkActivate = null!, _chkAskAdd = null!, _chkLog = null!;

    private const string TabExplorer = "explorer", TabQueue = "queue", TabErrors = "errors", TabOptions = "options", TabHistory = "history", TabTransfer = "transfer";
    private const string AboutText =
        "QBasCopier y Transfer crece de un sueño: el de QBaswing Designer, una pequeña compañía independiente " +
        "que nació de las manos del Dr. Sergio Grabiel Borbolla Verdecia. Desde Cuba, con el corazón " +
        "lleno de amor por la medicina y por el mundo digital, cada línea se escribe con esfuerzo y " +
        "esperanza, aunque a veces la tecnología no alcance.\n\n" +
        "En este camino no estoy solo, y quiero que lo sepas: una parte muy especial de esta obra " +
        "pertenece también a Frank Freeman, dueño de Freeman y Diseño —más conocido como Freeman " +
        "Impresiones—, a quien considero no solo un amigo, sino casi un hermano. Él cree en mí y en " +
        "este proyecto, me apoya y me ayuda en cada paso, y esa fe sincera es uno de los motores que " +
        "sostienen cada línea de este código.\n\n" +
        "Este es un pequeño homenaje a la idea de que con dedicación se cumplen sueños y se entregan " +
        "al mundo obras útiles y hermosas. Gracias por formar parte de él.\n\n— SGBV";

    // ---------------------- Transferir (QBasCopier y Transfer) ----------------------
    private readonly TransferHost _trSrv = new();
    private CheckBox? _trToggle;
    private Image? _trImg;
    private Image? _trNetImg;
    private TextBlock? _trNetInfo;
    private Border? _trNetBox;
    private TextBlock? _trInfo;
    private TextBlock? _trStatus;
    private TextBlock? _trRx;
    private TextBox? _trAddr;
    private ListBox? _trPeers;
    private readonly System.Collections.Generic.List<string> _trPeerList = new();
    private long _trRxBytes;
    private long _trLastFill;

    private string DevName => (S.DeviceName?.Trim() ?? "") is { Length: > 0 } n ? n : (OperatingSystem.IsAndroid() ? "Android" : Environment.MachineName);

    private static void DispatchUi(Action a) { try { Dispatcher.UIThread.Post(a); } catch { } }

    private void InitTransfer()
    {
        TransferHost.DeviceName = DevName;
        _trSrv.FileReceived += (path, bytes) =>
        {
            var name = System.IO.Path.GetFileName(path);
#if ANDROID
            var saved = path;
            _ = Task.Run(() => QBasCopier.Android.DroidPub.Publish(saved));
#endif
            _ = Task.Run(async () =>
            {
                try { await HistoryStore.AppendAsync(DeviceNameOrHost(), "Transferir (WiFi)", "Recibido: " + name + " (" + FilePane.Human(bytes) + ")", bytes); }
                catch { }
            });
            DispatchUi(() =>
            {
                _trRxBytes += bytes;
                _lblStatus.Text = "Recibido: " + name + " (" + FilePane.Human(bytes) + ")";
                if (_trStatus != null) _trStatus.Text = "Recibido: " + name;
#if ANDROID
                if (S.TransferAuto) QBasCopier.Android.DroidCtx.Toast("Recibido: " + name);
#endif
            });
        };
        Discovery.Changed += () => DispatchUi(FillPeers);
    }

    /// <summary>
    /// La clave de emparejamiento ahora se comprueba de verdad en el servidor, asi que
    /// hay que publicarla antes de arrancar a escuchar.
    /// </summary>
    private void ApplyTransferKey()
    {
        var k = (S.TransferKey ?? "").Trim();
        TransferHost.Key = k;
        TransferClient.Key = k;
        TransferHost.DeviceName = S.DeviceName ?? "";
    }

    /// <summary>
    /// Boton atras de Android. Antes salia de la app directamente desde cualquier
    /// pantalla, y con un dialogo de colision abierto se cerraba la app a medias.
    /// Devuelve true si el evento quedo consumido.
    /// </summary>
    private bool HandleBack()
    {
        if (_overlayClose != null)
        {
            // Cerrar el dialogo sin decidir: se vuelve a preguntar, no se cuelga la copia.
            _overlayClose();
            return true;
        }
        if (_running) { _lblStatus.Text = L.Get("busy"); return true; }
        if (_tabs != null && _tabs.SelectedIndex > 0) { _tabs.SelectedIndex = 0; return true; }
        return false;
    }

    private static string DeviceNameOrHost()
    {
        try { return (string.IsNullOrWhiteSpace(TransferHost.DeviceName) ? Environment.MachineName : TransferHost.DeviceName); }
        catch { return "QBasCopier"; }
    }

    private void ToggleTr(bool on)
    {
        if (on)
        {
            var inbox = S.TransferInbox;
            if (string.IsNullOrWhiteSpace(inbox)) inbox = TransferDefaultInbox();
            S.TransferInbox = inbox;
            if (!CopyEngine.IsSafPath(inbox)) { try { Directory.CreateDirectory(inbox); } catch { } }
            _trSrv.Start(S.TransferPort, inbox);
            Discovery.Start(DevName, S.TransferPort);
            _lblStatus.Text = "Modo Transferir activado. Muestra el QR o busca dispositivos.";
#if ANDROID
            QBasCopier.Android.MainActivity.Current?.OpenHotspotSettings();
#endif
        }
        else
        {
            _trSrv.Stop();
            Discovery.Stop();
            _trPeerList.Clear();
            FillPeers();
            _lblStatus.Text = "Modo Transferir apagado.";
        }
        // El unico Save() va al final: antes se guardaba TransferOn antes de decidir el
        // inbox, y el inbox nunca llegaba al disco.
        S.TransferOn = on;
        S.Save();
        RefreshTrUi();
    }

    private static string TransferDefaultInbox()
    {
        try
        {
            if (OperatingSystem.IsAndroid())
            {
                // SpecialFolder.ApplicationData en Android no es una ruta escribible de
                // verdad: al recibir, la escritura fallaba y no se guardaba nada.
                try
                {
                    var home = QBasCopier.Android.DroidList.Home();
                    var dir = System.IO.Path.Combine(home, "Recibidos");
                    Directory.CreateDirectory(dir);
                    return dir;
                }
                catch { }
            }
            return System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "QBasCopierRecibidos");
        }
        catch { return System.IO.Path.GetTempPath(); }
    }

    private Control BuildTransferBody()
    {
        InitTransfer();
        var sp = new StackPanel { Spacing = 10, Margin = new Thickness(6) };

        var head = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        _trToggle = MkChk("Modo Transferir (compartir por WiFi)", S.TransferOn, b => ToggleTr(b));
        head.Children.Add(_trToggle);
        head.Children.Add(Btn("Crear nuestra red", OpenHotspot));
        head.Children.Add(Btn("Escanear QR", ScanQr));
        head.Children.Add(Btn("Buscar dispositivos", () => { Discovery.Announce(); FillPeers(); _lblStatus.Text = "Buscando dispositivos…"; }));
        sp.Children.Add(head);

        var cfg = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        var nmLb = MkLbl("Mi nombre:", 12); nmLb.Tint(TextSoft);
        cfg.Children.Add(nmLb);
        var tbName = new TextBox { Text = S.DeviceName ?? "", Width = 180, Watermark = "android · PC · tablet" };
        tbName.TextChanged += (s, e) => { S.DeviceName = tbName?.Text ?? ""; S.Save(); };
        cfg.Children.Add(tbName);
        cfg.Children.Add(Btn("Carpeta de recibidos…", PickInbox));
        var chkAuto = MkChk("Avisar al recibir", S.TransferAuto, b => { S.TransferAuto = b; S.Save(); });
        cfg.Children.Add(chkAuto);
        sp.Children.Add(cfg);

        var row = new Grid { ColumnDefinitions = { new(GridLength.Auto), new(GridLength.Star) } };
        var leftCol = new StackPanel { Spacing = 8, VerticalAlignment = VerticalAlignment.Top };
        var qrPanel = new Border { Background = Brushes.White, CornerRadius = new CornerRadius(8), Padding = new Thickness(8), HorizontalAlignment = HorizontalAlignment.Left };
        _trImg = new Image { Width = 320, Height = 320, Stretch = Stretch.Uniform };
        qrPanel.Child = _trImg;
        leftCol.Children.Add(qrPanel);

        _trNetImg = new Image { Width = 200, Height = 200, Stretch = Stretch.Uniform };
        _trNetInfo = MkLbl("", 12); _trNetInfo.Tint(TextSoft); _trNetInfo.TextWrapping = TextWrapping.Wrap;
        _trNetInfo.MaxWidth = 320;
        _trNetBox = new Border
        {
            Background = Brushes.White,
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(8),
            HorizontalAlignment = HorizontalAlignment.Left,
            IsVisible = false,
            Child = new StackPanel
            {
                Spacing = 6,
                Children =
                {
                    new TextBlock { Text = "QR de nuestra red", FontSize = 13, FontWeight = FontWeight.Bold, Foreground = Brushes.Black },
                    _trNetImg,
                    _trNetInfo
                }
            }
        };
        leftCol.Children.Add(_trNetBox);
        Grid.SetColumn(leftCol, 0);
        row.Children.Add(leftCol);

        var col = new StackPanel { Spacing = 8, Margin = new Thickness(12, 0, 0, 0) };
        _trInfo = MkLbl("", 13); _trInfo.TextWrapping = TextWrapping.Wrap; _trInfo.MaxWidth = 430;
        col.Children.Add(_trInfo);
        _trStatus = MkLbl("", 12); _trStatus.Tint(TextSoft); _trStatus.TextWrapping = TextWrapping.Wrap;
        col.Children.Add(_trStatus);
        _trRx = MkLbl("", 12); _trRx.Tint(TextSoft);
        col.Children.Add(_trRx);
        col.Children.Add(MkLbl("O conéctate manualmente escribiendo la dirección:", 12).Tint(TextSoft));

        var mr = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
        _trAddr = new TextBox { Width = 250, Watermark = "http://IP:9527" };
        mr.Children.Add(_trAddr);
        mr.Children.Add(Btn("Conectar", ConnectManual));
        col.Children.Add(mr);

        col.Children.Add(MkLbl("Dispositivos encontrados (toca uno y pulsa Enviar):", 12).Tint(TextSoft));
        _trPeers = new ListBox { MinHeight = 120, MaxHeight = 190 };
        _trPeers.ItemsSource = _trPeerList;
        col.Children.Add(_trPeers);
        col.Children.Add(Btn("Enviar archivos al seleccionado…", PickAndSend));
        col.Children.Add(MkLbl("Sin la app en el otro equipo? Abre esa misma dirección en el navegador: " +
            "sube y descarga archivos (compatible con cualquier móvil, tablet u ordenador).", 12).Tint(TextSoft));
        col.Children.Add(MkLbl("Para transferir más rápido: 5 GHz, canal de 80 MHz y sin internet " +
            "compartido a la vez por el mismo móvil.", 12).Tint(TextSoft));

        Grid.SetColumn(col, 1);
        row.Children.Add(col);
        sp.Children.Add(row);

        RefreshTrUi();
        return sp;
    }

    private static Button Btn(string txt, Action a)
    {
        var b = new Button { Content = txt, FontSize = 12, Margin = new Thickness(2) };
        b.Click += (s, e) => a();
        return b;
    }

    private void RefreshTrUi()
    {
        if (_trInfo == null || _trImg == null) return;
        var on = _trSrv.Running;
        if (on)
        {
            var urls = new System.Collections.Generic.List<string>();
            foreach (var p in _trSrv.Prefixes) if (!p.Contains("127.0.0.1")) urls.Add(p.TrimEnd('/'));
            _trInfo.Text = "Red: " + S.TransferNet + Environment.NewLine +
                           "Clave: " + S.TransferKey + Environment.NewLine +
                           "Servidor activo en:" + Environment.NewLine +
                           string.Join(Environment.NewLine, urls);
            _trImg.Source = Qr.Make(MakeQrText(), 512);
            _trStatus.Text = "Activo. Listo para recibir en: " + S.TransferInbox;
            FillPeers();
        }
        else
        {
            _trInfo.Text = "Activa el modo Transferir para compartir archivos sin internet, entre móvil y PC por WiFi.";
            _trImg.Source = null;
            _trStatus.Text = "Modo apagado.";
        }
    }

    private string MakeQrText()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("QBasCopier y Transfer");
        sb.AppendLine("Red: " + S.TransferNet);
        sb.AppendLine("Clave: " + S.TransferKey);
        sb.AppendLine("WIFI:T:WPA;S:" + S.TransferNet + ";P:" + S.TransferKey + ";;");
        foreach (var p in _trSrv.Prefixes) if (!p.Contains("127.0.0.1")) sb.AppendLine(p);
        return sb.ToString();
    }

    private void PickAndSend()
    {
        var peer = _trPeers?.SelectedItem as string;
        if (!_trSrv.Running) { _lblStatus.Text = "Activa el modo Transferir primero."; return; }
        if (string.IsNullOrEmpty(peer)) { _lblStatus.Text = "Selecciona un dispositivo de la lista."; return; }
        var sp = peer.IndexOf(' ');
        if (sp <= 0) { _lblStatus.Text = "Dispositivo no válido."; return; }
        var addr = peer.Substring(0, sp);          // "ip:puerto"
        var colon = addr.LastIndexOf(':');
        var host = colon > 0 ? addr.Substring(0, colon) : addr;
        var port = colon > 0 && int.TryParse(addr.Substring(colon + 1), out var pp) ? pp : S.TransferPort;
        var baseUrl = "http://" + host + ":" + port + "/";
        PickFilesAndSend(baseUrl);
    }

    private void PickInbox()
    {
#if ANDROID
        var ma = QBasCopier.Android.MainActivity.Current;
        if (ma == null) return;
        ma.PickTree(uri =>
        {
            if (string.IsNullOrEmpty(uri)) return;
            S.TransferInbox = uri;
            S.Save();
            RestartTrServer();
            RefreshTrUi();
            _lblStatus.Text = "Inbox configurado (carpeta del sistema).";
        });
#else
        _ = PickInboxDesktopAsync();
#endif
    }

    private void RestartTrServer()
    {
        try
        {
            if (_trSrv.Running)
            {
                _trSrv.Stop();
                _trSrv.Start(S.TransferPort, S.TransferInbox);
            }
        }
        catch { }
    }

#if !ANDROID
    private async Task PickInboxDesktopAsync()
    {
        try
        {
            var d = new OpenFolderDialog { Title = "Carpeta de recibidos…" };
            var r = await d.ShowAsync(Host);
            if (!string.IsNullOrEmpty(r))
            {
                S.TransferInbox = r;
                S.Save();
                RestartTrServer();
                RefreshTrUi();
                _lblStatus.Text = "Inbox: " + r;
            }
        }
        catch { }
    }
#endif

    private void ScanQr()
    {
#if ANDROID
        QBasCopier.Android.ScanActivity.Callback = OnScanned;
        QBasCopier.Android.MainActivity.Current?.StartScan();
#else
        _lblStatus.Text = "En PC escribe la dirección manualmente (debajo del QR en el otro equipo).";
#endif
    }

    private async void PickFilesAndSend(string baseUrl)
    {
#if ANDROID
        QBasCopier.Android.MainActivity.Current?.PickFiles(async uris =>
        {
            var names = new string[uris.Length];
            for (int i = 0; i < uris.Length; i++)
            {
                var f = QBasCopier.Android.DroidFile.Info(uris[i]);
                names[i] = "droid:" + uris[i] + "|" + f.Name + "|" + f.Size;
            }
            await SendToBaseUrlAsync(baseUrl, names);
        });
#else
        try
        {
            var dlg = new OpenFileDialog { AllowMultiple = true, Title = "Enviar archivos…" };
            var r = await dlg.ShowAsync(this);
            if (r != null) await SendToBaseUrlAsync(baseUrl, r);
        }
        catch { }
#endif
    }

    private async Task SendToBaseUrlAsync(string baseUrl, string[] paths)
    {
        if (paths == null || paths.Length == 0) return;
        int sent = 0;
        foreach (var path in paths)
        {
            if (string.IsNullOrEmpty(path)) continue;
            string name; long total; Func<Stream> open;
            if (path.StartsWith("droid:"))
            {
#if ANDROID
                var parts = path.Substring(6).Split('|');
                var u = parts[0]; name = parts[1]; total = long.TryParse(parts[2], out var sz) ? sz : 0;
                var uri = u;
                open = () => QBasCopier.Android.DroidFile.Open(uri);
                if (total <= 0)
                {
                    DispatchUi(() => _lblStatus.Text = "Tamaño desconocido; se omite " + name);
                    continue;
                }
#else
                DispatchUi(() => _lblStatus.Text = "Origen no disponible en este equipo.");
                continue;
#endif
            }
            else
            {
                var fi = new FileInfo(path);
                if (!fi.Exists)
                {
                    DispatchUi(() => _lblStatus.Text = "No existe: " + path);
                    continue;
                }
                name = fi.Name; total = fi.Length;
                open = () => new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 256 * 1024, FileOptions.Asynchronous);
            }
            var label = name;
            long lastPost = 0;
            var ok = await TransferClient.Upload(baseUrl, name, total, open, done =>
            {
                var now = Environment.TickCount64;
                if (now - lastPost < 150) return;
                lastPost = now;
                DispatchUi(() => { if (_trStatus != null) _trStatus.Text = "Enviando " + label + ": " + FilePane.Human(done) + (total > 0 ? "/" + FilePane.Human(total) : ""); });
            });
            DispatchUi(() => _lblStatus.Text = (ok >= 0 ? "Enviado: " : "Error en: ") + label);
            if (ok >= 0)
            {
                sent++;
                try { await HistoryStore.AppendAsync(label, baseUrl, "Enviado por WiFi (" + FilePane.Human(total) + ")", total); } catch { }
            }
        }
        if (_trRx != null) _trRx.Text = "Enviados en sesión: " + sent + " archivo(s)";
    }

    private void OnScanned(string txt)
    {
        if (string.IsNullOrWhiteSpace(txt)) { _lblStatus.Text = "QR vacío."; return; }
        var url = "";
        foreach (var line in txt.Split('\n'))
            if (line.Trim().StartsWith("http", StringComparison.OrdinalIgnoreCase)) { url = line.Trim(); break; }
        if (url.Length == 0) { _lblStatus.Text = "QR no válido para QBasCopier y Transfer."; return; }
        ConnectBase(url.TrimEnd('/') + "/");
    }

    private void ConnectManual()
    {
        var t = (_trAddr?.Text ?? "").Trim();
        if (t.Length == 0) { _lblStatus.Text = "Escribe una dirección, p. ej. http://192.168.1.5:9527"; return; }
        if (!t.StartsWith("http", StringComparison.OrdinalIgnoreCase)) t = "http://" + t;
        ConnectBase(t.TrimEnd('/') + "/");
    }

    private void ConnectBase(string baseUrl)
    {
        _ = Task.Run(async () =>
        {
            string txt;
            try { txt = await TransferClient.Ping(baseUrl); }
            catch { txt = ""; }
            DispatchUi(() =>
            {
                if (txt.Contains("QBasCopier y Transfer"))
                {
                    _lblStatus.Text = "Conectado a: " + baseUrl;
                    if (_trStatus != null) _trStatus.Text = "Conectado a: " + baseUrl;
                }
                else _lblStatus.Text = "No responde en " + baseUrl;
            });
        });
    }

    private void OpenHotspot()
    {
        var ssid = (S.TransferNet ?? "").Trim().Replace("\"", "").Replace(";", "");
        if (ssid.Length == 0) ssid = "QBasWing-Transfer";
        var key = (S.TransferKey ?? "").Trim();
        if (key.Length < 8) key = "QBas2026";
        S.TransferNet = ssid; S.TransferKey = key; S.Save();

        ShowNetQr(ssid, key, "Red: " + ssid);
#if ANDROID
        var ma = QBasCopier.Android.MainActivity.Current;
        if (ma == null) { _lblStatus.Text = "Abre la app y vuelve a pulsar Crear nuestra red."; return; }
        _lblStatus.Text = "Abriendo los ajustes de WiFi…";
        ma.StartOwnNetwork(ssid, key, (ok, msg) => DispatchUi(() =>
        {
            if (ok)
            {
                ShowNetQr(ssid, key, "Red: " + ssid);
                if (_trStatus != null) _trStatus.Text = "1) En los ajustes, activa 'Punto de acceso' con el nombre " + ssid +
                    " y la clave " + key + "  2) En el otro equipo entra a esa red  3) Abre la dirección de la app " +
                    "en el navegador o pulsa Buscar dispositivos.";
                _lblStatus.Text = "Red propia con estos datos: " + ssid + " / " + key;
            }
            else
            {
                if (_trStatus != null) _trStatus.Text = "No se pudo crear la red automáticamente (" + msg + "). " +
                    "Abre los ajustes de punto de acceso y ponle el nombre " + ssid + " y la clave " + key + ".";
                _lblStatus.Text = "Crea la red a mano con estos datos: " + ssid + " / " + key;
                ma.OpenHotspotSettings();
            }
        }));
#else
        _lblStatus.Text = "Red propia: " + ssid + " · clave " + key;
        if (_trStatus != null) _trStatus.Text = "En el PC: activa el punto de acceso móvil de Windows (o comparte esta red) y " +
                                              "conecta el otro equipo. Luego pulsa Buscar dispositivos.";
        try
        {
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = OperatingSystem.IsWindows() ? "ms-settings:mobilehotspot" : "open",
                UseShellExecute = true
            };
            if (!OperatingSystem.IsWindows())
            {
                psi.ArgumentList.Add("x-apple.systempreferences:com.apple.Network-Settings");
            }
            System.Diagnostics.Process.Start(psi);
        }
        catch { }
#endif
    }

    private void ShowNetQr(string ssid, string key, string title)
    {
        if (_trNetImg == null || _trNetInfo == null) return;
        var payload = "WIFI:T:WPA;S:" + ssid + ";P:" + key + ";;\n\n" + ssid + "\nClave: " + key;
        _trNetImg.Source = Qr.Make(payload, 420);
        _trNetInfo.Text = title + "\nRed: " + ssid + "\nClave: " + key +
                          "\nEn el otro equipo: entra a esa red y luego pulsa Buscar dispositivos.";
        if (_trNetBox != null) _trNetBox.IsVisible = true;
    }

    private void FillPeers()
    {
        _trPeerList.Clear();
        lock (Discovery.Devices)
            foreach (var d in Discovery.Devices)
                _trPeerList.Add($"{d.Ip}:{d.Port}  ·  {d.Name}");
        if (_trPeers != null)
        {
            var sel = _trPeers.SelectedItem as string;
            _trPeers.ItemsSource = null;
            _trPeers.ItemsSource = _trPeerList;
            if (sel != null) _trPeers.SelectedItem = sel;
        }
    }

    public MainWindow()
    {
        InitializeComponent();
#if !ANDROID
        _logo = LoadLogo();
#endif
        BuildWindow();
        _ticker = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(Math.Clamp(S.WindowUpdateMs, 50, 1000)) };
        _ticker.Tick += (_, _) => Tick();
        _ticker.Start();
    }

    #if !ANDROID
    private static WindowIcon? LoadLogo()
    {
        try
        {
            using var s = AssetLoader.Open(new Uri("avares://QBasCopier/Assets/logo.png"));
            var bmp = new Bitmap(s);
            _logoBmp = bmp;
            return new WindowIcon(bmp);
        }
        catch { return null; }
    }
#else
    private static Bitmap? LoadLogo()
    {
        try
        {
            using var s = AssetLoader.Open(new Uri("avares://QBasCopier/Assets/logo.png"));
            var bmp = new Bitmap(s);
            _logoBmp = bmp;
            return bmp;
        }
        catch { return null; }
    }
#endif

    public void InitialBoot()
    {
        var args = Program.StartupArgs;
        S = Settings.Load();
        if (string.IsNullOrEmpty(S.Lang)) S.Lang = Code(DetectLikely());
        L.SetLanguage(S.Lang);

        var paths = new List<string>();
        string? dest = null;
        bool move = false, hidden = args.Contains("--hidden"), after = false;

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--lang" when i + 1 < args.Length:
                    S.Lang = Code(args[++i]); L.SetLanguage(S.Lang); break;
                case "--install-integration":
#if !ANDROID
                    if (OperatingSystem.IsWindows()) ExplorerIntegration.Install();
#endif
                    break;
                case "--uninstall-integration":
#if !ANDROID
                    if (OperatingSystem.IsWindows()) ExplorerIntegration.Uninstall();
#endif
                    break;
                case "--start-with-windows":
                    S.StartWithWindows = true;
#if !ANDROID
                    if (OperatingSystem.IsWindows()) ExplorerIntegration.SetStartWithWindows(true);
#endif
                    break;
                case "--move": move = true; break;
                case "--copy": after = true; break;
                case "--dest" when i + 1 < args.Length:
                    dest = args[++i];
                    if (Directory.Exists(dest) && dest.Length > 0 && dest[^1] != '/' && dest[^1] != '\\') dest += Path.DirectorySeparatorChar;
                    break;
                default:
                    if (after && !args[i].StartsWith("--")) paths.Add(args[i]);
                    break;
            }
        }

        ReloadTexts();
        BuildTray();
        ApplyTransferKey();
        if (paths.Count > 0) AddFiles(paths.ToArray());
        if (!string.IsNullOrEmpty(dest)) _tbTo.Text = dest;

        if (hidden && paths.Count == 0)
        {
#if !ANDROID
            Host?.Hide();
#endif
            return;
        }
#if !ANDROID
        Host?.Show();
        Host?.Activate();
#else
        // Si se dejo activado, la app lo encendia de nuevo pero la casilla se quedaba
        // en apagado: la UI mentia sobre lo que realmente estaba haciendo.
        if (S.TransferOn) { _trToggle.IsChecked = true; ToggleTr(true); }
        global::QBasCopier.Android.MainActivity.Current?.KeepAwake();
        global::QBasCopier.Android.MainActivity.BackHandler = HandleBack;
#endif
        if (paths.Count > 0 && !string.IsNullOrEmpty(dest)) _ = Task.Delay(120).ContinueWith(_ => Dispatcher.UIThread.Post(() => _ = StartCopy(move)));
    }

    public static string Code(string name)
    {
        var c = name.ToLowerInvariant();
        var known = Ex.Codes;
        if (known.Contains(c)) return c;
        if (c.StartsWith("es")) return "es";
        if (c.StartsWith("pt")) return "pt";
        if (c.StartsWith("zh")) return "zh";
        if (c.StartsWith("ja")) return "ja";
        if (c.StartsWith("ko")) return "ko";
        if (c.StartsWith("ar")) return "ar";
        if (c.StartsWith("ru")) return "ru";
        if (c.StartsWith("uk")) return "uk";
        if (c.Length >= 2) return c[..2];
        return "en";
    }

    private static string DetectLikely() => CultureInfo.CurrentUICulture.Name;

    // ---------------------------------------------------------------- build
    private void BuildWindow()
    {
        var root = this.FindControl<Grid>("Root");
        var main = new Grid
        {
            RowDefinitions = { new(GridLength.Auto), new(GridLength.Star), new(GridLength.Auto) }
        };
        main.Children.Add(BuildToolbar());
        main.Children.Add(BuildBody());
        main.Children.Add(BuildStatusbar());
        Grid.SetRow(main.Children[0] as Control, 0);
        Grid.SetRow(main.Children[1] as Control, 1);
        Grid.SetRow(main.Children[2] as Control, 2);
        root.Children.Add(main);
    }

    private Control BuildToolbar()
    {
        // La marca ocupa una columna Star con recorte: antes iba en Auto y en un movil de
        // 360 dp empujaba el desplegable de idioma y los botones fuera de la pantalla,
        // que era como se perdia el acceso a la pestana Transferir.
        var bar = new Grid
        {
            ColumnDefinitions = { new(GridLength.Star), new(GridLength.Auto), new(GridLength.Auto), new(GridLength.Auto), new(GridLength.Auto) },
            Margin = new Thickness(8, 4)
        };
        var brand = new TextBlock
        {
            Text = "QBasCopier y Transfer", FontSize = 18, FontWeight = FontWeight.Bold, Foreground = Gold,
            VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(2, 0, 8, 0),
            TextTrimming = TextTrimming.CharacterEllipsis, ClipToBounds = true
        };
        Grid.SetColumn(brand, 0);
        bar.Children.Add(brand);

        _cmbLangQuick = new ComboBox
        {
            ItemsSource = Ex.LangNames(), SelectedIndex = Ex.IndexOf(S.Lang),
            MinWidth = 70, MaxWidth = 170, Width = 170,
            HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 10, 0)
        };
        _cmbLangQuick.SelectionChanged += (s, e) => { if (Ex.Codes.Length > (s as ComboBox)?.SelectedIndex) ApplyLang(Ex.Codes[((ComboBox)s!).SelectedIndex]); };
        Grid.SetColumn(_cmbLangQuick, 1);
        bar.Children.Add(_cmbLangQuick);

        _bIntegrate = Mk("integrate", ToggleIntegration);
        Grid.SetColumn(_bIntegrate, 2);
        bar.Children.Add(_bIntegrate);

        _bFold = Mk("fold", ToggleFold);
        Grid.SetColumn(_bFold, 3);
        bar.Children.Add(_bFold);

        var bQuit = Mk("quit", DoQuit);
        bQuit.Classes.Add("primary");
        _bQuit = bQuit;
        Grid.SetColumn(bQuit, 4);
        bar.Children.Add(bQuit);

        // En pantallas estrechas el desplegable de idioma se encoge en vez de empujar
        // los botones fuera de la vista.
        bar.SizeChanged += (_, e) =>
        {
            var w = e.NewSize.Width;
            if (w <= 0) return;
            var tight = w < 620;
            _cmbLangQuick.Width = tight ? 92 : 170;
            _cmbLangQuick.FontSize = tight ? 12 : 13;
            brand.TextTrimming = tight ? TextTrimming.CharacterEllipsis : TextTrimming.None;
        };

        return bar;
    }

    private Control BuildBody()
    {
        var body = new Grid { RowDefinitions = { new(GridLength.Star) }, Margin = new Thickness(4, 2, 4, 2) };
        _tabs = new TabControl();
        // A 360 dp seis cabeceras completas no caben y la ultima (Transferir) quedaba
        // fuera de pantalla sin forma de alcanzarla.
        _tabs.Classes.Add("tabsCompact");
        _tabs.Items.Add(MkTab(TabExplorer, BuildExplorerTab()));
        _tabs.Items.Add(MkTab(TabQueue, BuildQueueBody()));
        _tabs.Items.Add(MkTab(TabErrors, MakeErrBody()));
        _tabs.Items.Add(MkTab(TabOptions, new ScrollViewer { Content = BuildOptsGrid() }));
        _tabs.Items.Add(MkTab(TabHistory, MakeHistBody()));
        _tabs.Items.Add(MkTab(TabTransfer, BuildTransferBody()));
        Grid.SetRow(_tabs, 0);
        body.Children.Add(_tabs);

        _mini = new Border
        {
            Background = BgPanel,
            BorderBrush = Line,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(12, 8),
            IsVisible = false,
            Child = new StackPanel
            {
                Spacing = 8,
                Children = { (_miniLbl = MkLbl("", 14)), (_miniBar = new ProgressBar { Minimum = 0, Maximum = 100, Height = 16 }) }
            }
        };
        Grid.SetRow(_mini, 0);
        body.Children.Add(_mini);
        return body;
    }

    private Control BuildExplorerTab()
    {
        _left = new FilePane();
        _right = new FilePane();
        _left.FilesDropped += paths => AddWithDest(paths, _right.CurrentPath);
        _right.FilesDropped += paths => AddWithDest(paths, _left.CurrentPath);

        var mid = new StackPanel { Spacing = 6, VerticalAlignment = VerticalAlignment.Center, MinWidth = 56 };
        mid.Children.Add(Mk("⇒", () => PaneCopy(false)));
        mid.Children.Add(Mk("⇐", () => PaneCopy(true)));
        mid.Children.Add(Mk("⇉", () => PaneMove(false)));
        mid.Children.Add(Mk("⇇", () => PaneMove(true)));

        var grid = new Grid
        {
            ColumnDefinitions = { new(GridLength.Star), new(GridLength.Auto), new(GridLength.Star) }
        };
        Grid.SetColumn(_left, 0);
        grid.Children.Add(_left);
        Grid.SetColumn(mid, 1);
        grid.Children.Add(mid);
        Grid.SetColumn(_right, 2);
        grid.Children.Add(_right);
        return grid;
    }

    private Control BuildQueueBody()
    {
        var panel = new StackPanel { Spacing = 6 };

        var fromRow = new Grid { ColumnDefinitions = { new(GridLength.Auto), new(GridLength.Star), new(GridLength.Auto), new(GridLength.Auto) } };
        _lblFrom = MkLbl("", 13);
        Grid.SetColumn(_lblFrom, 0); fromRow.Children.Add(_lblFrom);
        _tbFrom = new TextBox { Margin = new Thickness(4, 0, 4, 0) };
        Grid.SetColumn(_tbFrom, 1); fromRow.Children.Add(_tbFrom);
        var bFrom = Mk("…", () => PickDirInto(_tbFrom));
        Grid.SetColumn(bFrom, 2); fromRow.Children.Add(bFrom);
        var bAdd = Mk("+", () => PickFiles());
        Grid.SetColumn(bAdd, 3); fromRow.Children.Add(bAdd);

        var toRow = new Grid { ColumnDefinitions = { new(GridLength.Auto), new(GridLength.Star), new(GridLength.Auto) } };
        var lblTo = MkLbl("", 13);
        Grid.SetColumn(lblTo, 0); toRow.Children.Add(lblTo);
        _tbTo = new TextBox { Margin = new Thickness(4, 0, 4, 0) };
        Grid.SetColumn(_tbTo, 1); toRow.Children.Add(_tbTo);
        var bTo = Mk("…", () => PickDirInto(_tbTo));
        Grid.SetColumn(bTo, 2); toRow.Children.Add(bTo);

        _lbQueue = new ListBox { SelectionMode = SelectionMode.Multiple, MinHeight = 190 };
        DragDrop.SetAllowDrop(_lbQueue, true);
        _lbQueue.AddHandler(DragDrop.DragOverEvent, (EventHandler<DragEventArgs>)((s, e) => { if (e.Data.Contains(DataFormats.Files)) e.DragEffects = DragDropEffects.Copy; }));
        _lbQueue.AddHandler(DragDrop.DropEvent, (EventHandler<DragEventArgs>)((s, e) =>
        {
            if (!e.Data.Contains(DataFormats.Files)) return;
            var fl = e.Data.GetFiles()?.Select(x => x.Path.LocalPath).ToArray();
            if (fl is { Length: > 0 }) AddFiles(fl);
        }));
        _lbQueue.ItemTemplate = new FuncDataTemplate<CopyItem>((it, _ns) => BuildQueueRow(it));

        var gl = new Grid { ColumnDefinitions = { new(GridLength.Auto), new(GridLength.Auto), new(GridLength.Star) } };
        _lblProg = MkLbl("0%", 13);
        Grid.SetColumn(_lblProg, 0); gl.Children.Add(_lblProg);
        _lblRate = MkLbl("", 13);
        Grid.SetColumn(_lblRate, 1); gl.Children.Add(_lblRate);
        _lblTime = MkLbl("", 13);
        _lblTime.HorizontalAlignment = HorizontalAlignment.Right;
        Grid.SetColumn(_lblTime, 2); gl.Children.Add(_lblTime);

        _ggBar = new ProgressBar { Minimum = 0, Maximum = 100, Height = 16 };
        _lblCur = MkLbl("", 13);

        var btns = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Margin = new Thickness(0, 4, 0, 0) };
        btns.Children.Add(_bCopy = Mk("copy", () => _ = StartCopy(false)));
        _bCopy.Classes.Add("primary");
        btns.Children.Add(_bMove = Mk("move", () => _ = StartCopy(true)));
        btns.Children.Add(_bPause = Mk("pause", Pause));
        btns.Children.Add(_bResume = Mk("resume", Resume));
        btns.Children.Add(_bSkip = Mk("skip", SkipRest));
        btns.Children.Add(_bCancel = Mk("cancel", () => _engine?.Cancel()));
        btns.Children.Add(_bClear = Mk("clear", Clear));

        panel.Children.Add(fromRow);
        panel.Children.Add(toRow);
        panel.Children.Add(_lbQueue);
        panel.Children.Add(gl);
        panel.Children.Add(_ggBar);
        panel.Children.Add(_lblCur);
        panel.Children.Add(btns);

        Bind(_lblFrom, "histFrom");
        Bind(lblTo, "histTo");
        Bind(_bCopy, "copy");
        Bind(_bMove, "move");
        Bind(_bPause, "pause");
        Bind(_bResume, "resume");
        Bind(_bSkip, "skip");
        Bind(_bCancel, "cancel");
        Bind(_bClear, "clear");
        return panel;
    }

    private static Control BuildQueueRow(CopyItem it)
    {
        var grid = new Grid { ColumnDefinitions = { new ColumnDefinition(new GridLength(200)), new ColumnDefinition(new GridLength(90)), new ColumnDefinition(GridLength.Star), new ColumnDefinition(new GridLength(150)), new ColumnDefinition(new GridLength(120)) } };
        var name = new TextBlock { TextTrimming = TextTrimming.CharacterEllipsis, VerticalAlignment = VerticalAlignment.Center, FontWeight = it.IsDirectory ? FontWeight.SemiBold : FontWeight.Normal };
        name.Bind(TextBlock.TextProperty, new Binding("Name"));
        Grid.SetColumn(name, 0); grid.Children.Add(name);
        var size = new TextBlock { HorizontalAlignment = HorizontalAlignment.Right };
        size.Bind(TextBlock.TextProperty, new Binding("SizeText"));
        Grid.SetColumn(size, 1); grid.Children.Add(size);
        var bar = new ProgressBar { Minimum = 0, Maximum = 100, Height = 12 };
        bar.Bind(ProgressBar.ValueProperty, new Binding("Percent"));
        Grid.SetColumn(bar, 2); grid.Children.Add(bar);
        var state = new TextBlock { Foreground = TextSoft, TextTrimming = TextTrimming.CharacterEllipsis };
        state.Bind(TextBlock.TextProperty, new Binding("StateText"));
        Grid.SetColumn(state, 3); grid.Children.Add(state);
        var dst = new TextBlock { Foreground = TextSoft, TextTrimming = TextTrimming.CharacterEllipsis };
        dst.Bind(TextBlock.TextProperty, new Binding("DestName"));
        Grid.SetColumn(dst, 4); grid.Children.Add(dst);
        return grid;
    }

    private Control MakeErrBody()
    {
        var panel = new StackPanel { Spacing = 6 };
        _lbErrs = new ListBox { MinHeight = 240 };
        _lbErrs.ItemTemplate = new FuncDataTemplate<string>((s, _ns) => new TextBlock { Text = s, TextWrapping = TextWrapping.Wrap, Foreground = TextSoft });
        panel.Children.Add(_lbErrs);
        var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        var bClr = Mk("clear", () => { _errLines.Clear(); _lbErrs.ItemsSource = null; });
        row.Children.Add(bClr);
        panel.Children.Add(row);
        return panel;
    }

    private Control BuildOptsGrid()
    {
        var g = new Grid
        {
            ColumnDefinitions = { new(GridLength.Star), new(GridLength.Auto) },
            Margin = new Thickness(10)
        };
        var col = new StackPanel { Spacing = 8 };

        var afterCodes = new[] { "close", "keep", "keepIfErrors" };
        _cmbAfter = new ComboBox { ItemsSource = new[] { L.Get("afterClose"), L.Get("afterKeep"), L.Get("afterKeepErr") }, SelectedIndex = Math.Max(0, Array.IndexOf(afterCodes, S.AfterDone)) };
        _cmbAfter.SelectionChanged += (s, e) => { S.AfterDone = afterCodes[Math.Clamp(_cmbAfter.SelectedIndex, 0, 2)]; S.Save(); };

        _cmbLang = new ComboBox { ItemsSource = Ex.LangNames(), SelectedIndex = Ex.IndexOf(S.Lang) };
        _cmbLang.SelectionChanged += (s, e) => { if (_cmbLang.SelectedIndex >= 0 && _cmbLang.SelectedIndex < Ex.Codes.Length) ApplyLang(Ex.Codes[_cmbLang.SelectedIndex]); };

        _cmbEngine = new ComboBox { ItemsSource = new[] { "native", "buffered" }, SelectedItem = S.Engine };
        _cmbEngine.SelectionChanged += (s, e) => { S.Engine = (_cmbEngine.SelectedItem?.ToString() ?? "native") == "buffered" ? "buffered" : "native"; S.Save(); };

        _cmbPriority = new ComboBox { ItemsSource = new[] { "idle", "normal", "high" }, SelectedItem = S.Priority };
        _cmbPriority.SelectionChanged += (s, e) => { S.Priority = _cmbPriority.SelectedItem?.ToString() ?? "normal"; S.Save(); };

        _sldThreads = new Slider { Minimum = 1, Maximum = 32, TickFrequency = 1, IsSnapToTickEnabled = true, Value = S.Threads };
        _lblThreads = MkLbl(S.Threads.ToString(), 13);
        _sldThreads.ValueChanged += (s, e) => { S.Threads = (int)e.NewValue; _lblThreads.Text = ((int)e.NewValue).ToString(); S.SaveSoon(); };

        _tbBuffer = new TextBox { Text = (S.BufferBytes / 1024).ToString(), Width = 100 };
        _tbBuffer.TextChanged += (s, e) => { if (long.TryParse(_tbBuffer.Text, out var v) && v >= 16) { S.BufferBytes = v * 1024; S.SaveSoon(); } };
        _tbRetry = new TextBox { Text = S.RetryIntervalMs.ToString(), Width = 100 };
        _tbRetry.TextChanged += (s, e) => { if (int.TryParse(_tbRetry.Text, out var v) && v > 0) { S.RetryIntervalMs = v; S.SaveSoon(); } };
        _tbSpeed = new TextBox { Text = S.SpeedLimitKb.ToString(), Width = 110 };
        _tbSpeed.TextChanged += (s, e) => { if (long.TryParse(_tbSpeed.Text, out var v)) { S.SpeedLimitKb = v; S.SaveSoon(); } };
        _chkLimit = MkChk("", S.SpeedLimitEnabled, b => { S.SpeedLimitEnabled = b; S.Save(); });

        var bufPresets = new long[] { 64, 256, 1024, 4096, 16384, 65536 };
        _cmbBuffer = new ComboBox { Width = 120 };
        _cmbBuffer.ItemsSource = bufPresets.Select(x => $"{x} KB").ToArray();
        int bufIdx = Math.Max(0, Array.IndexOf(bufPresets, Math.Max(64, S.BufferBytes / 1024)));
        _cmbBuffer.SelectedIndex = bufIdx;
        _cmbBuffer.SelectionChanged += (s, e) => { if (_cmbBuffer.SelectedIndex >= 0) { S.BufferBytes = bufPresets[_cmbBuffer.SelectedIndex] * 1024; _tbBuffer.Text = (S.BufferBytes / 1024).ToString(); S.Save(); } };

        var sizeNames = new[] { "B", "KB", "MB", "GB", "TB", "auto" };
        var su = string.IsNullOrEmpty(S.SizeUnit) ? "auto" : S.SizeUnit.ToUpperInvariant();
        if (!sizeNames.Contains(su)) su = "auto";
        _cmbSizeUnit = new ComboBox { ItemsSource = sizeNames, SelectedItem = su };
        _cmbSizeUnit.SelectionChanged += (s, e) => { var v = _cmbSizeUnit.SelectedItem?.ToString() ?? "auto"; S.SizeUnit = v == "auto" ? "" : v; FilePane.SizeUnit = S.SizeUnit; S.Save(); };

        var addCodes = new[] { "never", "always", "sameSource", "sameDest", "both", "either" };
        _cmbAddWhen = new ComboBox();
        _cmbAddWhen.SelectedIndex = Math.Max(0, Array.IndexOf(addCodes, S.AddListsWhen));
        _cmbAddWhen.SelectionChanged += (s, e) => { S.AddListsWhen = addCodes[Math.Clamp(_cmbAddWhen.SelectedIndex, 0, addCodes.Length - 1)]; S.Save(); };

        _sldSpeed = new Slider { Minimum = 0, Maximum = 100, TickFrequency = 1, IsSnapToTickEnabled = true };
        _lblSpeed = MkLbl("", 12);
        _sldSpeed.Value = Math.Clamp((int)Math.Round(S.SpeedLimitKb / 1024.0), 0, 100);
        _lblSpeed.Text = ((int)_sldSpeed.Value).ToString() + " MB/s";
        _sldSpeed.ValueChanged += (s, e) => { _lblSpeed.Text = ((long)e.NewValue).ToString() + " MB/s"; _tbSpeed.Text = ((long)e.NewValue * 1024).ToString(); S.SpeedLimitKb = (long)e.NewValue * 1024; S.SaveSoon(); };

        _tbUpdate = new TextBox { Text = S.WindowUpdateMs.ToString(), Width = 100 };
        _tbUpdate.TextChanged += (s, e) => { if (int.TryParse(_tbUpdate.Text, out var v) && v >= 50) { S.WindowUpdateMs = v; _ticker.Interval = TimeSpan.FromMilliseconds(v); S.SaveSoon(); } };
        _tbAvg = new TextBox { Text = S.SpeedAvgMs.ToString(), Width = 100 };
        _tbAvg.TextChanged += (s, e) => { if (int.TryParse(_tbAvg.Text, out var v) && v >= 100) { S.SpeedAvgMs = v; S.SaveSoon(); Tick(); } };
        _tbThrottle = new TextBox { Text = S.ThrottleMs.ToString(), Width = 100 };
        _tbThrottle.TextChanged += (s, e) => { if (int.TryParse(_tbThrottle.Text, out var v) && v >= 0) { S.ThrottleMs = v; S.SaveSoon(); } };
        _tbWarn = new TextBox { Text = S.DiskWarnMb.ToString(), Width = 110 };
        _tbWarn.TextChanged += (s, e) => { if (long.TryParse(_tbWarn.Text, out var v) && v >= 0) { S.DiskWarnMb = v; S.SaveSoon(); } };
        _tbNewPat = new TextBox { Text = S.RenameNewPattern, Width = 240 };
        _tbNewPat.TextChanged += (s, e) => { S.RenameNewPattern = string.IsNullOrWhiteSpace(_tbNewPat.Text) ? "%NAME% (%COPY%)%EXT%" : _tbNewPat.Text; S.SaveSoon(); };

        _chkVerify = MkChk(Ex.Get("verify"), S.VerifyChecksum, b => { S.VerifyChecksum = b; S.Save(); });
        _chkActivate = MkChk(Ex.Get("activateOnStart"), S.ActivateOnStart, b => { S.ActivateOnStart = b; S.Save(); });
        _chkAskAdd = MkChk(Ex.Get("confirmAdd"), S.AskConfirm, b => { S.AskConfirm = b; S.Save(); });
        _chkLog = MkChk(Ex.Get("autoSaveLog"), S.SaveLog, b => { S.SaveLog = b; S.Save(); });

        var collCodes = new[] { "ask", "cancel", "skip", "resume", "overwrite", "overwriteIfDifferent", "renameNew", "renameOld" };
        var collNames = collCodes.Select(c => c switch { "ask" => L.Get("colAsk"), "cancel" => L.Get("colCancelAll"), "skip" => L.Get("skip"), "resume" => L.Get("resume"), "overwrite" => L.Get("overwrite"), "overwriteIfDifferent" => L.Get("overwriteDiff"), "renameNew" => L.Get("rename"), _ => L.Get("rename") }).ToArray();
        _cmbCollision = new ComboBox { ItemsSource = collNames, SelectedIndex = Math.Max(0, Array.IndexOf(collCodes, S.CollisionDefault)) };
        _cmbCollision.SelectionChanged += (s, e) => { S.CollisionDefault = collCodes[Math.Clamp(_cmbCollision.SelectedIndex, 0, collCodes.Length - 1)]; S.Save(); };

        var errCodes = new[] { "ask", "cancel", "skip", "retry", "bottom" };
        var errNames = errCodes.Select(c => c switch { "ask" => L.Get("errAsk"), "cancel" => L.Get("errCancelD"), "skip" => L.Get("skip"), "retry" => L.Get("retry"), _ => L.Get("retry") }).ToArray();
        _cmbError = new ComboBox { ItemsSource = errNames, SelectedIndex = Math.Max(0, Array.IndexOf(errCodes, S.ErrorDefault)) };
        _cmbError.SelectionChanged += (s, e) => { S.ErrorDefault = errCodes[Math.Clamp(_cmbError.SelectedIndex, 0, errCodes.Length - 1)]; S.Save(); };

        void Add(string key, Control c)
        {
            var lb = MkLbl(L.Get(key), 13);
            var row = new Grid { ColumnDefinitions = { new(GridLength.Auto), new(GridLength.Star) }, Margin = new Thickness(0, 2, 0, 2) };
            Grid.SetColumn(lb, 0);
            Grid.SetColumn(c, 1);
            row.Children.Add(lb);
            row.Children.Add(c);
            col.Children.Add(row);
            _texts.Add((key, () => { try { lb.Text = L.Get(key); } catch { } }));
        }

        void Sec(string key)
        {
            var t = MkLbl(L.Get(key), 13);
            t.FontWeight = FontWeight.Bold;
            t.Foreground = Gold;
            col.Children.Add(t);
            _texts.Add((key, () => { try { t.Text = L.Get(key); } catch { } }));
        }

        void AddEx(string key, Control c)
        {
            var lb = MkLbl(Ex.Get(key), 13);
            var row = new Grid { ColumnDefinitions = { new(GridLength.Auto), new(GridLength.Star) }, Margin = new Thickness(0, 2, 0, 2) };
            Grid.SetColumn(lb, 0);
            Grid.SetColumn(c, 1);
            row.Children.Add(lb);
            row.Children.Add(c);
            col.Children.Add(row);
            _texts.Add((key, () => { try { lb.Text = Ex.Get(key); } catch { } }));
        }

        void AddChk(CheckBox cb, string key)
        {
            col.Children.Add(cb);
            _texts.Add((key, () => { try { cb.Content = Ex.Get(key); } catch { } }));
        }

        Add("sLanguage", _cmbLang);
        Add("units", _cmbSizeUnit);
        Sec("afterDone"); col.Children.Add(_cmbAfter);
        Sec("speedLimit"); col.Children.Add(new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6, Children = { _chkLimit, _sldSpeed, _tbSpeed, _lblSpeed } });
        Sec("collisionDefault"); col.Children.Add(_cmbCollision);
        Sec("errorDefault"); col.Children.Add(_cmbError); Add("retryInterval", _tbRetry);
        Sec("addListsWhen"); col.Children.Add(_cmbAddWhen); AddChk(_chkAskAdd, "confirmAdd");
        Sec("rename"); col.Children.Add(_tbNewPat);
        Sec("sAdvanced");
        Add("engineType", _cmbEngine);
        Add("threads", new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6, Children = { _sldThreads, _lblThreads } });
        Add("buffer", new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6, Children = { _cmbBuffer, _tbBuffer } });
        Add("interval", _tbUpdate);
        Add("speedAvg", _tbAvg);
        Add("throttle", _tbThrottle);
        Add("priority", _cmbPriority);
        AddEx("diskWarn", _tbWarn);
        AddChk(_chkVerify, "verify");

        var checks = new StackPanel { Spacing = 4 };
        var chkTray = MkChk("", S.TrayIcon, b => { S.TrayIcon = b; S.Save(); BuildTray(); });
        var chkStart = MkChk("", S.StartWithWindows, b => { S.StartWithWindows = b; S.Save(); if (!OperatingSystem.IsAndroid()) ApplyStartWithWindows(b); });
        var chkAttrib = MkChk("", S.CopyAttributes, b => { S.CopyAttributes = b; S.Save(); });
        var chkSec = MkChk("", S.CopySecurity, b => { S.CopySecurity = b; S.Save(); });
        var chkDel = MkChk("", S.DeleteUnfinished, b => { S.DeleteUnfinished = b; S.Save(); });
        var chkKeep = MkChk("", S.KeepOnError, b => { S.KeepOnError = b; S.Save(); });
        var chkRO = MkChk("", S.OverwriteReadOnly, b => { S.OverwriteReadOnly = b; S.Save(); });
        var chkHidden = MkChk("", S.SkipHiddenSystem, b => { S.SkipHiddenSystem = b; S.Save(); });
        var chkTitle = MkChk("", S.ShowInTitle, b => { S.ShowInTitle = b; S.Save(); });
        void SecC(string key)
        {
            var t = MkLbl(L.Get(key), 13);
            t.FontWeight = FontWeight.Bold;
            t.Foreground = Gold;
            checks.Children.Add(t);
            _texts.Add((key, () => { try { t.Text = L.Get(key); } catch { } }));
        }
        void BindEx(CheckBox cb, string key)
        {
            checks.Children.Add(cb);
            _texts.Add((key, () => { try { cb.Content = Ex.Get(key); } catch { } }));
        }
        SecC("sStartup");
        Bind(chkStart, "startWithWindows");
        BindEx(_chkActivate, "activateOnStart");
        SecC("sUI");
        Bind(chkTray, "minToTray");
        Bind(chkTitle, "showInTitle");
        SecC("copyAttribs");
        Bind(chkAttrib, "copyAttribs");
        Bind(chkSec, "copySecurity");
        Bind(chkRO, "overwriteRO");
        Bind(chkHidden, "skipHidden");
        SecC("delUnfinished");
        Bind(chkDel, "delUnfinished");
        Bind(chkKeep, "keepOnError");
        SecC("sLog");
        BindEx(_chkLog, "autoSaveLog");
        Bind(_chkLimit, "enabled");

        var bottom = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Margin = new Thickness(0, 10, 0, 0) };
        var bSave = Mk("apply", () => { S.Save(); _lblStatus.Text = L.Get("ok"); });
        var bDef = Mk("sDefaults", () => { S = Settings.Load(); S.Save(); ReloadTexts(); });
        bottom.Children.Add(bSave);
        bottom.Children.Add(bDef);

        col.Children.Add(bottom);

        var about = new StackPanel { Spacing = 6, Margin = new Thickness(0, 16, 0, 0) };
        var aboutT = MkLbl("Acerca de QBasCopier y Transfer", 13);
        aboutT.FontWeight = FontWeight.Bold;
        aboutT.Foreground = Gold;
        about.Children.Add(aboutT);
        try
        {
            using var s = AssetLoader.Open(new Uri("avares://QBasCopier/Assets/logo.png"));
            about.Children.Add(new Image { Source = new Bitmap(s), Width = 96, Height = 96, Stretch = Stretch.Uniform, Margin = new Thickness(0, 2, 0, 0) });
        }
        catch { }
        var aboutBody = MkLbl(AboutText, 12);
        aboutBody.TextWrapping = TextWrapping.Wrap;
        aboutBody.MaxWidth = 460;
        aboutBody.Tint(TextSoft);
        about.Children.Add(aboutBody);
        col.Children.Add(about);

        var foot = new StackPanel { Spacing = 3, Margin = new Thickness(0, 14, 0, 0) };
        foot.Children.Add(MkLbl("Creado por QBaswing Designer · 2026", 11).Tint(TextSoft));
        foot.Children.Add(MkLbl("Asociado a Freeman y Diseños", 11).Tint(TextSoft));
        foot.Children.Add(MkLbl("Todos los derechos reservados © 2026", 11).Tint(TextSoft));
        col.Children.Add(foot);

        Grid.SetColumn(col, 0);
        g.Children.Add(col);
        Grid.SetColumn(checks, 1);
        g.Children.Add(checks);
        return g;
    }

    private Control MakeHistBody()
    {
        var panel = new StackPanel { Spacing = 6 };
        _lbHist = new ListBox { MinHeight = 240 };
        _lbHist.ItemTemplate = new FuncDataTemplate<HistoryEntry>((h, _ns) =>
        {
            var tp = new StackPanel { Spacing = 2 };
            tp.Children.Add(MkLbl($"{h.Time}  {h.Result}   {h.Source} → {h.Dest}", 12));
            tp.Children.Add(MkLbl(FilePane.Human(h.DoneBytes), 12).Tint(TextSoft));
            tp.Children.Add(new Border { Background = Line, Height = 1, Margin = new Thickness(0, 2, 0, 0) });
            return tp;
        });
        panel.Children.Add(_lbHist);
        var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        var bClr = Mk("histClear", async () => { await HistoryStore.ClearAsync(); RefreshHistory(); });
        var bRef = Mk("⟳", RefreshHistory);
        row.Children.Add(bClr);
        row.Children.Add(bRef);
        panel.Children.Add(row);
        return panel;
    }

    private static void WriteLog(string line)
    {
        try { File.AppendAllText(Path.Combine(Settings.Dir, "errors.log"), line + Environment.NewLine); } catch { }
    }

    private async void RefreshHistory()
    {
        _history.Clear();
        _history.AddRange(await HistoryStore.LoadAsync());
        _lbHist.ItemsSource = null;
        _lbHist.ItemsSource = _history;
    }

    private Control BuildStatusbar()
    {
        var bar = new Grid { ColumnDefinitions = { new(GridLength.Star), new(GridLength.Auto) }, Margin = new Thickness(8, 0, 8, 3) };
        _lblStatus = MkLbl("QBasCopier y Transfer 1.2.0", 12);
        Grid.SetColumn(_lblStatus, 0);
        bar.Children.Add(_lblStatus);
        var ver = MkLbl("© 2026", 12);
        ver.HorizontalAlignment = HorizontalAlignment.Right;
        Grid.SetColumn(ver, 1);
        bar.Children.Add(ver);
        return bar;
    }

    // ------------------------------------------------------------------ bind/tex
    private TabItem MkTab(string tag, Control c) => new() { Tag = tag, Header = "…", Content = c };

    private void Bind(Control c, string key)
    {
        if (c is Button b) _texts.Add((key, () => { try { b.Content = L.Get(key); } catch { } }));
        else if (c is TextBlock t) _texts.Add((key, () => { try { t.Text = L.Get(key); } catch { } }));
        else if (c is CheckBox ch) _texts.Add((key, () => { try { ch.Content = L.Get(key); } catch { } }));
    }

    private void ApplyLang(string code)
    {
        S.Lang = Code(code);
        L.SetLanguage(S.Lang);
        S.Save();
        ReloadTexts();
    }

    public void ReloadTexts()
    {
        foreach (var (_, set) in _texts) { try { set(); } catch { } }
        if (_tabs != null)
            foreach (var o in _tabs.Items)
                if (o is TabItem ti)
                    ti.Header = ti.Tag?.ToString() switch
                    {
                        TabExplorer => Ex.Get("tabExplorer"),
                        TabQueue => L.Get("tabCopyList"),
                        TabErrors => L.Get("tabErrors"),
                        TabOptions => L.Get("tabInterface"),
                        TabHistory => L.Get("tabHistory"),
                        _ => ti.Header
                    };
        if (_lbErrs != null) { _lbErrs.ItemsSource = null; _lbErrs.ItemsSource = _errLines; }
        RefreshIntegrationButton();
        if (_bFold != null) _bFold.Content = Ex.Get("fold");
        if (_bQuit != null) _bQuit.Content = L.Get("quit");
        if (_lblStatus != null) _lblStatus.Text = StatusText();
        if (_cmbLang != null) _cmbLang.SelectedIndex = Ex.IndexOf(S.Lang);
        if (_cmbLangQuick != null) _cmbLangQuick.SelectedIndex = Ex.IndexOf(S.Lang);
        FilePane.SizeUnit = S.SizeUnit;
        RebuildOptionLists();
    }

    private void RebuildOptionLists()
    {
        try
        {
            if (_cmbAfter != null)
            {
                int i = _cmbAfter.SelectedIndex;
                _cmbAfter.ItemsSource = new[] { L.Get("afterClose"), L.Get("afterKeep"), L.Get("afterKeepErr") };
                _cmbAfter.SelectedIndex = i;
            }
            if (_cmbCollision != null)
            {
                int i = _cmbCollision.SelectedIndex;
                _cmbCollision.ItemsSource = new[] { L.Get("colAsk"), L.Get("colCancelAll"), L.Get("skip"), L.Get("resume"), L.Get("overwrite"), L.Get("overwriteDiff"), L.Get("rename"), L.Get("rename") };
                _cmbCollision.SelectedIndex = i;
            }
            if (_cmbError != null)
            {
                int i = _cmbError.SelectedIndex;
                _cmbError.ItemsSource = new[] { L.Get("errAsk"), L.Get("errCancelD"), L.Get("skip"), L.Get("retry"), L.Get("retry") };
                _cmbError.SelectedIndex = i;
            }
            if (_cmbAddWhen != null)
            {
                int i = _cmbAddWhen.SelectedIndex;
                _cmbAddWhen.ItemsSource = new[] { L.Get("lstNever"), L.Get("lstAlways"), L.Get("lstSameSource"), L.Get("lstSameDest"), L.Get("lstBoth"), L.Get("lstEither") };
                if (i < 0) i = Math.Max(0, Array.IndexOf(new[] { "never", "always", "sameSource", "sameDest", "both", "either" }, S.AddListsWhen));
                _cmbAddWhen.SelectedIndex = i;
            }
            if (_cmbBuffer != null)
            {
                int i = _cmbBuffer.SelectedIndex;
                _cmbBuffer.ItemsSource = new long[] { 64, 256, 1024, 4096, 16384, 65536 }.Select(x => $"{x} KB").ToArray();
                if (i >= 0) _cmbBuffer.SelectedIndex = i;
            }
        }
        catch { }
    }

    private void RefreshIntegrationButton()
    {
        if (_bIntegrate == null) return;
#if !ANDROID
        bool on = OperatingSystem.IsWindows() && ExplorerIntegration.IsInstalled;
#else
        bool on = false;
#endif
        _bIntegrate.Content = on ? Ex.Get("unintegrate") : Ex.Get("integrate");
    }

    private void ToggleIntegration()
    {
#if ANDROID
        return;
#else
        if (!OperatingSystem.IsWindows())
        {
            _lblStatus.Text = Ex.Get("integrate") + " (Linux/macOS)";
            return;
        }
        if (ExplorerIntegration.IsInstalled) ExplorerIntegration.Uninstall();
        else ExplorerIntegration.Install();
        RefreshIntegrationButton();
        _lblStatus.Text = L.Get("ok");
#endif
    }

    private static void ApplyStartWithWindows(bool on)
    {
#if !ANDROID
        if (OperatingSystem.IsWindows()) ExplorerIntegration.SetStartWithWindows(on);
#endif
    }

    private void ToggleFold()
    {
        if (_mini == null || _tabs == null) return;
        bool t = _tabs.IsVisible;
        _tabs.IsVisible = !t;
        _mini.IsVisible = !t;
        _bFold.Content = t ? Ex.Get("fold") : Ex.Get("fold");
    }

    private string StatusText()
    {
        var src = _tbFrom?.Text ?? "";
        var dst = _tbTo?.Text ?? "";
        var run = _running ? " · " + L.Get("done") : "";
        return $"{src} → {dst}{run}".Trim();
    }

    // ------------------------------------------------------------------ engine
    private void Tick()
    {
        if (_engine != null && (_engine.IsBusy || _running))
        {
            long done = _engine.DoneBytes, total = _engine.TotalBytes;
            long now = Environment.TickCount64;
            if (_lastTickTicks != 0)
            {
                long dt = now - _lastTickTicks;
                if (dt > 400)
                {
                    double inst = (done - _lastDone) * 1000.0 / dt;
                    _lastRate = _lastRate <= 0 ? inst : _lastRate * 0.6 + inst * 0.4;
                    _lastDone = done;
                }
            }
            _lastTickTicks = now;
            double pct = total > 0 ? Math.Min(100, done * 100.0 / total) : 0;
            _ggBar.Maximum = Math.Max(100, total);
            _ggBar.Value = done;
            _lblProg.Text = pct.ToString("0.0") + "%";
            _lblRate.Text = L.Get("speed") + ": " + FilePane.Human((long)_lastRate) + "/s";
            double left = _lastRate > 0 ? (total - done) / _lastRate : 0;
            _lblTime.Text = L.Get("remaining") + ": " + FmtTime(left) + " · " + L.Get("elapsed") + ": " + FmtTime(_batchSw.Elapsed.TotalSeconds);
            var cur = _engine.Items.FirstOrDefault(x => x.State == ItemState.Copying);
            _lblCur.Text = cur != null ? $"{L.Get("currentFile")}: {cur.Name}" : "";
            _miniLbl.Text = _lblProg.Text + "  " + _lblRate.Text;
            _miniBar.Value = pct;
            #if !ANDROID
            if (S.ShowInTitle && Host != null) Host.Title = $"QBasCopier y Transfer · {pct:0.#}%";
#endif
        }
        else
        {
            _lastTickTicks = 0;
#if !ANDROID
            if (S.ShowInTitle && Host != null) Host.Title = "QBasCopier y Transfer";
#endif
        }

        if (_trRx != null && _trSrv.Running)
        {
            _trRx.Text = "Recibidos en sesión: " + FilePane.Human(_trRxBytes);
            if (NetTools.Now - _trLastFill > 3000) { _trLastFill = NetTools.Now; FillPeers(); }
        }

        var cmd = Program.WaitCommand(0);
        if (cmd != null) ProcessForwarded(cmd);
    }

    private static string FmtTime(double sec)
    {
        sec = Math.Max(0, sec);
        var ts = TimeSpan.FromSeconds(sec);
        return ts.TotalHours >= 1 ? $"{(int)ts.TotalHours}h {ts.Minutes}m {ts.Seconds}s" : $"{ts.Minutes}m {ts.Seconds}s";
    }

    // ------------------------------------------------------------------ cola
    private void AddFiles(string[] paths) => Enqueue(paths, _tbTo?.Text ?? "", false);
    private void AddWithDest(string[] paths, string dest) { AddFiles(paths); if (!string.IsNullOrEmpty(dest)) _tbTo.Text = dest; _tabs.SelectedIndex = 1; }

    private void Enqueue(string[] paths, string dest, bool move)
    {
        bool busy = _engine != null && _engine.IsBusy;
        if (busy && !MayAddWhileBusy(paths, dest))
        {
            if (S.AskConfirm) { _ = ConfirmAddAsync(paths, dest, move); return; }
            _lblStatus.Text = L.Get("busy");
            return;
        }
        if (_engine == null || !_engine.IsBusy) _engine = null;
        _tbFrom.Text = string.Join("; ", paths.Take(5)) + (paths.Length > 5 ? " …" : "");
        if (!string.IsNullOrEmpty(dest)) _tbTo.Text = dest;
        foreach (var p in paths)
        {
            if (string.IsNullOrEmpty(p)) continue;
            try
            {
                bool isContent = p.StartsWith("content://", StringComparison.OrdinalIgnoreCase);
                if (isContent)
                {
                    _queue.Add(new CopyItem { SourcePath = p, IsDirectory = false, IsContent = true });
                    continue;
                }
                bool isDir = Directory.Exists(p) && !File.Exists(p);
                if (isDir || File.Exists(p))
                    _queue.Add(new CopyItem { SourcePath = p, IsDirectory = isDir });
            }
            catch { }
        }
        _queue.Sort((a, b) => b.IsDirectory.CompareTo(a.IsDirectory));
    }

    private bool MayAddWhileBusy(string[] srcs, string dest)
    {
        switch (S.AddListsWhen)
        {
            case "never": return false;
            case "always": return true;
            case "sameSource": return srcs.All(s => SameRoot(s, _tbFrom.Text));
            case "sameDest": return SameRoot(dest, _tbTo.Text);
            case "both": return SameRoot(dest, _tbTo.Text) && srcs.All(s => SameRoot(s, _tbFrom.Text));
            case "either": return SameRoot(dest, _tbTo.Text) || srcs.All(s => SameRoot(s, _tbFrom.Text));
        }
        return true;
    }

    private static bool SameRoot(string a, string b)
    {
        try
        {
            return Path.GetPathRoot(a ?? "")?.TrimEnd('\\', '/') == Path.GetPathRoot(b ?? "")?.TrimEnd('\\', '/');
        }
        catch { return (a ?? "") == (b ?? ""); }
    }

    private async Task ConfirmAddAsync(string[] paths, string dest, bool move)
    {
        var bOk = new Button { Content = L.Get("ok") };
        var bNo = new Button { Content = L.Get("cancel") };
        var res = await AskAsync<bool>(L.Get("addListsWhen"), 380, 170, done =>
        {
            bOk.Click += (_, _) => done(true);
            bNo.Click += (_, _) => done(false);
            return new StackPanel
            {
                Spacing = 14,
                Children =
                {
                    new TextBlock { Text = L.Get("askConfirm"), TextWrapping = TextWrapping.Wrap, FontSize = 13 },
                    new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, HorizontalAlignment = HorizontalAlignment.Right, Children = { bOk, bNo } }
                }
            };
        });
        if (res == true) Enqueue(paths, dest, move);
    }

    private void RefreshQueueUi()
    {
        if (_engine == null) return;
        foreach (var it in _queue)
            if (_engine.Items.Contains(it) == false)
                _engine.Items.Add(it);
        _lbQueue.ItemsSource = _queue;
        _lblStatus.Text = StatusText();
    }

    private void Pause() { _engine?.Pause(); _bPause.IsEnabled = false; _bResume.IsEnabled = true; }
    private void Resume() { _engine?.Resume(); _bPause.IsEnabled = true; _bResume.IsEnabled = false; }

    private void SkipRest()
    {
        if (_engine == null) return;
        foreach (var it in _engine.Items)
            if (it.State == ItemState.Ready) { it.State = ItemState.Skipped; it.StateText = L.Get("stateSkipped"); }
    }

    private void Clear()
    {
        if (_engine?.IsBusy == true) return;
        _queue.Clear();
        _engine?.Items.Clear();
        _lbQueue.ItemsSource = null;
        _ggBar.Value = 0;
        _lblProg.Text = "0%";
        _lblRate.Text = _lblTime.Text = _lblCur.Text = "";
    }

    private async Task StartCopy(bool move)
    {
        if (_queue.Count == 0) { _lblStatus.Text = L.Get("histEmpty"); return; }
        var dest = _tbTo.Text?.Trim();
        if (string.IsNullOrEmpty(dest))
        {
            _lblStatus.Text = L.Get("destination") + "?";
            return;
        }
        bool safDest = CopyEngine.IsSafPath(dest);
        if (!safDest)
        {
            // Con un destino content:// esto siempre fallaba y la copia no arrancaba nunca.
            try { Directory.CreateDirectory(dest); }
            catch { _lblStatus.Text = L.Get("errTitle"); return; }
        }

        if (S.DiskWarnMb > 0 && !safDest)
        {
            try
            {
                long needed = 0;
                foreach (var q in _queue)
                    if (!q.IsDirectory && !q.IsContent) needed += SourceSizeOf(q);
                var drv = new DriveInfo(Path.GetPathRoot(Path.GetFullPath(dest).TrimEnd('/', '\\')) ?? dest);
                if (drv.AvailableFreeSpace - needed < S.DiskWarnMb * 1024L * 1024L)
                    _lblStatus.Text = Ex.Get("warnSpace");
            }
            catch { }
        }

        if (_engine == null || _engine.IsBusy)
        {
            _engine = new CopyEngine(S);
            _engine.AskCollision = PickCollisionAsync;
            _engine.AskError = PickErrorAsync;
            _engine.LogMessage += line => _ = Dispatcher.UIThread.InvokeAsync(() => { _errLines.Add(line); _lbErrs.ItemsSource = null; _lbErrs.ItemsSource = _errLines; if (S.SaveLog) WriteLog(line); });
            _engine.BatchEnd += (ok, err, cancelled, paused) => _ = Dispatcher.UIThread.InvokeAsync(() => OnBatchEnd(ok, err, cancelled));
        }
        RefreshQueueUi();

        foreach (var it in _queue)
        {
            it.DestDir = dest;
            it.Rel = it.Name;
        }

        _engine.Move = move;
        _batchSw.Restart();
        _lastDone = 0;
        _lastRate = 0;
        _lastTickTicks = 0;
        _running = true;
#if ANDROID
        QBasCopier.Android.MainActivity.Current?.KeepAwake();
#endif
        _bCopy.IsEnabled = _bMove.IsEnabled = false;
        _bPause.IsEnabled = true;
        _bResume.IsEnabled = false;
        _bSkip.IsEnabled = _bCancel.IsEnabled = true;
        _tabs.SelectedIndex = 1;

        try { await _engine.RunAsync(); }
        catch { }

        _running = false;
        _bCopy.IsEnabled = _bMove.IsEnabled = true;
        _bPause.IsEnabled = _bResume.IsEnabled = false;
        _bSkip.IsEnabled = _bCancel.IsEnabled = false;
        _lblStatus.Text = L.Get("done");
#if ANDROID
        QBasCopier.Android.MainActivity.Current?.ReleaseLocks();
#endif
    }

    private static long SourceSizeOf(CopyItem q)
    {
        try { return q.IsDirectory ? 0 : new FileInfo(q.SourcePath).Length; }
        catch { return 0; }
    }

    private async void OnBatchEnd(int ok, int err, bool cancelled)
    {
        var done = _engine?.DoneBytes ?? 0;
        _engine = null;
        await HistoryStore.AppendAsync(_tbFrom.Text ?? "", _tbTo.Text ?? "", cancelled ? L.Get("histCancelled") : err > 0 ? L.Get("histErrors") : L.Get("histOk"), done);
        RefreshHistory();
        if (S.AfterDone == "close" || (S.AfterDone == "keepIfErrors" && err == 0)) DoQuit();
        else BuildTray();
    }

    private void DoQuit()
    {
        _forceClose = true;
        try { Settings.Flush(); S.Save(); } catch { }
#if !ANDROID
        Host?.Close();
#else
        // Al ser MainWindow un UserControl ya no hay Window.Close(): sin esto, la opcion
        // "cerrar al terminar" no hacia nada y la app seguia abierta.
        QBasCopier.Android.MainActivity.Current?.FinishApp();
#endif
    }

    // ----------------------------------------------------------- diálogos
    // En escritorio son una ventana modal; en Android no se puede construir un Window
    // (WindowingPlatformStub.CreateWindow lanza NotSupportedException), asi que se
    // superponen sobre la propia UI. Sin esto la copia se queda esperando para siempre
    // en la primera colision o el primer error, porque el await nunca se completa.
    /// <summary>Cerrar el dialogo abierto en Android. Lo usa el boton atras. Null en escritorio.</summary>
    private Action? _overlayClose;

    private Task<T?> AskAsync<T>(string title, int width, int height, Func<Action<T?>, Control> build)
    {
#if ANDROID
        var tcs = new TaskCompletionSource<T?>();
        var root = this.FindControl<Grid>("Root");
        if (root == null) { tcs.TrySetResult(default); return tcs.Task; }
        var layer = new Grid { Background = new SolidColorBrush(Color.FromArgb(170, 0, 0, 0)) };
        var card = new Border
        {
            Background = BgPanel, BorderBrush = Line, BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(10), Padding = new Thickness(14), Margin = new Thickness(10),
            HorizontalAlignment = HorizontalAlignment.Stretch, VerticalAlignment = VerticalAlignment.Center,
            MaxWidth = width
        };
        layer.Children.Add(card);
        root.Children.Add(layer);
        Action close = () =>
        {
            root.Children.Remove(layer);
            _overlayClose = null;
            // Cerrar sin decidir deja la tarea en default, que es lo que el motor
            // interpreta como "el usuario no eligio": vuelve a preguntar.
            tcs.TrySetResult(default);
        };
        card.Child = build(v => { root.Children.Remove(layer); _overlayClose = null; tcs.TrySetResult(v); });
        _overlayClose = close;
        return tcs.Task;
#else
        var win = new Window
        {
            Title = title, Width = width, Height = height, Background = BgPanel,
            WindowStartupLocation = WindowStartupLocation.CenterOwner, ShowInTaskbar = false
        };
        win.Content = build(v => win.Close(v));
        return win.ShowDialog<T?>(Host);
#endif
    }

    private async Task<CollisionDecision> PickCollisionAsync(CopyItem it, string target)
    {
        var res = await AskAsync<CollisionDecision>(L.Get("colTitle"), 640, 360, done =>
        {
            var sp = new StackPanel { Spacing = 8 };
            sp.Children.Add(MkLbl(L.Get("colTitle") + ":", 15));
            sp.Children.Add(MkLbl(it.SourcePath, 12).Tint(TextSoft));
            sp.Children.Add(MkLbl("→", 14).Tint(Gold));
            sp.Children.Add(MkLbl(target, 12).Tint(TextSoft));
            var chkAll = MkChk("", false, null);
            Bind(chkAll, "allfiles");
            sp.Children.Add(chkAll);
            var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6, Margin = new Thickness(0, 10, 0, 0) };
            foreach (var (a, k) in new (CopyAction, string)[] { (CopyAction.Overwrite, "overwrite"), (CopyAction.OverwriteIfDifferent, "overwriteDiff"), (CopyAction.Resume, "resume"), (CopyAction.Rename, "rename"), (CopyAction.Skip, "skip"), (CopyAction.CancelAll, "cancel") })
            {
                var b = Mk("…", () => done(new CollisionDecision(a, chkAll.IsChecked == true)));
                b.Content = L.Get(k);
                row.Children.Add(b);
            }
            sp.Children.Add(row);
            return new Border { BorderBrush = Line, BorderThickness = new Thickness(1), Child = sp };
        });
        return res ?? new CollisionDecision(CopyAction.Overwrite, false);
    }

    private async Task<ErrorDecision> PickErrorAsync(CopyItem it, string msg)
    {
        var res = await AskAsync<ErrorDecision>(L.Get("errTitle"), 640, 320, done =>
        {
            var sp = new StackPanel { Spacing = 8 };
            sp.Children.Add(MkLbl(it.SourcePath, 12).Tint(TextSoft));
            sp.Children.Add(MkLbl(msg, 12).Tint(Red));
            var chkAll = MkChk("", false, null);
            Bind(chkAll, "allfiles");
            sp.Children.Add(chkAll);
            var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6, Margin = new Thickness(0, 10, 0, 0) };
            foreach (var (a, k) in new (CopyAction, string)[] { (CopyAction.Retry, "retry"), (CopyAction.Skip, "skip"), (CopyAction.CancelAll, "cancel") })
            {
                var b = Mk("…", () => done(new ErrorDecision(a, chkAll.IsChecked == true)));
                b.Content = L.Get(k);
                row.Children.Add(b);
            }
            sp.Children.Add(row);
            return new Border { BorderBrush = Line, BorderThickness = new Thickness(1), Child = sp };
        });
        return res ?? new ErrorDecision(CopyAction.Skip, false);
    }

    // ----------------------------------------------------------- explorador
    private void PaneCopy(bool toLeft) => Transfer((toLeft ? _right : _left).SelectedPaths, (toLeft ? _left : _right).CurrentPath, false);
    private void PaneMove(bool toLeft) => Transfer((toLeft ? _right : _left).SelectedPaths, (toLeft ? _left : _right).CurrentPath, true);

    private void Transfer(List<string> paths, string dest, bool move)
    {
        if (paths.Count == 0 || string.IsNullOrEmpty(dest)) return;
        _tabs.SelectedIndex = 1;
        Enqueue(paths.ToArray(), dest, move);
    }

    private void ProcessForwarded(string args)
    {
        var parts = args.Split('\n').Where(x => x.Length > 0).ToArray();
        var paths = new List<string>();
        string? dest = null;
        bool after = false, move = false;
        for (int i = 0; i < parts.Length; i++)
        {
            if (parts[i] == "--copy") after = true;
            else if (parts[i] == "--move") move = true;
            else if (parts[i] == "--dest" && i + 1 < parts.Length) dest = parts[++i];
            else if (after && !parts[i].StartsWith("--")) paths.Add(parts[i]);
        }
        if (paths.Count > 0) Enqueue(paths.ToArray(), dest ?? "", move);
        if (!string.IsNullOrEmpty(dest)) _tbTo.Text = dest;
    }

    // ----------------------------------------------------------- bandeja
    private void BuildTray()
    {
#if !ANDROID
        try
        {
            if (_tray != null) { _tray.IsVisible = false; _tray.Dispose(); _tray = null; }
            if (!S.TrayIcon || _logoBmp == null) return;
            _tray = new TrayIcon { Icon = new WindowIcon(_logoBmp), ToolTipText = "QBasCopier y Transfer", IsVisible = true };
            _tray.Menu = new NativeMenu();
            var mOpen = new NativeMenuItem("QBasCopier y Transfer");
            mOpen.Click += (s, e) => { Show(); Activate(); };
            var mQuit = new NativeMenuItem(L.Get("quit"));
            mQuit.Click += (s, e) => DoQuit();
            _tray.Menu.Items.Add(mOpen);
            _tray.Menu.Items.Add(new NativeMenuItemSeparator());
            _tray.Menu.Items.Add(mQuit);
        }
        catch { }
#endif
    }

    internal bool ForceClose { get => _forceClose; set => _forceClose = value; }
    internal bool TrayVisible => _tray is { IsVisible: true };

    // ----------------------------------------------------------- util
    private static Button Mk(string text, Action? act)
    {
        var b = new Button { Content = L.Get(text), FontSize = 12, Margin = new Thickness(2) };
        if (act != null) b.Click += (s, e) => act();
        return b;
    }

    private static TextBlock MkLbl(string t, int size) => new() { Text = t, FontSize = size, Foreground = TextMain, VerticalAlignment = VerticalAlignment.Center };

    private static CheckBox MkChk(string content, bool init, Action<bool>? on)
    {
        var cb = new CheckBox { Content = content, IsChecked = init, VerticalAlignment = VerticalAlignment.Center };
        if (on != null) cb.Click += (s, e) => on(cb.IsChecked == true);
        return cb;
    }

    private void PickDirInto(TextBox tb) => _ = PickDirAsync(tb);
    private async Task PickDirAsync(TextBox tb)
    {
#if ANDROID
        // En Android no hay OpenFolderDialog (exige un owner Window y solo admite rutas
        // del sistema de archivos): se usa el selector de carpetas del sistema, que ademas
        // concede permiso de escritura real sobre la carpeta elegida.
        var ma = global::QBasCopier.Android.MainActivity.Current;
        if (ma == null) return;
        ma.PickTree(uri =>
        {
            if (string.IsNullOrEmpty(uri)) return;
            DispatchUi(() => tb.Text = global::QBasCopier.Android.DroidList.Root(uri));
        });
        await Task.CompletedTask;
#else
        var dlg = new OpenFolderDialog { Title = L.Get("browse") };
        var dir = await dlg.ShowAsync(Host);
        if (!string.IsNullOrEmpty(dir)) tb.Text = dir;
#endif
    }

    private void PickFiles() => _ = PickFilesAsync();
    private async Task PickFilesAsync()
    {
#if ANDROID
        var ma = global::QBasCopier.Android.MainActivity.Current;
        if (ma == null) return;
        ma.PickFiles(uris => DispatchUi(() =>
        {
            if (uris.Length > 0) AddFiles(uris);
        }));
        await Task.CompletedTask;
#else
        var dlg = new OpenFileDialog { Title = L.Get("addFiles"), AllowMultiple = true };
        var files = await dlg.ShowAsync(Host);
        if (files != null && files.Length > 0) AddFiles(files);
#endif
    }
}

public static class Ext
{
    public static T Also<T>(this T o, Action<T> a) { a(o); return o; }
    public static TextBlock Tint(this TextBlock t, IBrush b) { t.Foreground = b; return t; }
}

// Textos adicionales de la interfaz (20 idiomas)
public static class Ex
{
    public static readonly string[] Codes =
    {
        "es", "en", "fr", "pt", "it", "de", "nl", "ru", "uk", "pl",
        "tr", "cs", "ro", "hi", "ar", "zh", "ja", "ko", "id", "vi"
    };

    // 0 tabExplorer, 1 from, 2 to, 3 fold, 4 integrate, 5 unintegrate,
    // 6 verify, 7 activateOnStart, 8 diskWarn, 9 confirmAdd, 10 autoSaveLog, 11 warnSpace
    private static readonly string[][] Rows =
    {
        new[] { "Explorador", "Origen", "Destino", "Plegar", "Integrar en el sistema", "Quitar del sistema", "Verificar integridad (SHA-256)", "Activar ventana al iniciar", "Aviso de espacio mínimo (MB)", "Preguntar antes de añadir", "Guardar log de errores automático", "Poco espacio libre en el disco" },
        new[] { "Browser", "From", "To", "Collapse", "Integrate into system", "Remove from system", "Verify integrity (SHA-256)", "Activate window on startup", "Minimum free-space warning (MB)", "Ask before adding", "Save error log automatically", "Low free disk space" },
        new[] { "Explorateur", "Source", "Destination", "Replier", "Intégrer au système", "Retirer du système", "Vérifier l'intégrité (SHA-256)", "Activer la fenêtre au démarrage", "Avertissement d'espace libre (Mo)", "Demander avant d'ajouter", "Enregistrer le journal des erreurs", "Espace disque libre faible" },
        new[] { "Explorador", "Origem", "Destino", "Recolher", "Integrar ao sistema", "Remover do sistema", "Verificar integridade (SHA-256)", "Ativar janela ao iniciar", "Aviso de espaço mínimo (MB)", "Perguntar antes de adicionar", "Salvar log de erros automaticamente", "Pouco espaço livre no disco" },
        new[] { "Browser", "Origine", "Destinazione", "Comprimi", "Integra nel sistema", "Rimuovi dal sistema", "Verifica integrità (SHA-256)", "Attiva finestra all'avvio", "Avviso spazio minimo (MB)", "Chiedi prima di aggiungere", "Salva log errori automaticamente", "Spazio su disco basso" },
        new[] { "Explorer", "Quelle", "Ziel", "Einklappen", "In System integrieren", "Aus System entfernen", "Integrität prüfen (SHA-256)", "Fenster beim Start aktivieren", "Mindestplatz-Warnung (MB)", "Vor dem Hinzufügen fragen", "Fehlerlog automatisch speichern", "Wenig freier Speicherplatz" },
        new[] { "Verkenner", "Bron", "Doel", "Inklappen", "Integreren in systeem", "Uit systeem verwijderen", "Integriteit controleren (SHA-256)", "Venster activeren bij start", "Waarschuwing minimale ruimte (MB)", "Vragen voor toevoegen", "Foutenlog automatisch opslaan", "Weinig vrije schijfruimte" },
        new[] { "Проводник", "Источник", "Назначение", "Свернуть", "Интегрировать в систему", "Убрать из системы", "Проверять целостность (SHA-256)", "Показывать окно при запуске", "Предупреждение о свободном месте (МБ)", "Спрашивать перед добавлением", "Автосохранение журнала ошибок", "Мало свободного места на диске" },
        new[] { "Провідник", "Джерело", "Призначення", "Згорнути", "Інтегрувати в систему", "Прибрати з системи", "Перевіряти цілісність (SHA-256)", "Показувати вікно при запуску", "Попередження про вільне місце (МБ)", "Питати перед додаванням", "Автозбереження журналу помилок", "Мало вільного місця на диску" },
        new[] { "Eksplorator", "Źródło", "Cel", "Zwiń", "Zintegruj z systemem", "Usuń z systemu", "Sprawdzaj integralność (SHA-256)", "Aktywuj okno przy starcie", "Ostrzeżenie o wolnej przestrzeni (MB)", "Pytaj przed dodaniem", "Automatycznie zapisuj log błędów", "Mało wolnego miejsca na dysku" },
        new[] { "Gezgin", "Kaynak", "Hedef", "Daralt", "Sisteme entegre et", "Sistemden kaldır", "Bütünlüğü doğrula (SHA-256)", "Başlangıçta pencereyi etkinleştir", "Minimum boş alan uyarısı (MB)", "Eklerken sor", "Hata günlüğünü otomatik kaydet", "Diski az yer kaldı" },
        new[] { "Průzkumník", "Zdroj", "Cíl", "Sbalit", "Integrovat do systému", "Odebrat ze systému", "Ověřit integritu (SHA-256)", "Aktivovat okno při startu", "Upozornění na volné místo (MB)", "Zeptat se před přidáním", "Uložit log chyb automaticky", "Málo volného místa na disku" },
        new[] { "Explorator", "Sursă", "Destinație", "Restrânge", "Integrează în sistem", "Elimină din sistem", "Verifică integritatea (SHA-256)", "Activează fereastra la pornire", "Avertisment spațiu minim (MB)", "Întreabă înainte de adăugare", "Salvează automat logul de erori", "Puțin spațiu liber pe disc" },
        new[] { "ब्राउज़र", "स्रोत", "गंतव्य", "संक्षिप्त करें", "सिस्टम में एकीकृत करें", "सिस्टम से हटाएँ", "अखंडता सत्यापित करें (SHA-256)", "प्रारंभ पर विंडो सक्रिय करें", "न्यूनतम स्थान चेतावनी (MB)", "जोड़ने से पहले पूछें", "त्रुटि लॉग स्वतः सहेजें", "डिस्क में कम जगह" },
        new[] { "المتصفح", "المصدر", "الوجهة", "طي", "دمج في النظام", "إزالة من النظام", "التحقق من السلامة (SHA-256)", "تنشيط النافذة عند بدء التشغيل", "تحذير المساحة الحرة (MB)", "اسأل قبل الإضافة", "حفظ سجل الأخطاء تلقائيًا", "مساحة قرص منخفضة" },
        new[] { "浏览器", "源", "目标", "折叠", "集成到系统", "从系统移除", "验证完整性 (SHA-256)", "启动时激活窗口", "最小剩余空间警告 (MB)", "添加前询问", "自动保存错误日志", "磁盘可用空间不足" },
        new[] { "エクスプローラー", "送信元", "宛先", "折りたたむ", "システムに統合", "システムから削除", "整合性を検証 (SHA-256)", "起動時にウィンドウを表示", "空き容量警告 (MB)", "追加前に確認", "エラーログを自動保存", "ディスク空き容量が少ない" },
        new[] { "탐색기", "소스", "대상", "접기", "시스템에 통합", "시스템에서 제거", "무결성 검증 (SHA-256)", "시작 시 창 활성화", "최소 여유 공간 경고 (MB)", "추가하기 전에 물어보기", "오류 로그 자동 저장", "디스크 여유 공간 부족" },
        new[] { "Penjelajah", "Sumber", "Tujuan", "Ciutkan", "Integrasikan ke sistem", "Hapus dari sistem", "Verifikasi integritas (SHA-256)", "Aktifkan jendela saat mulai", "Peringatan ruang minimum (MB)", "Tanya sebelum menambah", "Simpan log kesalahan otomatis", "Ruang disk rendah" },
        new[] { "Trình duyệt", "Nguồn", "Đích", "Thu gọn", "Tích hợp vào hệ thống", "Xóa khỏi hệ thống", "Xác minh tính toàn vẹn (SHA-256)", "Kích hoạt cửa sổ khi khởi động", "Cảnh báo dung lượng tối thiểu (MB)", "Hỏi trước khi thêm", "Tự động lưu nhật ký lỗi", "Ít dung lượng ổ đĩa trống" },
    };

    public static int IndexOf(string code)
    {
        var i = Array.IndexOf(Codes, code.ToLowerInvariant());
        return i < 0 ? 0 : i;
    }

    public static string[] LangNames() => Rows.Select(r => r[0]).ToArray();

    public static string Get(string key)
    {
        var idx = key switch
        {
            "tabExplorer" => 0,
            "from" => 1,
            "to" => 2,
            "fold" => 3,
            "integrate" => 4,
            "unintegrate" => 5,
            "verify" => 6,
            "activateOnStart" => 7,
            "diskWarn" => 8,
            "confirmAdd" => 9,
            "autoSaveLog" => 10,
            "warnSpace" => 11,
            _ => -1
        };
        if (idx < 0) return key;
        var r = Rows[Math.Clamp(L.Current, 0, Rows.Length - 1)];
        return idx < r.Length ? r[idx] : key;
    }
}