using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Platform;

namespace QBasCopier;

/// <summary>
/// Iconos de la interfaz, dibujados en tiempo de ejecucion con trazos geometricos.
///
/// No se usan archivos .svg ni fuentes de iconos a proposito: en Android un recurso
/// dibujado tiene que ir empaquetado en la APK y el escalado se ve borroso en pantallas
/// de alta densidad. Aqui cada icono es una lista de polilineas y circunferencias en una
/// caja de 24x24 que se escala al tamano pedido, asi que se ve nitido en cualquier
/// pantalla y ocupa cero bytes.
/// </summary>
public static class Ico
{
    // Cada icono: polilineas (x,y) y circunferencias (x,y,r).
    // Cadenas: nombre -> polilineas; circulos: nombre -> circulos.
    private static readonly Dictionary<string, double[][]> Lines = new()
    {
        ["copy"] = new[] { new[] { 8.0, 2, 20, 2, 20, 16, 8, 16, 8, 2 }, new[] { 4.0, 8, 16, 8, 16, 22, 4, 22, 4, 8 } },
        ["cut"] = new[] { new[] { 5.0, 5, 12, 12 }, new[] { 19.0, 5, 12, 12 }, new[] { 12.0, 12, 12, 21 } },
        ["paste"] = new[] { new[] { 6.0, 3, 16, 3, 16, 7, 20, 7, 20, 21, 6, 21, 6, 3 }, new[] { 9.0, 3, 9, 7, 15, 7, 15, 3 } },
        ["folder"] = new[] { new[] { 2.0, 20, 2, 6, 9, 6, 12, 9, 22, 9, 22, 20, 2, 20 } },
        ["folderPlus"] = new[] { new[] { 2.0, 20, 2, 6, 9, 6, 12, 9, 18, 9, 18, 20, 2, 20 }, new[] { 20.0, 13, 20, 22 }, new[] { 15.5, 17.5, 24.5, 17.5 } },
        ["filePlus"] = new[] { new[] { 4.0, 2, 13, 2, 18, 7, 18, 16, 4, 16, 4, 2 }, new[] { 13.0, 2, 13, 7, 18, 7 }, new[] { 18.0, 20, 18, 25 }, new[] { 15.5, 22.5, 20.5, 22.5 } },
        ["hdd"] = new[] { new[] { 2.0, 6, 22, 6, 22, 18, 2, 18, 2, 6 }, new[] { 2.0, 12, 22, 12 } },
        ["up"] = new[] { new[] { 12.0, 20, 12, 4 }, new[] { 5.0, 11, 12, 4, 19, 11 } },
        ["home"] = new[] { new[] { 2.0, 12, 12, 3, 22, 12 }, new[] { 5.0, 10, 5, 21, 19, 21, 19, 10 } },
        ["play"] = new[] { new[] { 6.0, 4, 20, 12, 6, 20, 6, 4 } },
        ["pause"] = new[] { new[] { 7.0, 4, 10, 4, 10, 20, 7, 20, 7, 4 }, new[] { 14.0, 4, 17, 4, 17, 20, 14, 20, 14, 4 } },
        ["stop"] = new[] { new[] { 6.0, 6, 18, 6, 18, 18, 6, 18, 6, 6 } },
        ["skip"] = new[] { new[] { 5.0, 4, 5, 20 }, new[] { 11.0, 4, 19, 12, 11, 20, 11, 4 }, new[] { 19.0, 4, 19, 20 } },
        ["trash"] = new[] { new[] { 4.0, 6, 20, 6 }, new[] { 8.0, 6, 9, 3, 15, 3, 16, 6 }, new[] { 6.0, 6, 7, 21, 17, 21, 18, 6 }, new[] { 10.0, 10, 10, 17 }, new[] { 14.0, 10, 14, 17 } },
        ["gear"] = new[] { new[] { 12.0, 3, 12, 5 }, new[] { 12.0, 19, 12, 21 }, new[] { 3.0, 12, 5, 12 }, new[] { 19.0, 12, 21, 12 }, new[] { 5.6, 5.6, 7, 7 }, new[] { 17, 17, 18.4, 18.4 }, new[] { 18.4, 5.6, 17, 7 }, new[] { 7, 17, 5.6, 18.4 } },
        ["globe"] = new[] { new[] { 2.0, 12, 22, 12 }, new[] { 12.0, 2, 12, 22 }, new[] { 5.0, 6, 19, 6 }, new[] { 5.0, 18, 19, 18 } },
        ["history"] = new[] { new[] { 3.0, 12, 3, 7, 7, 3, 12, 3 }, new[] { 3.0, 7, 3, 12, 8, 12 }, new[] { 12.0, 6, 12, 12, 17, 15 } },
        ["wifi"] = new[] { new[] { 2.0, 8, 12, 2, 22, 8 }, new[] { 6.0, 13, 12, 9, 18, 13 }, new[] { 9.0, 18, 12, 16, 15, 18 } },
        ["device"] = new[] { new[] { 3.0, 3, 21, 3, 21, 15, 3, 15, 3, 3 }, new[] { 9.0, 19, 15, 19 }, new[] { 12.0, 15, 12, 19 }, new[] { 7.0, 20, 17, 20 } },
        ["send"] = new[] { new[] { 3.0, 12, 21, 4, 21, 20, 3, 12 }, new[] { 3.0, 12, 15, 12 } },
        ["receive"] = new[] { new[] { 12.0, 2, 12, 14 }, new[] { 6.0, 8, 12, 14, 18, 8 }, new[] { 3.0, 16, 3, 21, 21, 21, 21, 16 } },
        ["qr"] = new[] { new[] { 3.0, 3, 9, 3, 9, 9, 3, 9, 3, 3 }, new[] { 15.0, 3, 21, 3, 21, 9, 15, 9, 15, 3 }, new[] { 3.0, 15, 9, 15, 9, 21, 3, 21, 3, 15 }, new[] { 12.0, 12, 13, 12 }, new[] { 15.0, 12, 16, 12 }, new[] { 19.0, 12, 20, 12 }, new[] { 12.0, 15, 13, 15 }, new[] { 19.0, 15, 20, 15 }, new[] { 12.0, 19, 13, 19 }, new[] { 15.0, 19, 16, 19 } },
        ["check"] = new[] { new[] { 4.0, 13, 9, 18, 20, 6 } },
        ["x"] = new[] { new[] { 5.0, 5, 19, 19 }, new[] { 19.0, 5, 5, 19 } },
        ["chevronUp"] = new[] { new[] { 5.0, 15, 12, 8, 19, 15 } },
        ["chevronDown"] = new[] { new[] { 5.0, 9, 12, 16, 19, 9 } },
        ["plus"] = new[] { new[] { 12.0, 4, 12, 20 }, new[] { 4.0, 12, 20, 12 } },
        ["minus"] = new[] { new[] { 4.0, 12, 20, 12 } },
        ["clock"] = new[] { new[] { 12.0, 5, 12, 12, 17, 15 } },
        ["cpu"] = new[] { new[] { 6.0, 6, 18, 6, 18, 18, 6, 18, 6, 6 }, new[] { 9.0, 9, 15, 9, 15, 15, 9, 15, 9, 9 }, new[] { 9.0, 2, 9, 6 }, new[] { 15.0, 2, 15, 6 }, new[] { 9.0, 18, 9, 22 }, new[] { 15.0, 18, 15, 22 }, new[] { 2.0, 9, 6, 9 }, new[] { 2.0, 15, 6, 15 }, new[] { 18.0, 9, 22, 9 }, new[] { 18.0, 15, 22, 15 } },
        ["list"] = new[] { new[] { 3.0, 6, 21, 6 }, new[] { 3.0, 12, 21, 12 }, new[] { 3.0, 18, 21, 18 } },
        ["table"] = new[] { new[] { 3.0, 3, 21, 3, 21, 21, 3, 21, 3, 3 }, new[] { 3.0, 9, 21, 9 }, new[] { 3.0, 15, 21, 15 }, new[] { 10.0, 3, 10, 21 }, new[] { 16.0, 3, 16, 21 } },
        ["shield"] = new[] { new[] { 12.0, 2, 20, 6, 20, 12, 12, 22, 4, 12, 4, 6, 12, 2 } },
        ["power"] = new[] { new[] { 6.0, 6, 12, 11 }, new[] { 18.0, 6, 12, 11 }, new[] { 12.0, 3, 12, 12 }, new[] { 6.0, 10, 4, 16 }, new[] { 18.0, 10, 20, 16 }, new[] { 5.0, 20, 19, 20 } },
        ["download"] = new[] { new[] { 12.0, 2, 12, 15 }, new[] { 6.0, 9, 12, 15, 18, 9 }, new[] { 3.0, 17, 3, 21, 21, 21, 21, 17 } },
        ["upload"] = new[] { new[] { 12.0, 16, 12, 3 }, new[] { 6.0, 9, 12, 3, 18, 9 }, new[] { 3.0, 17, 3, 21, 21, 21, 21, 17 } },
        ["star"] = new[] { new[] { 12.0, 2, 15, 9, 22, 10, 17, 15, 18, 22, 12, 19, 6, 22, 7, 15, 2, 10, 9, 9, 12, 2 } },
        ["search"] = new[] { new[] { 15.0, 15, 22, 22 } },
        ["info"] = new[] { new[] { 12.0, 10, 12, 17 }, new[] { 12.0, 7, 12, 7.1 } },
        ["image"] = new[] { new[] { 3.0, 4, 21, 4, 21, 20, 3, 20, 3, 4 }, new[] { 3.0, 16, 9, 10, 15, 16, 18, 13, 21, 16 } },
        ["fold"] = new[] { new[] { 4.0, 19, 20, 19 }, new[] { 7.0, 14, 17, 14 }, new[] { 10.0, 9, 14, 9 } },
        ["expand"] = new[] { new[] { 4.0, 9, 4, 4, 9, 4 }, new[] { 15.0, 4, 20, 4, 20, 9 }, new[] { 20.0, 15, 20, 20, 15, 20 }, new[] { 9.0, 20, 4, 20, 4, 15 } },
        ["exit"] = new[] { new[] { 12.0, 3, 4, 3, 4, 21, 12, 21 }, new[] { 9.0, 12, 21, 12 }, new[] { 17.0, 8, 21, 12, 17, 16 } },
        ["lang"] = new[] { new[] { 3.0, 5, 13, 5, 8, 19 }, new[] { 4.5, 10, 11.5, 10 }, new[] { 21.0, 5, 16, 20, 13, 12, 21, 12 } },
        ["refresh"] = new[] { new[] { 20.0, 12, 4, 12, 4, 7, 9, 4 }, new[] { 4.0, 12, 12, 12, 12, 18, 7, 20 } },
        ["swap"] = new[] { new[] { 4.0, 9, 16, 9 }, new[] { 12.0, 5, 16, 9, 12, 13 }, new[] { 20.0, 16, 8, 16 }, new[] { 12.0, 12, 8, 16, 12, 20 } },
        ["arrowRight"] = new[] { new[] { 4.0, 12, 20, 12 }, new[] { 14.0, 6, 20, 12, 14, 18 } },
        ["arrowLeft"] = new[] { new[] { 20.0, 12, 4, 12 }, new[] { 10.0, 6, 4, 12, 10, 18 } },
        ["music"] = new[] { new[] { 9.0, 18, 9.0, 5, 19.0, 3, 19.0, 16 }, new[] { 9.0, 9, 19.0, 7 } },
        ["sliders"] = new[] { new[] { 4.0, 7, 20, 7 }, new[] { 4.0, 17, 20, 17 }, new[] { 9.0, 4, 9, 10 }, new[] { 15.0, 14, 15, 20 } },
        ["warning"] = new[] { new[] { 12.0, 3, 22, 20, 2, 20, 12, 3 }, new[] { 12.0, 9, 12, 14 }, new[] { 12.0, 16.5, 12, 16.6 } },
        ["transfer"] = new[] { new[] { 2.0, 7, 15, 7 }, new[] { 11.0, 3, 15, 7, 11, 11 }, new[] { 22.0, 17, 9, 17 }, new[] { 13.0, 13, 9, 17, 13, 21 } },
        ["eye"] = new[] { new[] { 2.0, 12, 7, 6, 17, 6, 22, 12, 17, 18, 7, 18, 2, 12 } },
        ["plug"] = new[] { new[] { 7.0, 3, 7, 9 }, new[] { 17.0, 3, 17, 9 }, new[] { 4.0, 9, 20, 9, 20, 14, 4, 14, 4, 9 }, new[] { 12.0, 14, 12, 21 } },
        ["file"] = new[] { new[] { 5.0, 2, 14, 2, 19, 7, 19, 22, 5, 22, 5, 2 }, new[] { 14.0, 2, 14, 7, 19, 7 } },
    };

    private static readonly Dictionary<string, double[][]> Circles = new()
    {
        ["hdd"] = new[] { new[] { 17.0, 15.0, 1.6 } },
        ["gear"] = new[] { new[] { 12.0, 12.0, 4.2 } },
        ["globe"] = new[] { new[] { 12.0, 12.0, 10.0 } },
        ["history"] = new[] { new[] { 12.0, 12.0, 9.0 } },
        ["clock"] = new[] { new[] { 12.0, 12.0, 9.0 } },
        ["search"] = new[] { new[] { 10.0, 10.0, 6.5 } },
        ["info"] = new[] { new[] { 12.0, 12.0, 9.0 } },
        ["qr"] = new[] { new[] { 6.0, 6.0, 1.1 }, new[] { 18.0, 6.0, 1.1 }, new[] { 6.0, 18.0, 1.1 } },
        ["star"] = new[] { new[] { 12.0, 12.0, 0.4 } },
        ["music"] = new[] { new[] { 6.0, 18.0, 3.0 }, new[] { 16.0, 16.0, 3.0 } },
        ["warning"] = new[] { new[] { 12.0, 17.0, 0.4 } },
        // Eslabones: dos anillos que se pisan, como una cadena. Es el icono de
        // "unirse" en la ventanita de Transferir.
        ["link"] = new[] { new[] { 8.6, 12.0, 3.6 }, new[] { 15.4, 12.0, 3.6 } },
    };

    private static readonly Dictionary<string, Geometry> Cache = new();

    private static Geometry Build(string name)
    {
        if (Cache.TryGetValue(name, out var g)) return g;
        var geo = new StreamGeometry();
        using (var ctx = geo.Open())
        {
            if (Lines.TryGetValue(name, out var polys))
            {
                foreach (var p in polys)
                {
                    if (p.Length < 4) continue;
                    ctx.BeginFigure(new Point(p[0], p[1]), isFilled: false, isClosed: false);
                    for (int i = 2; i + 1 < p.Length; i += 2)
                        ctx.LineTo(new Point(p[i], p[i + 1]));
                    ctx.EndFigure(closeFigure: false);
                }
            }
            if (Circles.TryGetValue(name, out var circles))
            {
                foreach (var c in circles)
                {
                    if (c.Length < 3) continue;
                    var cx = c[0];
                    var cy = c[1];
                    var r = c[2];
                    ctx.BeginFigure(new Point(cx - r, cy), isFilled: false, isClosed: true);
                    ctx.ArcTo(new Point(cx + r, cy), new Size(r, r), 0, isLargeArc: false, sweepDirection: SweepDirection.Clockwise);
                    ctx.ArcTo(new Point(cx - r, cy), new Size(r, r), 0, isLargeArc: false, sweepDirection: SweepDirection.Clockwise);
                    ctx.EndFigure(closeFigure: true);
                }
            }
        }
        Cache[name] = geo;
        return geo;
    }

    /// <summary>Control con el icono pedido, del tamano y color indicados.</summary>
    public static Control Get(string name, double size = 20, IBrush? brush = null)
    {
        if (!Lines.ContainsKey(name) && !Circles.ContainsKey(name)) name = "info";
        return new IconView { Spec = name, IconSize = size, Brush = brush };
    }

    /// <summary>Cambia el tamano de un icono ya creado, para el escalado por pantalla.</summary>
    public static void SetSize(Control c, double size)
    {
        if (c is IconView v) { v.IconSize = size; v.Width = size; v.Height = size; v.InvalidateMeasure(); v.InvalidateVisual(); }
    }

    /// <summary>Control con icono y texto al lado, para los botones de la barra de comandos.</summary>
    public static Control Btn(string icon, string text, IBrush? brush, double size = 20, double fontSize = 12)
    {
        var sp = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Spacing = 3,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        sp.Children.Add(new IconView { Spec = icon, IconSize = size, Brush = brush, HorizontalAlignment = HorizontalAlignment.Center });
        if (!string.IsNullOrEmpty(text))
            sp.Children.Add(new TextBlock
            {
                Text = text,
                FontSize = fontSize,
                Foreground = brush,
                HorizontalAlignment = HorizontalAlignment.Center,
                TextAlignment = TextAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis
            });
        return sp;
    }

    private sealed class IconView : Control
    {
        public string Spec { get; set; } = "info";
        public double IconSize { get; set; } = 20;

        public IBrush? Brush
        {
            get => _brush;
            set { _brush = value; InvalidateVisual(); }
        }

        IBrush? _brush = Brushes.White;

        public IconView()
        {
            Width = IconSize;
            Height = IconSize;
        }

        /// <summary>
        /// En Avalonia 11 Measure no es virtual: el tamaño que un control quiere
        /// ocupar se declara con MeasureOverride, no sobreescribiendo Measure.
        /// Por eso el icono declara un cuadrado de su tamano y asi el que lo
        /// contiene lo mide bien.
        /// </summary>
        protected override Size MeasureOverride(Size availableSize)
        {
            var s = IconSize > 0 ? IconSize : 24;
            return new Size(s, s);
        }

        protected override void Render(DrawingContext ctx)
        {
            base.Render(ctx);
            var b = _brush ?? Brushes.White;
            var pen = new Pen(b, Math.Max(1.4, IconSize * 0.085));
            var geo = Build(Spec);
            var scale = IconSize / 24.0;
            using (ctx.PushTransform(Matrix.CreateScale(scale, scale)))
                ctx.DrawGeometry(null, pen, geo);
        }
    }
}
