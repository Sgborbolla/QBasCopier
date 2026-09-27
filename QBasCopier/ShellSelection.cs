#if !ANDROID
using System.Runtime.InteropServices;

namespace QBasCopier;

/// <summary>
/// Los archivos que el Explorador tiene marcados en este momento.
///
/// Al lanzar un comando del menu contextual, Windows solo pone "%1": el primer
/// archivo de los veinte que hay marcados. Para copiar la seleccion entera hay que
/// preguntarselo al propio Explorador, que es lo que hace esta clase. Sin esto, el
/// "Copiar con..." del menu solo copiaria un archivo de cada vez, que no es lo que
/// nadie espera de un copiador.
///
/// Se habla con el Explorador a traves de la automatizacion de COM, que hay que hacer
/// en un hilo con su propio apartamento (STA); por eso va en un hilo aparte con
/// tiempo limite: si el Explorador esta ocupado o no contesta, se sigue sin saber y
/// el proceso no se queda colgado esperando.
/// </summary>
public static class ShellSelection
{
    public sealed record Resultado(List<string> Archivos, string Carpeta);

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    /// <summary>Lo que hay marcado ahora, o null si no se ha podido averiguar.</summary>
    public static Resultado? Ahora()
    {
        if (!OperatingSystem.IsWindows()) return null;
        Resultado? r = null;
        var t = new Thread(() => r = Busca()) { IsBackground = true, Name = "shell-selection" };
        if (OperatingSystem.IsWindows()) t.SetApartmentState(ApartmentState.STA);
        t.Start();
        // Si en cuatro segundos no ha volvido, se sigue sin la seleccion: es preferible
        // copiar el primer archivo a quedarse esperando.
        t.Join(4000);
        return r;
    }

    private static Resultado? Busca()
    {
        if (!OperatingSystem.IsWindows()) return null;   // la automatizacion de COM es solo de Windows
        try
        {
            var fg = GetForegroundWindow().ToInt64();
            var tipo = Type.GetTypeFromProgID("Shell.Application");
            if (tipo == null) return null;
            dynamic shell = Activator.CreateInstance(tipo)!;
            dynamic ventanas = shell.Windows();
            for (int i = 0; i < (int)ventanas.Count; i++)
            {
                dynamic w = ventanas.Item(i);
                if ((long)w.HWND != fg) continue;   // solo la ventana que esta delante

                var carpeta = Ruta(w);
                var archivos = new List<string>();
                dynamic sel = w.Document.SelectedItems();
                for (int j = 0; j < (int)sel.Count; j++)
                {
                    try
                    {
                        var p = (string)sel.Item(j).Path;
                        if (!string.IsNullOrWhiteSpace(p)) archivos.Add(p);
                    }
                    catch { }
                }
                return new Resultado(archivos, carpeta);
            }
        }
        catch (Exception e) { CrashLog.Save("ERROR al leer la seleccion del Explorador: " + e.Message); }
        return null;
    }

    private static string Ruta(dynamic ventana)
    {
        try
        {
            var ruta = (string)ventana.Document.Folder.Self.Path;
            return ruta ?? "";
        }
        catch { return ""; }
    }

    /// <summary>
    /// Adapta la linea de ordenes a lo que el Explorador tiene marcado. "recibidos"
    /// son las rutas que venia con el comando; aqui se amplian a la seleccion entera
    /// y se decide la carpeta de destino.
    /// </summary>
    public static bool Completa(List<string> recibidos, ref string? dest, bool move)
    {
        var sel = Ahora();
        if (sel == null || sel.Archivos.Count == 0) return false;

        // Lo que venia en la linea de ordenes puede ser la carpeta de destino (un
        // "pegar dentro de"), uno de los archivos marcados, o directamente nada. Se
        // busca una carpeta que no este entre lo marcado: esa es la de destino.
        foreach (var r in recibidos)
        {
            if (r.Length == 0 || !Directory.Exists(r)) continue;
            if (sel.Archivos.Any(x => string.Equals(x, r, StringComparison.OrdinalIgnoreCase))) continue;
            dest = r;
            break;
        }

        recibidos.Clear();
        recibidos.AddRange(sel.Archivos);

        // Mover a la carpeta que ya se esta viendo no copia nada, asi que en ese caso no
        // se rellena: se deja que el usuario elija donde va en la ventana principal.
        if (string.IsNullOrEmpty(dest) && !move && sel.Carpeta.Length > 0)
        {
            bool dentro = sel.Archivos.All(f =>
            {
                try { return Path.GetDirectoryName(Path.GetFullPath(f)) == Path.GetFullPath(sel.Carpeta); }
                catch { return false; }
            });
            if (!dentro) dest = sel.Carpeta;
        }
        return true;
    }
}
#endif
