#if !ANDROID
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Threading;

namespace QBasCopier;

/// <summary>
/// La ventanita de copia: sale sola cuando empieza un Copiar o un Mover, enseña como
/// va lo de ahora y se va sola cuando se acaba.
///
/// Va por detras de la ventana principal a proposito. La ventana principal es la que
/// se usa para preparar el trabajo (elegir origen, destino, filtros); una vez que ya
/// esta copiando, lo que interesa es un vistazo rapido a como va sin tener que
/// cambiar de ventana. Por eso aqui solo hay barras y tres o cuatro botones.
///
/// Sirve igual para las dos formas de entrar:
///  - desde la propia app, con la ventana principal abierta;
///  - desde el menu del Explorador o desde el menu de otro gestor de archivos, con
///    --copy / --move. Ahi no hay ventana principal: esta ventanita ES la app.
///
/// El boton "Mas" despliega la lista completa. Con la lista plegada solo se ven los
/// archivos que se estan moviendo ahora mismo, que es lo unico que cambia deprisa;
/// con la lista abierta se puede quitar de la cola lo que ya no se quiere copiar.
/// </summary>
public sealed class CopyPopup : Window
{
    private static readonly IBrush Panel = new SolidColorBrush(Color.Parse("#060D24"));
    private static readonly IBrush Fondo = new SolidColorBrush(Color.Parse("#0B1533"));
    private static readonly IBrush Linea = new SolidColorBrush(Color.Parse("#1F3B8C"));
    private static readonly IBrush Tinta = new SolidColorBrush(Color.Parse("#F6F8FF"));
    private static readonly IBrush Suave = new SolidColorBrush(Color.Parse("#9FB3E8"));
    private static readonly IBrush Oro = new SolidColorBrush(Color.Parse("#FBBF24"));
    private static readonly IBrush Verde = new SolidColorBrush(Color.Parse("#34D399"));
    private static readonly IBrush Rojo = new SolidColorBrush(Color.Parse("#F0A9B2"));

    private readonly CopyEngine _engine;
    private readonly string _destino;
    private readonly bool _suelto;          // lanzada desde fuera: no hay ventana principal
    private readonly Action _alTerminar;

    private readonly TextBlock _cab = Txt(15, true, Tinta);
    private readonly TextBlock _sub = Txt(11, false, Suave);
    private readonly TextBlock _pct = Txt(12, true, Tinta);
    private readonly TextBlock _vel = Txt(12, false, Suave);
    private readonly TextBlock _resta = Txt(12, false, Suave);
    private readonly TextBlock _actual = Txt(11, false, Suave);
    private readonly TextBlock _fin = Txt(12, false, Verde);
    private readonly ProgressBar _barra = new() { Minimum = 0, Maximum = 100, Height = 14 };
    private readonly ProgressBar _barraVel = new() { Minimum = 0, Maximum = 1, Height = 4 };
    private readonly StackPanel _filas = new() { Spacing = 6 };
    private readonly ScrollViewer _scroll;
    private readonly Button _bPausa, _bCancelar, _bMas, _bDestino, _bReintentar;
    private readonly DispatcherTimer _t, _tCierre;

    private readonly Dictionary<CopyItem, (ProgressBar Bar, TextBlock Info)> _vistas = new();
    private List<CopyItem> _visibles = new();
    private long _pico;            // velocidad mas alta vista, para la barrita de ritmo
    private long _rateBase;        // bytes de la foto anterior, para medir la velocidad
    private long rateActual;       // velocidad del ultimo calculo
    private long _antes;
    private double _segundos;
    private bool _detalle;
    private bool _terminado;

    public CopyPopup(CopyEngine engine, bool moviendo, string destino, bool suelto,
                     Action abrirDestino, Action reintentar, Action alTerminar)
    {
        _engine = engine;
        _destino = destino;
        _suelto = suelto;
        _alTerminar = alTerminar;

        Title = moviendo ? L.Get("move") : L.Get("copy");
        Width = 520; Height = 330;
        MinWidth = 380; MinHeight = 220;
        Background = Panel;
        ShowInTaskbar = false;
        CanResize = true;
        Topmost = true;
        // No roba el foco: se esta copiando por debajo de lo que el usuario estaba
        // haciendo y no tiene por que enterarse de que aparecio una ventana.
        ShowActivated = false;
        WindowStartupLocation = WindowStartupLocation.Manual;

        _cab.Text = moviendo ? L.Get("move") : L.Get("copy");
        _sub.Text = destino;
        _sub.TextTrimming = TextTrimming.CharacterEllipsis;

        _scroll = new ScrollViewer
        {
            Content = _filas,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            MaxHeight = 190
        };

        _bPausa = Btn(L.Get("pause"), false);
        _bPausa.Click += (_, _) =>
        {
            if (_engine.PauseRequested) _engine.Resume(); else _engine.Pause();
            _bPausa.Content = _engine.PauseRequested ? L.Get("resume") : L.Get("pause");
        };
        _bCancelar = Btn(L.Get("cancel"), false, "danger");
        _bCancelar.Click += (_, _) => _engine.Cancel();
        _bMas = Btn(L.Get("popMore"), false);
        _bMas.Click += (_, _) => { _detalle = !_detalle; _bMas.Content = L.Get("popMore") + (_detalle ? " ▴" : " ▾"); Recarga(); AjustaAlto(); };
        _bDestino = Btn(L.Get("openFolder"), false);
        _bDestino.Click += (_, _) => abrirDestino();
        _bReintentar = Btn(L.Get("popRetry"), false);
        _bReintentar.IsVisible = false;
        _bReintentar.Click += (_, _) => reintentar();
        var cerrar = Btn("✕", false);
        cerrar.Click += (_, _) => Close();

        var botonera = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
        botonera.Children.Add(_bPausa);
        botonera.Children.Add(_bCancelar);
        botonera.Children.Add(_bMas);
        botonera.Children.Add(new Border { Width = 6 });
        botonera.Children.Add(_bDestino);
        botonera.Children.Add(_bReintentar);
        botonera.Children.Add(cerrar);

        var med = new Grid { ColumnDefinitions = { new(GridLength.Star), new(GridLength.Auto), new(GridLength.Auto) } };
        Grid.SetColumn(_pct, 0); med.Children.Add(_pct);
        _vel.HorizontalAlignment = HorizontalAlignment.Right;
        Grid.SetColumn(_vel, 1); med.Children.Add(_vel);
        _resta.HorizontalAlignment = HorizontalAlignment.Right;
        Grid.SetColumn(_resta, 2); med.Children.Add(_resta);

        var sp = new StackPanel { Spacing = 7, Margin = new Thickness(14, 12, 14, 12) };
        sp.Children.Add(_cab);
        sp.Children.Add(_sub);
        sp.Children.Add(_barra);
        sp.Children.Add(_barraVel);   // el ritmo: cuanto va de lo mas rapido que ha llegado
        sp.Children.Add(med);
        sp.Children.Add(_actual);
        sp.Children.Add(_scroll);
        sp.Children.Add(_fin);
        sp.Children.Add(botonera);

        Content = new Border { BorderBrush = Linea, BorderThickness = new Thickness(1), Child = sp };

        _t = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(Math.Clamp(MainWindow.S.WindowUpdateMs, 100, 500)) };
        _t.Tick += (_, _) => Actualiza();
        _t.Start();

        // Al terminar se va sola, pero no de golpe: primero deja ver que se acabo.
        _tCierre = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2.2) };
        _tCierre.Tick += (_, _) => { _tCierre.Stop(); Close(); };

        Coloca();
    }

    /// <summary>Esquina inferior derecha, como una notificacion que no tapa el trabajo.</summary>
    private void Coloca()
    {
        try
        {
            var wa = Screens.All.FirstOrDefault()?.WorkingArea;
            if (wa == null || wa.Value.Width <= 0) return;
            Position = new PixelPoint(wa.Value.Right - (int)Width - 20, wa.Value.Bottom - (int)Height - 20);
        }
        catch (Exception e) { CrashLog.Save("ERROR al colocar la ventanita de copia: " + e.Message); }
    }

    private void AjustaAlto()
    {
        // Plegada es una ventanita; desplegada tiene que caber la lista.
        _scroll.MaxHeight = _detalle ? 380 : 190;
        Height = _detalle ? 560 : 330;
        Coloca();
    }

    // ------------------------------------------------------------------ refresco
    private void Actualiza()
    {
        if (_terminado) return;
        var total = _engine.TotalBytes;
        var hecho = _engine.DoneBytes;
        _barra.Value = total > 0 ? Math.Clamp(hecho * 100.0 / total, 0, 100) : 0;
        _pct.Text = _barra.Value.ToString("0.#", CultureInfo.CurrentCulture) + "%";

        var ahora = Environment.TickCount64;
        var dt = (ahora - _antes) / 1000.0;
        _antes = ahora;
        if (dt > 0.15 && !_engine.PauseRequested)
        {
            _segundos += dt;
            long rate = (long)((hecho - _rateBase) / dt);
            _rateBase = hecho;
            rateActual = rate;
            if (rate > _pico) _pico = rate;
            _vel.Text = L.Get("speed") + ": " + Fmt.Rate(rate);
            _barraVel.Maximum = Math.Max(_pico, 1);
            _barraVel.Value = Math.Clamp(rate, 0, Math.Max(_pico, 1));
            _barraVel.Foreground = rate > 0 ? Verde : Suave;
        }

        var falta = total - hecho;
        _resta.Text = falta > 0 && rateActual > 0
            ? L.Get("remaining") + ": " + Fmt.Time(falta / (double)rateActual)
            : L.Get("elapsed") + ": " + Fmt.Time(_segundos);

        _cab.Text = (_engine.Move ? L.Get("move") : L.Get("copy"))
            + (_engine.PauseRequested ? " · " + L.Get("pausedTag") : "");

        var copiando = Cola().FirstOrDefault(i => i.State == ItemState.Copying);
        _actual.Text = copiando == null ? "" : L.Get("currentFile") + ": " + copiando.Name;

        Recarga();
    }

    /// <summary>
    /// Que filas se ven. Plegada, solo lo que esta en marcha; desplegada, la cola
    /// entera. Solo se reconstruye la lista cuando el grupo de archivos cambia: los
    /// valores de las barras se actualizan en sitio, sin tocar la lista.
    /// </summary>
    private void Recarga()
    {
        var ahora = Cola();
        var nuevas = _detalle
            ? ahora.ToList()
            : ahora.Where(i => i.State is ItemState.Copying or ItemState.Conflict).Take(12).ToList();

        if (!Mismas(nuevas, _visibles))
        {
            _visibles = nuevas;
            _filas.Children.Clear();
            _vistas.Clear();
            foreach (var it in nuevas)
            {
                var bar = new ProgressBar { Minimum = 0, Maximum = 100, Height = 6 };
                var info = Txt(10, false, Suave);
                var nombre = Txt(11, false, Tinta);
                nombre.Text = it.Name;
                nombre.TextTrimming = TextTrimming.CharacterEllipsis;
                var cab = new Grid { ColumnDefinitions = { new(GridLength.Star), new(GridLength.Auto) } };
                Grid.SetColumn(nombre, 0); cab.Children.Add(nombre);
                info.HorizontalAlignment = HorizontalAlignment.Right;
                Grid.SetColumn(info, 1); cab.Children.Add(info);
                var col = new StackPanel { Spacing = 2, Children = { cab, bar } };
                _filas.Children.Add(col);
                _vistas[it] = (bar, info);
            }
        }

        foreach (var it in _visibles)
        {
            if (!_vistas.TryGetValue(it, out var v)) continue;
            v.Bar.Value = Math.Clamp(it.Percent, 0, 100);
            v.Bar.Foreground = it.State switch
            {
                ItemState.Done => Verde,
                ItemState.Error => Rojo,
                ItemState.Skipped => Suave,
                _ => Oro
            };
            // El texto del estado lo pone el motor y ya viene en el idioma del usuario;
            // mientras el archivo se esta copiando, lo util es cuanto pesa.
            v.Info.Text = it.State == ItemState.Copying || string.IsNullOrEmpty(it.StateText)
                ? it.SizeText
                : it.StateText;
        }
    }

    private List<CopyItem> Cola() => _engine.Snapshot();

    private static bool Mismas(List<CopyItem> a, List<CopyItem> b)
    {
        if (a.Count != b.Count) return false;
        for (int i = 0; i < a.Count; i++) if (!ReferenceEquals(a[i], b[i])) return false;
        return true;
    }

    /// <summary>Se llama cuando el motor ya termino. La ventanita se despide sola.</summary>
    public void Terminada(int errores)
    {
        if (_terminado) return;
        _terminado = true;
        _t.Stop();
        Recarga();

        _cab.Text = L.Get("done");
        _sub.Text = _destino;
        _actual.Text = "";
        _barra.Value = 100;
        _barra.Foreground = errores > 0 ? Oro : Verde;
        _pct.Text = "100%";
        _vel.Text = "";
        _resta.Text = L.Get("elapsed") + ": " + Fmt.Time(_segundos);
        _fin.Foreground = errores > 0 ? Rojo : Verde;
        _fin.Text = errores > 0 ? "!  " + L.Get("errTitle") + "  (" + errores + ")" : "";
        _bPausa.IsVisible = false;
        _bCancelar.IsVisible = false;
        _bMas.IsVisible = false;
        _bDestino.IsVisible = true;
        _bReintentar.IsVisible = errores > 0;

        // Si hubo errores y esta ventanita es lo unico que hay en pantalla, no se
        // cierra sola: seria esconder el fallo justo cuando hay que verlo. En la app
        // abierta los errores ya estan en su pestana, asi que aqui si se va.
        if (errores > 0 && _suelto) return;
        _tCierre.Start();
    }

    /// <summary>
    /// Vuelve al estado de "copiando" cuando se le da a Reintentar: se rearrancan los
    /// contadores y se vuelve a enseñar la botonera de siempre.
    /// </summary>
    public void Reanudando()
    {
        _terminado = false;
        _antes = Environment.TickCount64;
        _rateBase = 0;
        rateActual = 0;
        _pico = 0;
        _segundos = 0;
        _tCierre.Stop();
        _fin.Text = "";
        _bPausa.IsVisible = _bCancelar.IsVisible = _bMas.IsVisible = true;
        _bReintentar.IsVisible = false;
        _t.Start();
    }

    protected override void OnClosed(EventArgs e)
    {
        _t.Stop();
        _tCierre.Stop();
        base.OnClosed(e);
        try { _alTerminar(); } catch { }
    }

    // ------------------------------------------------------------------ ayuda
    private static TextBlock Txt(double size, bool bold, IBrush color) => new()
    {
        FontSize = size,
        FontWeight = bold ? FontWeight.SemiBold : FontWeight.Normal,
        Foreground = color,
        VerticalAlignment = VerticalAlignment.Center
    };

    private static Button Btn(string text, bool primario, string? estilo = null)
    {
        var b = new Button { Content = text, MinHeight = 32, Padding = new Thickness(12, 0, 12, 0), FontSize = 12 };
        if (estilo == "danger") { b.Classes.Add("danger"); }
        else if (primario) { b.Classes.Add("primary"); }
        return b;
    }
}
#endif
