#if !ANDROID
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;

namespace QBasCopier;

/// <summary>
/// El buscador para mandar archivos desde el PC.
///
/// En el movil esto no hace falta: cada equipo tiene ya su selector de archivos, con
/// sus fotos, su musica y sus filtros, y es mejor usar ese. En el PC, en cambio, hay
/// que ver las carpetas y poder elegir cosas de varias a la vez, asi que esta ventana
/// lo hace: se navega, se marca, y tambien se pueden soltar archivos encima.
///
/// Acepta lo que sea: archivos sueltos, carpetas enteras y las dos cosas mezcladas.
/// Las carpetas viajan con su contenido y su estructura, sin limites de tamano.
/// </summary>
public sealed class SendFilesWindow : Window
{
    private readonly MainWindow _owner;
    private readonly string _baseUrl;
    private readonly ListBox _list;
    private readonly TextBlock _ruta;
    private readonly TextBlock _resumen;
    private readonly TextBlock _estado;
    private readonly Button _enviar;
    private readonly CheckBox _ocultos;

    private readonly HashSet<string> _marcados = new(StringComparer.OrdinalIgnoreCase);
    private string _dir = "";

    private static readonly IBrush Panel = new SolidColorBrush(Color.Parse("#060D24"));
    private static readonly IBrush Linea = new SolidColorBrush(Color.Parse("#1F3B8C"));
    private static readonly IBrush Tinta = new SolidColorBrush(Color.Parse("#F6F8FF"));
    private static readonly IBrush TintaSuave = new SolidColorBrush(Color.Parse("#9FB3E8"));
    private static readonly IBrush Oro = new SolidColorBrush(Color.Parse("#FBBF24"));

    /// <summary>Ultima carpeta abierta, para no empezar siempre en el mismo sitio.</summary>
    private static string UltimaCarpeta = "";

    private SendFilesWindow(MainWindow owner, string baseUrl)
    {
        _owner = owner;
        _baseUrl = baseUrl;

        Title = L.Get("addFiles") + " · " + L.Get("trSend");
        Width = 860; Height = 620;
        MinWidth = 620; MinHeight = 420;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = Panel;
        ShowInTaskbar = false;

        _ruta = new TextBlock { FontSize = 13, Foreground = Oro, TextTrimming = TextTrimming.CharacterEllipsis, Margin = new Thickness(0, 0, 0, 6) };
        _resumen = new TextBlock { FontSize = 12.5, Foreground = TintaSuave };
        _estado = new TextBlock { FontSize = 12.5, Foreground = TintaSuave, TextWrapping = TextWrapping.Wrap, MaxLines = 2 };
        _enviar = new Button { Content = L.Get("trSend"), Classes = { "primary" }, MinHeight = 38, Padding = new Thickness(18, 0, 18, 0), IsEnabled = false };
        _enviar.Click += async (_, _) => await Enviar();

        _ocultos = new CheckBox { Content = ".", IsChecked = false, MinWidth = 0 };
        _ocultos.IsVisible = false;   // reservado: los ocultos no se muestran

        _list = new ListBox
        {
            Background = new SolidColorBrush(Color.Parse("#08122F")),
            BorderBrush = Linea,
            BorderThickness = new Thickness(1),
            ItemTemplate = null
        };
        _list.DoubleTapped += DobleToque;
        _list.SelectionMode = SelectionMode.Multiple;

        // Botonera
        var barra = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6, Margin = new Thickness(0, 0, 0, 8) };
        barra.Children.Add(Boton(L.Get("up"), Subir));
        barra.Children.Add(Boton(L.Get("refresh"), Refrescar));
        barra.Children.Add(Boton(L.Get("addFiles"), ElegirConDialogo));
        barra.Children.Add(Boton(L.Get("trSendFolder"), ElegirCarpetaConDialogo));

        var sp = new StackPanel { Spacing = 8, Margin = new Thickness(14) };
        sp.Children.Add(new TextBlock { Text = L.Get("trJoinTitle"), FontSize = 15, Foreground = Tinta, FontWeight = FontWeight.SemiBold });
        sp.Children.Add(_ruta);
        sp.Children.Add(barra);
        sp.Children.Add(_list);

        var pie = new Grid { ColumnDefinitions = { new ColumnDefinition(GridLength.Auto), new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) }, Margin = new Thickness(0, 8, 0, 0) };
        var pista = new TextBlock { Text = L.Get("trDropHint"), Foreground = TintaSuave, FontSize = 12, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 10, 0) };
        pista.SetValue(Grid.ColumnProperty, 0);
        pie.Children.Add(pista);
        _estado.SetValue(Grid.ColumnProperty, 1);
        var der = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        var cancelar = new Button { Content = L.Get("cancel"), MinHeight = 38, Padding = new Thickness(14, 0, 14, 0) };
        cancelar.Click += (_, _) => Close(null);
        der.Children.Add(cancelar);
        der.Children.Add(_enviar);
        der.SetValue(Grid.ColumnProperty, 2);
        pie.Children.Add(der);
        sp.Children.Add(pie);
        sp.Children.Add(_resumen);

        Content = new Border { Padding = new Thickness(0), Child = sp };

        // Soltar archivos o carpetas desde el explorador del escritorio.
        var root = this;
        DragDrop.SetAllowDrop(root, true);
        root.AddHandler(DragDrop.DropEvent, (_, e) =>
        {
            var rutas = RutasDe(e.Data);
            if (rutas.Count > 0) Anadidas(rutas);
            e.Handled = true;
        });
        root.AddHandler(DragDrop.DragOverEvent, (_, e) =>
        {
            e.Handled = RutasDe(e.Data).Count > 0;
        });

        _dir = UltimaCarpeta.Length > 0 && Directory.Exists(UltimaCarpeta)
            ? UltimaCarpeta
            : Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        if (string.IsNullOrEmpty(_dir) || !Directory.Exists(_dir)) _dir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (string.IsNullOrEmpty(_dir) || !Directory.Exists(_dir)) _dir = Path.GetTempPath();
        IrA(_dir);
    }

    /// <summary>
    /// Abre el buscador y devuelve lo elegido. ShowDialog devuelve una Task, asi que
    /// esto se espera de verdad: antes se devolvia null sin esperar y no se mandaba
    /// nunca nada.
    /// </summary>
    public static Task<string[]?> Open(MainWindow owner, string baseUrl) =>
        new SendFilesWindow(owner, baseUrl).ShowDialog<string[]?>(MainWindow.Host);

    // ------------------------------------------------------------------ interfaz

    private static Button Boton(string txt, Action a)
    {
        var b = new Button { Content = txt, MinHeight = 32, Padding = new Thickness(12, 0, 12, 0) };
        b.Click += (_, _) => a();
        return b;
    }

    private void IrA(string dir)
    {
        if (!Directory.Exists(dir)) return;
        _dir = dir;
        UltimaCarpeta = dir;
        _ruta.Text = dir;

        var filas = new List<(string Nombre, string Ruta, bool EsDir, long Tam, DateTime Fecha)>();
        try
        {
            foreach (var d in new DirectoryInfo(dir).GetDirectories())
            {
                try { filas.Add((d.Name, d.FullName, true, 0, d.LastWriteTime)); } catch { }
            }
            foreach (var f in new DirectoryInfo(dir).GetFiles())
            {
                try
                {
                    var oculto = (f.Attributes & FileAttributes.Hidden) != 0;
                    if (oculto && _ocultos.IsChecked != true) continue;
                    filas.Add((f.Name, f.FullName, false, f.Length, f.LastWriteTime));
                }
                catch { }
            }
        }
        catch (Exception e)
        {
            CrashLog.Info("listar carpeta: " + e.Message);
            _estado.Text = e.Message;
        }

        // Carpetas primero y luego archivos, cada grupo por nombre.
        var orden = filas
            .OrderByDescending(f => f.EsDir)
            .ThenBy(f => f.Nombre, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

        _list.ItemsSource = orden.Select(f => Fila(f)).ToList();
        ActualizaResumen();
    }

    private Control Fila((string Nombre, string Ruta, bool EsDir, long Tam, DateTime Fecha) f)
    {
        var sel = _marcados.Contains(f.Ruta);
        var marca = new TextBlock
        {
            Text = sel ? "✔" : "",
            Foreground = Oro,
            FontSize = 15,
            Width = 22,
            VerticalAlignment = VerticalAlignment.Center
        };
        var nombre = new TextBlock { Text = f.Nombre, Foreground = Tinta, FontSize = 13.5, TextTrimming = TextTrimming.CharacterEllipsis };
        var det = new TextBlock
        {
            Text = f.EsDir ? "—" : Fmt.Human(f.Tam),
            Foreground = TintaSuave,
            FontSize = 12,
            Width = 110,
            TextAlignment = TextAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center
        };
        var fecha = new TextBlock
        {
            Text = f.Fecha.ToString("yyyy-MM-dd HH:mm"),
            Foreground = TintaSuave,
            FontSize = 12,
            Width = 130,
            VerticalAlignment = VerticalAlignment.Center
        };

        var grid = new Grid { ColumnDefinitions = { new ColumnDefinition(GridLength.Auto), new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto), new ColumnDefinition(GridLength.Auto) } };
        grid.Children.Add(marca);
        nombre.SetValue(Grid.ColumnProperty, 1);
        det.SetValue(Grid.ColumnProperty, 2);
        fecha.SetValue(Grid.ColumnProperty, 3);
        grid.Children.Add(nombre);
        grid.Children.Add(det);
        grid.Children.Add(fecha);

        var row = new Border
        {
            Padding = new Thickness(6, 5),
            CornerRadius = new CornerRadius(6),
            Background = sel ? new SolidColorBrush(Color.Parse("#132350")) : Brushes.Transparent,
            Tag = f.Ruta
        };
        row.Child = grid;

        // Un clic marca o quita; con Ctrl se pueden marcar varios sin perder el resto.
        row.PointerPressed += (_, e) =>
        {
            var yaEstaba = _marcados.Contains(f.Ruta);
            var quitar = e.KeyModifiers.HasFlag(KeyModifiers.Control) && yaEstaba;
            if (quitar) _marcados.Remove(f.Ruta);
            else if (yaEstaba) _marcados.Remove(f.Ruta);
            else _marcados.Add(f.Ruta);
            ActualizaResumen();
            IrA(_dir);
        };
        return row;
    }

    private void ActualizaResumen()
    {
        var ficheros = 0; var carpetas = 0; long peso = 0;
        foreach (var r in _marcados)
        {
            try
            {
                if (Directory.Exists(r)) { carpetas++; peso += PesoDe(r); }
                else { ficheros++; peso += new FileInfo(r).Length; }
            }
            catch { }
        }
        _resumen.Text = L.F("trSelDirs", ficheros, carpetas)
            + (peso > 0 ? "  ·  " + Fmt.Human(peso) : "");
        _enviar.IsEnabled = _marcados.Count > 0;
    }

    private static long PesoDe(string carpeta)
    {
        long total = 0;
        try
        {
            foreach (var f in new DirectoryInfo(carpeta).EnumerateFiles("*", SearchOption.AllDirectories))
            {
                try { total += f.Length; } catch { }
            }
        }
        catch { }
        return total;
    }

    // ------------------------------------------------------------------ acciones

    private void Subir()
    {
        try
        {
            var padre = Directory.GetParent(_dir);
            if (padre != null) IrA(padre.FullName);
        }
        catch { }
    }

    private void Refrescar() => IrA(_dir);

    private void DobleToque(object? sender, TappedEventArgs e)
    {
        if (sender is not ListBox lb) return;
        if (lb.SelectedItem is not Border b) return;
        if (b.Tag is not string ruta) return;
        try
        {
            if (Directory.Exists(ruta)) IrA(ruta);
            else
            {
                _marcados.Add(ruta);
                ActualizaResumen();
                IrA(_dir);
            }
        }
        catch { }
    }

    private void Anadidas(IEnumerable<string> rutas)
    {
        foreach (var r in rutas)
        {
            try { if (File.Exists(r) || Directory.Exists(r)) _marcados.Add(r); } catch { }
        }
        ActualizaResumen();
    }

    /// <summary>Del volcado de un arrastre solo interesan las rutas del disco.</summary>
    private static List<string> RutasDe(IDataObject datos)
    {
        var res = new List<string>();
        try
        {
            if (!datos.Contains(DataFormats.Files)) return res;
            var lista = datos.Get(DataFormats.Files) as IEnumerable<string>;
            if (lista != null)
            {
                foreach (var s in lista)
                {
                    if (string.IsNullOrEmpty(s)) continue;
                    if (s.StartsWith("file://", StringComparison.OrdinalIgnoreCase))
                    {
                        try { res.Add(Uri.UnescapeDataString(s[7..])); } catch { }
                    }
                    else res.Add(s);
                }
            }
        }
        catch (Exception e) { CrashLog.Info("arrastrar archivos: " + e.Message); }
        return res;
    }

    private async void ElegirConDialogo()
    {
        try
        {
            var dlg = new OpenFileDialog { AllowMultiple = true, Title = L.Get("addFiles") };
            var files = await dlg.ShowAsync(this);
            if (files != null && files.Length > 0) Anadidas(files);
        }
        catch (Exception e) { CrashLog.Info("abrir archivos: " + e.Message); }
    }

    private async void ElegirCarpetaConDialogo()
    {
        try
        {
            var dlg = new OpenFolderDialog { Title = L.Get("trSendFolder") };
            var dir = await dlg.ShowAsync(this);
            if (!string.IsNullOrEmpty(dir)) Anadidas(new[] { dir });
        }
        catch (Exception e) { CrashLog.Info("abrir carpeta: " + e.Message); }
    }

    private async Task Enviar()
    {
        var lista = _marcados.ToArray();
        if (lista.Length == 0) return;
        _enviar.IsEnabled = false;
        _estado.Text = L.F("trSelDirs", lista.Length, 0);
        try
        {
            // La ventana se cierra sola cuando ya no queda nada que enviar: se manda
            // en segundo plano y el progreso se ve en la ventana principal.
            Close(lista);
        }
        catch (Exception e)
        {
            CrashLog.Info("enviar: " + e.Message);
            _estado.Text = e.Message;
        }
        await Task.CompletedTask;
    }
}
#endif
