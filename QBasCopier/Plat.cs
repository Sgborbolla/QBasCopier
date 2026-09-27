using System.Diagnostics;
using System.Runtime.InteropServices;

namespace QBasCopier;

/// <summary>
/// Lo unico que necesita el sistema operativo y que no se puede hacer con
/// System.IO. En Android se resuelve con los intents de SAF y en Windows
/// cambiando el Explorador de archivos por defecto.
/// </summary>
public static class Plat
{
    /// <summary>Abre una carpeta en el explorador de archivos del sistema.</summary>
    public static void OpenFolder(string dir)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(dir)) return;
#if ANDROID
            // global:: porque, dentro de "namespace QBasCopier", el nombre "Android" se
            // resolvia a QBasCopier.Android (el namespace del proyecto de Android) y no
            // al de la plataforma, y estas llamadas no existian.
            var ctx = global::Android.App.Application.Context;
            var uri = global::AndroidX.Core.Content.FileProvider.GetUriForFile(
                ctx, ctx.PackageName + ".fileprovider", new global::Java.IO.File(dir));
            var i = new global::Android.Content.Intent(global::Android.Content.Intent.ActionView);
            i.SetDataAndType(uri, global::Android.Content.ContentResolver.TypeDirectory);
            i.AddFlags(global::Android.Content.Intent.ActivityFlags.NewTask);
            ctx.StartActivity(i);
#else
            Process.Start(new ProcessStartInfo
            {
                FileName = OperatingSystem.IsWindows() ? "explorer.exe" : "xdg-open",
                ArgumentList = { dir },
                UseShellExecute = true
            });
#endif
        }
        catch { }
    }

    /// <summary>Abre un archivo con la app que tenga asociada su extension.</summary>
    public static void OpenFile(string path)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) return;
#if ANDROID
            var ctx = global::Android.App.Application.Context;
            var uri = global::AndroidX.Core.Content.FileProvider.GetUriForFile(
                ctx, ctx.PackageName + ".fileprovider", new global::Java.IO.File(path));
            var mime = global::Android.Webkit.MimeTypeMap.Singleton.GetMimeTypeFromExtension(System.IO.Path.GetExtension(path)) ?? "*/*";
            var i = new global::Android.Content.Intent(global::Android.Content.Intent.ActionView);
            i.SetDataAndType(uri, mime);
            i.AddFlags(global::Android.Content.Intent.ActivityFlags.NewTask);
            ctx.StartActivity(i);
#else
            Process.Start(new ProcessStartInfo { FileName = path, UseShellExecute = true });
#endif
        }
        catch { }
    }

    /// <summary>
    /// Espacio libre de la unidad que contiene la ruta, en bytes.
    /// Devuelve -1 cuando la unidad no se puede consultar, para no bloquear la copia.
    /// </summary>
    public static long FreeSpace(string path)
    {
        try
        {
            var root = Path.GetPathRoot(Path.GetFullPath(path));
            if (string.IsNullOrEmpty(root)) return -1;
            var di = new DriveInfo(root);
            return di.AvailableFreeSpace;
        }
        catch { return -1; }
    }
}
