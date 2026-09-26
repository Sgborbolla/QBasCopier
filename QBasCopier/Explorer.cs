using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using System.Globalization;
using System.IO;

namespace QBasCopier;

/// <summary>Un archivo o carpeta que se esta viendo en el panel.</summary>
public sealed class Ent
{
    public string Path = "";     // ruta local, o content:// en Android
    public string Name = "";
    public bool IsDir;
    public long Size;

    public Ent(string p, string n, bool d, long s) { Path = p; Name = n; IsDir = d; Size = s; }
}

/// <summary>
/// El panel unico de archivos.
///
/// Antes habia un explorador de dos paneles al estilo Total Commander. En un
/// movil se veia regado y apretado, asi que se sustituyo por una sola lista:
/// se toca una carpeta para entrar, se tocan los archivos para marcarlos, y
/// abajo estan las tres cosas que se hacen seguido (anadir a la lista, marcar
/// todo, usar esta carpeta como destino).
///
/// En Android trabaja sobre SAF (content://), que es lo unico que permite
/// recorrer carpetas ajenas a la app con permiso real de lectura. En PC
/// trabaja sobre rutas normales del sistema de archivos.
/// </summary>
public sealed class Explorer : UserControl
{
    private static IBrush B(string hex) => new SolidColorBrush(Color.Parse(hex));
    private static readonly IBrush BgPanel = B("#060D24");
    private static readonly IBrush BgRow = B("#0C1B4A");
    private static readonly IBrush TextMain = B("#F6F8FF");
    private static readonly IBrush TextSoft = B("#9FB3E8");
    private static readonly IBrush Gold = B("#FBBF24");
    private static readonly IBrush Line = B("#1F3B8C");

    /// <summary>Carpetas por las que se ha pasado, para el boton de subir.</summary>
    private readonly List<string> _back = new();

    private string _cur = "";
    private readonly List<Ent> _ents = new();
    private bool _loading;

    private readonly ListBox _list = new() { SelectionMode = SelectionMode.Multiple };
    private readonly TextBlock _path = new();
    private readonly TextBlock _info = new();
    private readonly Button _up = new();
    private readonly Border _body;
    private readonly StackPanel _empty = new();

    /// <summary>Se dispara con la carpeta actual cuando el usuario la elige como destino.</summary>
    public event Action<string>? FolderChosen;
    /// <summary>Se dispara con lo que el usuario marco, para meterlo en la cola de copia.</summary>
    public event Action<string[]>? FilesPicked;

    public string CurrentPath => _cur;

    public Explorer()
    {
        // Ruta de arranque: en Android, donde el usuario ya concedio permiso; en PC,
        // el escritorio o las descargas, que es lo que se usa el 99% de las veces.
        _cur = Initial();

        _path.FontSize = 12.5;
        _path.Foreground = TextSoft;
        _path.TextTrimming = TextTrimming.CharacterEllipsis;
        _path.VerticalAlignment = VerticalAlignment.Center;
        _path.MaxLines = 1;

        _info.FontSize = 12;
        _info.Foreground = TextSoft;
        _info.TextTrimming = TextTrimming.CharacterEllipsis;

        // La fila es un boton plano, asi que el toque va directo a el sin tener que
        // adivinar donde ha caido el dedo. Tocar una carpeta entra en ella; tocar un
        // archivo lo marca. Es lo que espera el dedo, y evita el menu contextual que
        // en un movil estorba.
        _list.ItemTemplate = new FuncDataTemplate<Ent>((e, _) => Row(e));
        _list.SelectionChanged += (_, _) => UpdateInfo();

        _up.Content = Ico.Get("arrowLeft", 18, TextSoft);
        _up.ToolTip = L.Get("up");
        _up.Width = 40; _up.Height = 36;
        _up.Background = BgRow;
        _up.BorderBrush = Line;
        _up.BorderThickness = new Thickness(1);
        _up.CornerRadius = new CornerRadius(8);
        _up.Click += (_, _) => GoUp();

        var head = new Grid
        {
            ColumnDefinitions = { new(GridLength.Auto), new(GridLength.Star) },
            Margin = new Thickness(0, 0, 0, 6)
        };
        Grid.SetColumn(_up, 0); head.Children.Add(_up);
        Grid.SetColumn(_path, 1);
        _path.Margin = new Thickness(8, 0, 0, 0);
        head.Children.Add(_path);

        // Estado vacio: sin esto, un movil en la raiz de Descargas muestra un hueco
        // negro sin explicacion.
        _empty.Spacing = 6;
        _empty.Children.Add(Ico.Get("folder", 34, TextSoft));
        _empty.Children.Add(new TextBlock
        {
            Text = L.Get("nothingHere"),
            Foreground = TextSoft,
            FontSize = 13,
            HorizontalAlignment = HorizontalAlignment.Center
        });
        _empty.IsVisible = false;

        var stack = new StackPanel();
        stack.Children.Add(_empty);
        stack.Children.Add(_list);

        _body = new Border
        {
            Background = BgPanel,
            BorderBrush = Line,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(9),
            Padding = new Thickness(4),
            Child = stack
        };

        var root = new Grid
        {
            RowDefinitions =
            {
                new(GridLength.Auto),   // cabecera con la ruta
                new(GridLength.Star),   // la lista
                new(GridLength.Auto)    // los botones de abajo
            },
            RowSpacing = 6
        };
        Grid.SetRow(head, 0); root.Children.Add(head);
        Grid.SetRow(_body, 1); root.Children.Add(_body);

        var bar = new WrapPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
        bar.Children.Add(Btn("check", "selectAll", SelectAll));
        bar.Children.Add(Btn("x", "deselectAll", () => _list.SelectedItems.Clear()));
        bar.Children.Add(Btn("plus", "addToList", () => FilesPicked?.Invoke(Selected())));
        bar.Children.Add(Btn("folder", "useAsDest", () => { if (!string.IsNullOrEmpty(_cur)) FolderChosen?.Invoke(_cur); }));
        bar.Children.Add(Btn("refresh", "refresh", Reload));
        Grid.SetRow(bar, 2); root.Children.Add(bar);
        _info.Margin = new Thickness(2, 0, 2, 0);
        Grid.SetRow(_info, 3);
        root.RowDefinitions.Add(new(GridLength.Auto));
        root.Children.Add(_info);

        Content = root;
        Reload();
    }

    private static string Initial()
    {
#if ANDROID
        try
        {
            var last = S.ExplorerLast;
            if (!string.IsNullOrEmpty(last) && global::QBasCopier.Android.DroidList.Exists(last)) return last;
            var home = global::QBasCopier.Android.DroidList.Home();
            if (!string.IsNullOrEmpty(home)) return home;
        }
        catch { }
        return "";
#else
        foreach (var d in new[] {
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory) })
            if (!string.IsNullOrEmpty(d) && Directory.Exists(d)) return d;
        return Environment.CurrentDirectory;
#endif
    }

    private Control Row(Ent e)
    {
        var left = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, VerticalAlignment = VerticalAlignment.Center };
        left.Children.Add(Ico.Get(e.IsDir ? "folder" : "file", 19, e.IsDir ? Gold : TextSoft));
        left.Children.Add(new TextBlock
        {
            Text = e.Name,
            FontSize = 13.5,
            Foreground = TextMain,
            VerticalAlignment = VerticalAlignment.Center,
            TextTrimming = TextTrimming.CharacterEllipsis,
            MaxWidth = 420
        });

        var row = new Grid { ColumnDefinitions = { new(GridLength.Star), new(GridLength.Auto) } };
        Grid.SetColumn(left, 0);
        row.Children.Add(left);
        if (!e.IsDir)
        {
            var sz = new TextBlock
            {
                Text = FilePane.Human(e.Size),
                FontSize = 12,
                Foreground = TextSoft,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(sz, 1);
            row.Children.Add(sz);
        }

        var b = new Button
        {
            Content = row,
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            CornerRadius = new CornerRadius(7),
            Padding = new Thickness(8, 7, 8, 7),
            HorizontalContentAlignment = HorizontalAlignment.Stretch,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        b.Click += (_, _) => { if (e.IsDir) Open(e.Path); else Toggle(e); };
        return b;
    }

    private Button Btn(string ico, string key, Action go)
    {
        var b = new Button
        {
            Content = Ico.Btn(ico, L.Get(key), TextMain, 17, 11.5),
            Background = BgRow,
            BorderBrush = Line,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            MinHeight = 38,
            Padding = new Thickness(10, 4, 10, 4)
        };
        b.Click += (_, _) => go();
        return b;
    }

    private void Toggle(Ent e)
    {
        foreach (var s in _list.SelectedItems.OfType<Ent>()) if (s.Path == e.Path) return;
        _list.SelectedItems.Add(e);
        UpdateInfo();
    }

    private string[] Selected() => _list.SelectedItems.OfType<Ent>().Select(x => x.Path).ToArray();

    private void SelectAll()
    {
        _list.SelectedItems.Clear();
        foreach (var e in _ents) if (!e.IsDir) _list.SelectedItems.Add(e);
        UpdateInfo();
    }

    /// <summary>Entra en una carpeta y la recuerda para poder volver.</summary>
    public void Open(string path)
    {
        if (string.IsNullOrEmpty(path)) return;
        if (_cur != path) _back.Add(_cur);
        _cur = path;
        S.ExplorerLast = path;
        S.Save();
        Reload();
    }

    private void GoUp()
    {
        if (_back.Count == 0) return;
        _cur = _back[^1];
        _back.RemoveAt(_back.Count - 1);
        Reload();
    }

    /// <summary>Vuelve a la pantalla y a la carpeta de destino sin perder el hilo.</summary>
    public void SetPathSilently(string p)
    {
        if (string.IsNullOrEmpty(p) || p == _cur) return;
        _back.Clear();
        _cur = p;
        Reload();
    }

    public void Reload()
    {
        if (_loading) return;
        _loading = true;
        try
        {
            _ents.Clear();
            _list.ItemsSource = null;
            if (!string.IsNullOrEmpty(_cur)) ReadDir(_cur, _ents);
            // Carpetas primero, luego archivos, y cada grupo con su nombre en
            // orden: es como se espera ver una carpeta, no como Nicholson 1970.
            var ordered = _ents
                .OrderByDescending(x => x.IsDir)
                .ThenBy(x => x.Name, StringComparer.CurrentCultureIgnoreCase)
                .ToList();
            _list.ItemsSource = ordered;
            _empty.IsVisible = ordered.Count == 0;
            _list.IsVisible = ordered.Count > 0;
            _up.IsEnabled = _back.Count > 0;
            _up.Opacity = _back.Count > 0 ? 1 : 0.35;
            _path.Text = Display(_cur);
            UpdateInfo();
        }
        finally { _loading = false; }
    }

    /// <summary>
    /// Lo que se ve en la cabecera. Un content:// es larguisimo e ilegible, asi que
    /// en Android solo se ensena el final, que es donde esta el nombre real.
    /// </summary>
    private static string Display(string p)
    {
#if ANDROID
        var i = p.LastIndexOf("/document/");
        if (i >= 0) p = p[(i + 10)..];
        var c = p.LastIndexOf(':');
        if (c >= 0) p = p[(c + 1)..];
        return p;
#else
        return p;
#endif
    }

    private void UpdateInfo()
    {
        var n = _list.SelectedItems.Count;
        long bytes = 0;
        foreach (Ent e in _list.SelectedItems.OfType<Ent>()) bytes += e.Size;
        _info.Text = n == 0
            ? L.Get("tapToMark")
            : string.Format(CultureInfo.CurrentCulture, L.Get("markedFmt"), n, FilePane.Human(bytes));
    }

    private static void ReadDir(string path, List<Ent> into)
    {
#if ANDROID
        foreach (var (u, n, d, s) in global::QBasCopier.Android.DroidList.Children(path))
            into.Add(new Ent(u, n, d, s));
#else
        try
        {
            if (!Directory.Exists(path)) return;
            foreach (var d in Directory.GetDirectories(path)) into.Add(new Ent(d, Path.GetFileName(d), true, 0));
            foreach (var f in Directory.GetFiles(path))
            {
                long s = 0;
                try { s = new FileInfo(f).Length; } catch { }
                into.Add(new Ent(f, Path.GetFileName(f), false, s));
            }
        }
        catch { }
#endif
    }
}
