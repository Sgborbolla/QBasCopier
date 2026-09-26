namespace QBasCopier;

/// <summary>
/// Formatea tamaños de archivo de forma legible.
///
/// Antes esto vivía dentro de FilePane, el panel de dos columnas que se quitó
/// cuando el explorador pasó a ser de un solo panel. Como solo se usaba para
/// poner "1,4 MB" al lado de un archivo, se separó en su propia clase y con
/// ella se fueron 400 líneas de código de panel que ya no servían para nada.
/// </summary>
public static class Fmt
{
    /// <summary>Unidad forzada desde Ajustes (KB, MB, GB, TB). Vacío = automática.</summary>
    public static string SizeUnit = "";

    private static readonly string[] Units = { "B", "KB", "MB", "GB", "TB" };

    public static string Human(long bytes)
    {
        if (bytes < 0) return "—";

        var force = (SizeUnit ?? "").Trim().ToUpperInvariant();
        if (force.Length > 0)
        {
            int fi = Array.IndexOf(Units, force);
            if (fi > 0)
            {
                double v = bytes;
                for (int i = 0; i < fi; i++) v /= 1024.0;
                return $"{v:0.##} {force}";
            }
        }

        double va = bytes;
        int ia = 0;
        while (va >= 1024 && ia < Units.Length - 1) { va /= 1024; ia++; }
        return $"{va:0.##} {Units[ia]}";
    }
}
