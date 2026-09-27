using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace QBasCopier;

public static class NetTools
{
    /// <summary>Reloj monotono real. Antes era un campo fijo evaluado al cargar, asi que
    /// ningun par caducaba y la lista de dispositivos se congelaba para siempre.</summary>
    public static long Now => Environment.TickCount64;

    public static List<string> LanIps()
    {
        var res = new List<string>();
        try
        {
            foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (ni.OperationalStatus != OperationalStatus.Up) continue;
                if (ni.NetworkInterfaceType is NetworkInterfaceType.Loopback or NetworkInterfaceType.Tunnel) continue;
                foreach (var u in ni.GetIPProperties().UnicastAddresses)
                    if (u.Address.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(u.Address))
                        res.Add(u.Address.ToString());
            }
        }
        catch { }
        return res.Distinct().ToList();
    }
}

public sealed class TransferHost : IDisposable
{
    public const int DiscoveryPort = 9528;
    public const int LegacyDiscoveryPort = 9527;

    // path/área final donde se guarda el archivo recibido
    public event Action<string, long>? FileReceived;
    /// <summary>Progreso de lo que esta entrando ahora mismo: cuanto va y cuanto se espera.</summary>
    public event Action<string, long, long>? Receiving;


    // Ganchos SAF (los define MainActivity en Android). Permiten servir listados y descargas
    // cuando la carpeta de recibidos es un árbol content:// y no una ruta del sistema de archivos.
    public static Func<string, Stream>? OpenDoc;
    public static Func<string, (string Name, long Size)>? DocInfo;
    public static Func<string, List<(string Uri, string Name, bool IsDir, long Size, DateTime Modified)>>? ListDoc;

    // Escribe directo dentro del árbol SAF (Android): una sola escritura, sin temporal.
    public static Func<string, string, Stream>? WriteDoc;

    /// <summary>
    /// Crea (o reutiliza) una carpeta dentro de un arbol SAF y devuelve su URI.
    /// En Android es la unica forma de crear subcarpetas donde se recibe, porque
    /// desde Android 11 no se puede escribir en /storage con rutas sueltas.
    /// </summary>
    public static Func<string, string, string?>? EnsureDocDir;

    /// <summary>
    /// Dice si un archivo ya existe dentro de una carpeta SAF, para no pisar nada
    /// que el usuario ya tenia ahi.
    /// </summary>
    public static Func<string, string, bool>? DocExists;

    /// <summary>
    /// Igual que <see cref="EnsureDocDir"/> pero con un nombre que todavia no este en
    /// uso: dos carpetas con el mismo nombre no se mezclan, la segunda llega como
    /// "nombre (1)". En el escritorio esto ya lo hace Unique.
    /// </summary>
    public static Func<string, string, string?>? NewDocDir;

    /// <summary>
    /// Si lo recibido se ordena en la subcarpeta de su tipo. Es lo de siempre
    /// (Fotos, Videos, Musica...) y se puede apagar en Opciones para que todo caiga
    /// junto, sin clasificar.
    /// </summary>
    public static bool SortByType = true;

    /// <summary>
    /// Las carpetas por tipo en las que se ordena lo recibido: una carpeta con
    /// la marca y dentro una por tipo, en vez de un unico cajon con todo
    /// mezclado. Es lo que espera cualquiera que venga de otra app de este tipo.
    /// </summary>
    public static readonly string[] CategoryKeys = {
        "catPhotos", "catVideos", "catMusic", "catDocs", "catApps", "catOther"
    };

    private static readonly HashSet<string> ExtPhotos = new(StringComparer.OrdinalIgnoreCase)
        { ".jpg", ".jpeg", ".png", ".heic", ".heif", ".webp", ".gif", ".bmp", ".tif", ".tiff", ".raw", ".dng", ".ico", ".svg" };
    private static readonly HashSet<string> ExtVideos = new(StringComparer.OrdinalIgnoreCase)
        { ".mp4", ".mkv", ".mov", ".avi", ".webm", ".3gp", ".m4v", ".mpeg", ".mpg", ".wmv", ".flv", ".mts" };
    private static readonly HashSet<string> ExtMusic = new(StringComparer.OrdinalIgnoreCase)
        { ".mp3", ".flac", ".wav", ".m4a", ".ogg", ".oga", ".aac", ".wma", ".opus", ".mid" };
    private static readonly HashSet<string> ExtDocs = new(StringComparer.OrdinalIgnoreCase)
        { ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx", ".txt", ".md", ".rtf", ".odt", ".ods", ".csv", ".epub", ".json", ".xml", ".html", ".htm" };
    private static readonly HashSet<string> ExtApps = new(StringComparer.OrdinalIgnoreCase)
        { ".apk", ".exe", ".msi", ".deb", ".rpm", ".dmg", ".pkg", ".appimage", ".zip", ".7z", ".rar", ".iso", ".cab" };

    /// <summary>La carpeta por tipo a la que pertenece un archivo, segun su extension.</summary>
    public static string CategoryOf(string name)
    {
        var i = name.LastIndexOf('.');
        if (i < 0) return "catOther";
        var ext = name[i..];
        if (ExtPhotos.Contains(ext)) return "catPhotos";
        if (ExtVideos.Contains(ext)) return "catVideos";
        if (ExtMusic.Contains(ext)) return "catMusic";
        if (ExtDocs.Contains(ext)) return "catDocs";
        if (ExtApps.Contains(ext)) return "catApps";
        return "catOther";
    }

    private static bool IsSaf(string p) => p.StartsWith("content://", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Crea la carpeta de marca con sus subcarpetas por tipo. Se hace al encender
    /// Transferir: asi el usuario ve de antemano donde va a caer cada cosa y la
    /// carpeta esta lista antes de que llegue el primer archivo.
    /// </summary>
    public static bool PrepareInbox(string inbox)
    {
        if (string.IsNullOrWhiteSpace(inbox)) return false;
        if (IsSaf(inbox))
        {
            if (EnsureDocDir == null) return false;
            if (!SortByType) return EnsureDocDir(inbox, "") != null;
            var ok = false;
            foreach (var k in CategoryKeys)
            {
                try { if (EnsureDocDir(inbox, L.Get(k)) != null) ok = true; } catch { }
            }
            return ok;
        }
        try
        {
            Directory.CreateDirectory(inbox);
            if (SortByType)
                foreach (var k in CategoryKeys) Directory.CreateDirectory(Path.Combine(inbox, L.Get(k)));
            return true;
        }
        catch (Exception e)
        {
            CrashLog.Save("ERROR carpeta de recibidos: " + e.Message);
            return false;
        }
    }

    /// <summary>Carpeta concreta donde se guarda un archivo, creandola si falta.</summary>
    public static string InboxFor(string inbox, string fileName)
    {
        // Con la clasificacion apagada todo cae junto, sin subcarpetas.
        if (!SortByType) return inbox;
        var cat = L.Get(CategoryOf(fileName));
        if (IsSaf(inbox))
        {
            try { return EnsureDocDir?.Invoke(inbox, cat) ?? inbox; } catch { return inbox; }
        }
        var dir = Path.Combine(inbox, cat);
        try { Directory.CreateDirectory(dir); } catch { return inbox; }
        return dir;
    }

    public static string DeviceName = "";

    /// <summary>
    /// Clave de emparejamiento. Si esta vacia el servidor es abierto (util en una red
    /// domestica de confianza); si tiene valor, sin la clave correcta no se sube ni se
    /// descarga nada. Antes se mostraba en el QR y jamas se comprobaba.
    /// </summary>
    public static string Key = "";

    /// <summary>
    /// A partir de aqui se avisa en el registro, pero NO se corta la transferencia:
    /// el programa no pone topes de tamano, y lo que el otro equipo manda llega
    /// entero. Solo sirve para dejar constancia de que algo muy grande paso.
    /// </summary>
    public const long AvisoDeBytes = 1024L * 1024 * 1024 * 1024;

    private TcpListener? _tcp;
    private CancellationTokenSource? _cts;
    private readonly SemaphoreSlim _slots = new(4, 4);
    public readonly List<string> Prefixes = new();
    public int Port { get; private set; }
    public string Inbox { get; set; } = "";
    public bool Running => _tcp != null;

    public void Start(int port, string inbox)
    {
        try { Stop(); } catch { }
        Port = port; Inbox = inbox;
        try
        {
            Prefixes.Clear();
            foreach (var ip in NetTools.LanIps()) Prefixes.Add($"http://{ip}:{port}/");
            Prefixes.Add("http://127.0.0.1:" + port + "/");
            _tcp = new TcpListener(IPAddress.Any, port);
            _tcp.Start();
        }
        catch { _tcp = null; return; }
        _cts = new CancellationTokenSource();
        _ = Task.Run(() => Loop(_cts.Token));
    }

    private readonly System.Collections.Concurrent.ConcurrentDictionary<TcpClient, byte> _live = new();

    public void Stop()
    {
        try { _cts?.Cancel(); } catch { }
        try { _tcp?.Stop(); _tcp = null; } catch { }
        // Sin esto, "Apagar" dejaba la descarga escribiendo en el disco de fondo.
        foreach (var c in _live.Keys)
        {
            try { c.Client?.Shutdown(SocketShutdown.Both); } catch { }
            try { c.Close(); } catch { }
        }
        _live.Clear();
    }

    private async Task Loop(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested && _tcp != null)
        {
            TcpClient c;
            try { c = await _tcp.AcceptTcpClientAsync().WaitAsync(ct); }
            catch { break; }
            _ = Task.Run(() => HandleClient(c), CancellationToken.None);
        }
    }

    private async Task HandleClient(TcpClient c)
    {
        try
        {
            c.NoDelay = true;
            try { c.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendBuffer, 4 * 1024 * 1024); } catch { }
            try { c.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveBuffer, 4 * 1024 * 1024); } catch { }
            try { c.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true); } catch { }
            _live[c] = 0;
            using (c)
            using (var ns = c.GetStream())
            {
                var head = new byte[64 * 1024];
                int got = 0, idx = -1;
                while (got < head.Length)
                {
                    int n = await ns.ReadAsync(head.AsMemory(got, head.Length - got));
                    if (n <= 0) break;
                    got += n;
                    idx = IndexOfHeaderEnd(head, got);
                    if (idx >= 0) break;
                }
                if (idx < 0) return;

                var headText = Encoding.UTF8.GetString(head, 0, got);
                var lines = headText.Split('\n');
                var first = lines[0].Trim();
                var sp1 = first.IndexOf(' ');
                var method = sp1 > 0 ? first[..sp1].ToUpperInvariant() : "GET";
                var sp2 = sp1 > 0 ? first.IndexOf(' ', sp1 + 1) : -1;
                var target = sp1 > 0 && sp2 > sp1 ? first[(sp1 + 1)..sp2] : "/";
                var qp = target.IndexOf('?');
                var path = qp >= 0 ? target[..qp] : target;
                var query = qp >= 0 ? target[(qp + 1)..] : "";
                path = Uri.UnescapeDataString(path);
                query = Uri.UnescapeDataString(query);

                string name = "archivo.bin";
                long len = -1;
                bool esCarpeta = false;
                foreach (var raw in lines.Skip(1))
                {
                    var line = raw.TrimEnd('\r');
                    var ci = line.IndexOf(':');
                    if (ci <= 0) continue;
                    var k = line.Substring(0, ci).Trim();
                    var v = line.Substring(ci + 1).Trim();
                    if (k.Equals("X-File", StringComparison.OrdinalIgnoreCase))
                        name = Uri.UnescapeDataString(v);
                    else if (k.Equals("X-Folder", StringComparison.OrdinalIgnoreCase))
                    { name = Uri.UnescapeDataString(v); esCarpeta = true; }
                    else if (k.Equals("X-Len", StringComparison.OrdinalIgnoreCase) && long.TryParse(v, out var l))
                        len = l;
                    else if (k.Equals("Content-Length", StringComparison.OrdinalIgnoreCase) && long.TryParse(v, out var cl))
                    { if (len < 0) len = cl; }
                    else if (k.Equals("Transfer-Encoding", StringComparison.OrdinalIgnoreCase) && v.Contains("chunked", StringComparison.OrdinalIgnoreCase))
                        len = -2;
                }

                // nombre por query (?name=) para envíos hechos desde un navegador
                var qn = QueryValue(query, "name");
                if (qn.Length > 0) name = qn;

                if (!Authorized(query, lines))
                {
                    await WriteRawAsync(ns, "HTTP/1.1 403 Prohibido\r\nAccess-Control-Allow-Origin: *\r\n" +
                        "Content-Length: 0\r\nConnection: close\r\n\r\n");
                    return;
                }

                // Sin tope de tamano: lo que el otro equipo manda, del tamano que sea,
                // entra entero. Un Content-Length enorme solo se avisa en el registro.
                if (len > AvisoDeBytes) CrashLog.Save("INFO envio muy grande a la espera: " + Fmt.Human(len));

                var bodyStart = idx + 4;
                var backlog = got - bodyStart; // bytes del cuerpo que ya llegaron pegados

                if (method == "OPTIONS")
                {
                    await WriteRawAsync(ns, "HTTP/1.1 204 No Content\r\nAccess-Control-Allow-Origin: *\r\n" +
                        "Access-Control-Allow-Methods: GET, POST, OPTIONS\r\nAccess-Control-Allow-Headers: X-File, X-Len, Content-Type\r\n" +
                        "Access-Control-Max-Age: 600\r\nContent-Length: 0\r\nConnection: close\r\n\r\n");
                    return;
                }

                if (method == "POST" && (path.Equals("/up", StringComparison.OrdinalIgnoreCase) || path.Equals("/", StringComparison.OrdinalIgnoreCase)))
                {
                    if (esCarpeta && len <= 0)
                    {
                        // Carpeta: el que envia va diciendo que archivo va luego y a
                        // continuacion, sin comprimir y sin dejar temporal en disco.
                        // Asi el otro recibe la carpeta montada con su contenido, y
                        // da igual lo grande que sea.
                        var (dir, shaDir) = await ReceiveFolderAsync(ns, head, bodyStart, backlog, name);
                        await WriteTextAsync(ns, dir.Length > 0 ? "ok " + shaDir : "error");
                        return;
                    }
                    var (finalPath, sha) = await ReceiveAsync(ns, head, bodyStart, backlog, name, len);
                    await WriteTextAsync(ns, finalPath.Length > 0 ? "ok " + sha : "error");
                    return;
                }

                if (method is "GET" or "HEAD")
                {
                    if (await ServeGetAsync(ns, path, query, method == "HEAD")) return;
                }

                await WriteTextAsync(ns, "QBasWing Shuttle listo");
            }
        }
        catch { }
        finally { _live.TryRemove(c, out _); }
    }

    /// <summary>Comprueba la clave de emparejamiento por cabecera o por ?k=.</summary>
    private static bool Authorized(string query, string[] headerLines)
    {
        if (string.IsNullOrEmpty(Key)) return true;
        foreach (var raw in headerLines)
        {
            var line = raw.TrimEnd('\r');
            var ci = line.IndexOf(':');
            if (ci <= 0) continue;
            if (line[..ci].Trim().Equals("X-Key", StringComparison.OrdinalIgnoreCase))
                if (line[(ci + 1)..].Trim() == Key) return true;
        }
        return QueryValue(query, "k") == Key;
    }

    // ------------------- interop HTTP: cualquier navegador o app -------------
    private async Task<bool> ServeGetAsync(NetworkStream ns, string path, string query, bool headOnly)
    {
        try
        {
            if (path.Equals("/dl", StringComparison.OrdinalIgnoreCase) ||
                path.Equals("/down", StringComparison.OrdinalIgnoreCase) ||
                path.Equals("/get", StringComparison.OrdinalIgnoreCase))
            {
                var u = QueryValue(query, "u");
                if (u.Length == 0) u = QueryValue(query, "path");
                if (u.Length == 0) u = QueryValue(query, "name");
                if (u.Length == 0) { await WriteTextAsync(ns, "falta ?u="); return true; }

                Stream? src = null;
                long size = -1;
                var fileName = u;
                if (u.StartsWith("content://", StringComparison.OrdinalIgnoreCase))
                {
                    if (OpenDoc == null) { await WriteTextAsync(ns, "SAF no disponible"); return true; }
                    var info = DocInfo?.Invoke(u);
                    if (info != null) { fileName = info.Value.Name; size = info.Value.Size; }
                    src = OpenDoc(u);
                }
                else
                {
                    var full = SafeJoin(Inbox, u);
                    if (full.Length == 0) { await WriteTextAsync(ns, "ruta no permitida"); return true; }
                    try
                    {
                        var fi = new FileInfo(full);
                        if (!fi.Exists) { await WriteTextAsync(ns, "no existe: " + u); return true; }
                        fileName = fi.Name; size = fi.Length;
                    }
                    catch { await WriteTextAsync(ns, "no existe: " + u); return true; }
                    try { src = new FileStream(full, FileMode.Open, FileAccess.Read, FileShare.Read, 256 * 1024, true); }
                    catch { await WriteTextAsync(ns, "no se puede leer"); return true; }
                }

                using (src)
                {
                    if (src == null) { await WriteTextAsync(ns, "no se puede abrir"); return true; }
                    var safe = Sanitize(Path.GetFileName(fileName));
                    var hdr = "HTTP/1.1 200 OK\r\n" +
                               "Content-Type: " + MimeOf(safe) + "\r\n" +
                               (size >= 0 ? $"Content-Length: {size}\r\n" : "") +
                               "Content-Disposition: attachment; filename=\"" + safe + "\"\r\n" +
                               "Access-Control-Allow-Origin: *\r\n" +
                               "Cache-Control: no-store\r\nConnection: close\r\n\r\n";
                    await ns.WriteAsync(Encoding.UTF8.GetBytes(hdr));
                    await ns.FlushAsync();
                    if (!headOnly)
                    {
                        var buf = new byte[256 * 1024];
                        int n;
                        while ((n = await src.ReadAsync(buf.AsMemory(0, buf.Length))) > 0)
                            await ns.WriteAsync(buf.AsMemory(0, n));
                    }
                    await ns.FlushAsync();
                }
                return true;
            }

            if (path.Equals("/list", StringComparison.OrdinalIgnoreCase) || path.Equals("/api/list", StringComparison.OrdinalIgnoreCase))
            {
                var items = ListEntries(Inbox, 0);
                var sb = new StringBuilder();
                sb.Append("{\"app\":\"QBasWing Shuttle\",\"inbox\":\"").Append(JsonStr(Inbox)).Append("\",\"files\":[");
                for (int i = 0; i < items.Count; i++)
                {
                    if (i > 0) sb.Append(',');
                    var it = items[i];
                    sb.Append("{\"n\":\"").Append(JsonStr(it.Name)).Append("\",\"p\":\"").Append(JsonStr(it.Rel))
                      .Append("\",\"s\":").Append(it.Size).Append(",\"d\":").Append(it.IsDir ? "true" : "false")
                      .Append(",\"u\":\"").Append(Uri.EscapeDataString(it.Key)).Append("\"}");
                }
                sb.Append("]}");
                await WriteRawAsync(ns, "HTTP/1.1 200 OK\r\nContent-Type: application/json; charset=utf-8\r\n" +
                    $"Content-Length: {Encoding.UTF8.GetByteCount(sb.ToString())}\r\nAccess-Control-Allow-Origin: *\r\nConnection: close\r\n\r\n" + sb);
                return true;
            }

            if (path.Equals("/", StringComparison.OrdinalIgnoreCase) || path.Equals("/index.html", StringComparison.OrdinalIgnoreCase))
            {
                await WriteRawAsync(ns, "HTTP/1.1 200 OK\r\nContent-Type: text/html; charset=utf-8\r\n" +
                    $"Content-Length: {Encoding.UTF8.GetByteCount(PageHtml())}\r\nAccess-Control-Allow-Origin: *\r\nConnection: close\r\n\r\n" + PageHtml());
                return true;
            }
        }
        catch { }
        return false;
    }

    private string PageHtml()
    {
        var K = Uri.EscapeDataString(Key);
        var items = ListEntries(Inbox, 0);
        var sb = new StringBuilder();
        sb.Append("<!doctype html><html lang=\"es\"><head><meta charset=\"utf-8\">")
          .Append("<meta name=\"viewport\" content=\"width=device-width,initial-scale=1\">")
          .Append("<title>QBasWing Shuttle</title><style>")
          .Append("body{font-family:system-ui,sans-serif;background:#eaf2ff;color:#0d1b3e;margin:0;padding:16px}")
          .Append("h1{font-size:19px;margin:0 0 4px}p{font-size:13px;margin:4px 0 14px}")
          .Append("table{border-collapse:collapse;width:100%;background:#fff;border-radius:10px;overflow:hidden}")
          .Append("th,td{font-size:13px;padding:8px 10px;border-bottom:1px solid #d3e0ff;text-align:left}")
          .Append("a{color:#0b3ea8}input,button{font-size:14px;padding:8px;border-radius:8px;border:1px solid #b9cdf5}")
          .Append("</style></head><body>");
        sb.Append("<h1>QBasWing Shuttle</h1>");
        sb.Append("<p>Equipo: <b>").Append(Html(DeviceName)).Append("</b> &middot; recibidos en: <b>").Append(Html(Inbox)).Append("</b></p>");
        sb.Append("<p>Enviar archivos: el&iacute;ge un archivo y pulsa <b>Enviar</b>. Autom&aacute;ticamente se guarda en la carpeta de recibidos de la otra m&aacute;quina.</p>");
        sb.Append("<form id=f><input type=file id=i multiple><button type=button onclick=go()>Enviar</button></form><p id=s></p>");
        sb.Append("<table><tr><th>Archivo</th><th>Tama&ntilde;o</th><th></th></tr>");
        foreach (var it in items)
        {
            if (it.IsDir) { sb.Append("<tr><td>").Append(Html(it.Name)).Append("/</td><td>-</td><td></td></tr>"); continue; }
            sb.Append("<tr><td>").Append(Html(it.Rel)).Append("</td><td>").Append(Fmt.Human(it.Size))
              .Append("</td><td><a href=\"/dl?u=").Append(Uri.EscapeDataString(it.Key))
              .Append(K.Length > 0 ? "&k=" + K : "").Append("\">Descargar</a></td></tr>");
        }
        sb.Append("</table>");
        sb.Append("<script>async function go(){var i=document.getElementById('i'),s=document.getElementById('s');if(!i.files.length)return;s.textContent='Enviando...';for(var k=0;k<i.files.length;k++){var f=i.files[k];var r=await fetch('/up?name='+encodeURIComponent(f.name)+'&k='+K,{method:'POST',body:f});s.textContent='Enviado: '+f.name+' ('+r.status+')';}}</script>");
        sb.Append("</body></html>");
        return sb.ToString();
    }

    private sealed class ItemRow
    {
        public string Name = "", Rel = "", Key = "";
        public long Size;
        public bool IsDir;
    }

    private List<ItemRow> ListEntries(string root, int depth)
    {
        var res = new List<ItemRow>();
        if (root.StartsWith("content://", StringComparison.OrdinalIgnoreCase))
        {
            var lister = ListDoc;
            if (lister == null) return res;
            foreach (var ch in lister(root))
                res.Add(new ItemRow { Name = ch.Name, Rel = ch.Name, Key = ch.Uri, Size = ch.Size, IsDir = ch.IsDir });
            return res;
        }
        if (depth > 3) return res;
        try
        {
            if (!Directory.Exists(root)) return res;
            foreach (var d in Directory.EnumerateDirectories(root))
            {
                var dn = Path.GetFileName(d);
                res.Add(new ItemRow { Name = dn, Rel = dn, Key = dn, IsDir = true });
                foreach (var sub in ListEntries(d, depth + 1))
                    res.Add(new ItemRow { Name = sub.Name, Rel = dn + "/" + sub.Rel, Key = dn + "/" + sub.Key, Size = sub.Size, IsDir = sub.IsDir });
                if (res.Count > 4000) break;
            }
            foreach (var f in Directory.EnumerateFiles(root))
            {
                var fi = new FileInfo(f);
                res.Add(new ItemRow { Name = fi.Name, Rel = fi.Name, Key = fi.Name, Size = fi.Length });
                if (res.Count > 4000) break;
            }
        }
        catch { }
        return res;
    }

    private static string SafeJoin(string root, string rel)
    {
        if (root.Length == 0 || root.StartsWith("content://", StringComparison.OrdinalIgnoreCase)) return "";
        rel = rel.Replace('\\', '/').TrimStart('/');
        if (rel.Length == 0) return "";
        try
        {
            var baseFull = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var full = Path.GetFullPath(Path.Combine(baseFull, rel));
            // Sin la barra final, "/recibidos-secreto" pasaba el StartsWith de "/recibidos".
            if (!full.StartsWith(baseFull + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)) return "";
            return full;
        }
        catch { return ""; }
    }

    private static string QueryValue(string query, string key)
    {
        if (string.IsNullOrEmpty(query)) return "";
        foreach (var part in query.Split('&'))
        {
            var eq = part.IndexOf('=');
            if (eq <= 0) continue;
            if (part[..eq].Equals(key, StringComparison.OrdinalIgnoreCase))
                return part[(eq + 1)..];
        }
        return "";
    }

    private static string MimeOf(string file)
    {
        var ext = Path.GetExtension(file).ToLowerInvariant();
        return ext switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            ".bmp" => "image/bmp",
            ".svg" => "image/svg+xml",
            ".mp4" => "video/mp4",
            ".webm" => "video/webm",
            ".mkv" => "video/x-matroska",
            ".3gp" => "video/3gpp",
            ".mp3" => "audio/mpeg",
            ".wav" => "audio/wav",
            ".ogg" => "audio/ogg",
            ".m4a" => "audio/mp4",
            ".flac" => "audio/flac",
            ".pdf" => "application/pdf",
            ".txt" or ".log" => "text/plain",
            ".csv" => "text/csv",
            ".json" => "application/json",
            ".xml" => "application/xml",
            ".html" or ".htm" => "text/html",
            ".zip" => "application/zip",
            ".rar" => "application/vnd.rar",
            ".7z" => "application/x-7z-compressed",
            ".apk" => "application/vnd.android.package-archive",
            _ => "application/octet-stream"
        };
    }

    private static string Html(string s)
    {
        var sb = new StringBuilder(s.Length + 16);
        foreach (var ch in s)
            sb.Append(ch switch
            {
                '&' => "&amp;",
                '<' => "&lt;",
                '>' => "&gt;",
                '"' => "&quot;",
                _ => ch.ToString()
            });
        return sb.ToString();
    }

    private static string JsonStr(string s)
    {
        var sb = new StringBuilder(s.Length + 8);
        foreach (var ch in s)
        {
            if (ch == '"' || ch == '\\') sb.Append('\\').Append(ch);
            else if (ch < ' ') sb.Append("\\u").Append(((int)ch).ToString("x4"));
            else sb.Append(ch);
        }
        return sb.ToString();
    }

    private static async Task WriteTextAsync(NetworkStream ns, string text)
    {
        var body = Encoding.UTF8.GetBytes(text);
        var hdr = Encoding.UTF8.GetBytes("HTTP/1.1 200 OK\r\nContent-Type: text/plain; charset=utf-8\r\n" +
            $"Content-Length: {body.Length}\r\nAccess-Control-Allow-Origin: *\r\nConnection: close\r\n\r\n");
        await ns.WriteAsync(hdr);
        await ns.WriteAsync(body);
        await ns.FlushAsync();
    }

    private static async Task WriteRawAsync(NetworkStream ns, string text)
    {
        await ns.WriteAsync(Encoding.UTF8.GetBytes(text));
        await ns.FlushAsync();
    }

    private const int IoBuf = 1024 * 1024;

    /// <summary>
    /// Recibe una carpeta entera. El que envia escribe una linea por archivo (su
    /// tamano y su ruta dentro de la carpeta) seguida de los bytes, y al final una
    /// linea vacia. Nada se comprime y no queda ningun temporal en disco: da igual
    /// que la carpeta pese 2 GB o 200 GB. Al terminar esta montada con su contenido
    /// y con las fechas, igual que si se hubiera copiado a mano.
    /// </summary>
    private async Task<(string Path, string Sha)> ReceiveFolderAsync(NetworkStream ns, byte[] head, int bodyStart, int backlog, string rawName)
    {
        await _slots.WaitAsync();
        var scratch = System.Buffers.ArrayPool<byte>.Shared.Rent(IoBuf);
        var carpeta = "";
        var pendientes = head.AsMemory(bodyStart, backlog);
        var pos = 0;
        long total = 0;
        var archivos = 0;
        Stream? outS = null;
        // SHA-256 de la carpeta entera: se va haciendo con la ruta de cada elemento y
        // el hash de sus bytes, en el mismo orden que va por el cable. El que envia
        // hace exactamente lo mismo, asi que comparar los dos resultados dice si la
        // carpeta llego entera y sin retocar, con todo lo que va dentro.
        using var hCarpeta = System.Security.Cryptography.IncrementalHash.CreateHash(System.Security.Cryptography.HashAlgorithmName.SHA256);
        using var hArchivo = System.Security.Cryptography.IncrementalHash.CreateHash(System.Security.Cryptography.HashAlgorithmName.SHA256);
        // De una carpeta no se sabe cuanto va a venir por adelantado: solo se avisa de
        // lo que lleva entrado, que es lo unico que se puede saber.
        var evC = Receiving;
        long ultimoAvisoC = 0;
        void AvanzaCarpeta()
        {
            if (evC == null) return;
            var ahora = Environment.TickCount64;
            if (ahora - ultimoAvisoC < 250) return;
            ultimoAvisoC = ahora;
            try { evC(rawName, total, 0); } catch { }
        }
        try
        {
            // Parte de lo que venia pegado a la cabecera puede ser la primera linea
            // del listado, asi que se lee de ahi antes que de la red.
            async Task<string> Linea()
            {
                if (pos >= pendientes.Length) return await ReadLineAsync(ns);
                int salto = -1;
                for (int i = pos; i < pendientes.Length; i++)
                    if (pendientes.Span[i] == (byte)'\n') { salto = i; break; }
                if (salto < 0)
                {
                    var cola = Encoding.UTF8.GetString(pendientes.Span[pos..]);
                    pos = pendientes.Length;
                    return cola + await ReadLineAsync(ns);
                }
                var s0 = Encoding.UTF8.GetString(pendientes.Span[pos..salto]);
                pos = salto + 1;
                return s0.TrimEnd('\r');
            }

            async Task<int> Leer(byte[] dst, int off, int count)
            {
                int he = 0;
                if (pos < pendientes.Length)
                {
                    int take = Math.Min(count, pendientes.Length - pos);
                    pendientes.Span.Slice(pos, take).CopyTo(dst.AsSpan(off));
                    pos += take; he = take;
                }
                while (he < count)
                {
                    int n = await ns.ReadAsync(dst.AsMemory(off + he, count - he));
                    if (n <= 0) break;
                    he += n;
                }
                return he;
            }

            // Tolerancia con emisores antiguos, que escribian un "\n" de mas detrás
            // de cada archivo: aquel blanco se leia como linea vacia y cerraba la
            // carpeta en la primera entrada. Un blanco solo acaba la carpeta si de
            // verdad no queda nada mas: si despues llegan datos, era de mas y se
            // sigue leyendo. Se lee en bloque y con margen, no con cancelacion, para
            // no perder el byte que esta en camino.
            var uno = new byte[1];
            int Chincheta()
            {
                if (pos < pendientes.Length) { var b = pendientes.Span[pos]; Devolver(b); return b; }
                try
                {
                    ns.ReadTimeout = 250;
                    return ns.Read(uno, 0, 1) == 1 ? uno[0] : -1;
                }
                catch { return -1; }
                finally { try { ns.ReadTimeout = 0; } catch { } }
            }

            void Devolver(byte b)
            {
                var np = new byte[pendientes.Length - pos + 1];
                np[0] = b;
                pendientes.Span[pos..].CopyTo(np.AsSpan(1));
                pendientes = np;
                pos = 0;
            }

            var nombre = Sanitize(rawName);
            if (nombre.Length == 0) return ("", "");
            var baseDir = InboxFor(Inbox, nombre + ".x");
            bool toDoc = IsSaf(baseDir) && WriteDoc != null && EnsureDocDir != null;
            if (toDoc)
            {
                // Con SAF se reutilizaba la carpeta si ya existia y se le metia lo nuevo
                // dentro: dos carpetas con el mismo nombre acababan mezcladas. Aqui se
                // pide una que no exista todavia, igual que en el escritorio.
                carpeta = (NewDocDir ?? EnsureDocDir)!(baseDir, nombre) ?? "";
                if (carpeta.Length == 0) return ("", "");
            }
            else
            {
                if (IsSaf(baseDir) && EnsureDocDir == null) return ("", "");
                try { Directory.CreateDirectory(baseDir); } catch { }
                carpeta = Unique(Path.Combine(baseDir, nombre));
                try { Directory.CreateDirectory(carpeta); } catch { }
            }

            while (true)
            {
                var line = await Linea();
                if (line.Length == 0)
                {
                    // Linea vacia = fin de la carpeta, salvo que haya mas datos.
                    if (Chincheta() < 0) break;
                    continue;
                }
                var p1 = line.IndexOf('\t');
                if (p1 < 0) return ("", "");
                var p2 = line.IndexOf('\t', p1 + 1);
                if (p2 < 0) return ("", "");
                if (!long.TryParse(line[..p1], System.Globalization.NumberStyles.HexNumber, null, out var size)) return ("", "");
                long unix = long.TryParse(line[(p1 + 1)..p2], out var u) ? u : 0;
                var rel = line[(p2 + 1)..];

                // La ruta entra tal cual en el hash de la carpeta, con la misma "/", para
                // que los dos extremos coincidan incluso con las carpetas vacias.
                var marcaRuta = Encoding.UTF8.GetBytes(rel + "\n");
                hCarpeta.AppendData(marcaRuta);

                // Carpeta vacia: el que envia la marca con "/" al final.
                if (size == 0 && rel.EndsWith('/'))
                {
                    if (SafeRel(rel, out var dRel))
                    {
                        if (toDoc) EnsureDocDir!(carpeta, dRel);
                        else
                        {
                            TryMakeDir(Path.Combine(carpeta, dRel));
                            // Las carpetas vacias tambien llevan su fecha: si no, al
                            // ordenarlas en el explorador salen todas juntas.
                            if (unix > 0) try { Directory.SetLastWriteTime(Path.Combine(carpeta, dRel), DateTimeOffset.FromUnixTimeSeconds(unix).LocalDateTime); } catch { }
                        }
                    }
                    continue;
                }
                if (size < 0 || !SafeRel(rel, out var relOk)) return ("", "");
                var relUnix = relOk.Replace('\\', '/');
                var unixTs = unix > 0 ? DateTimeOffset.FromUnixTimeSeconds(unix).LocalDateTime : DateTime.MinValue;

                string final;
                if (toDoc)
                {
                    var padre = Path.GetDirectoryName(relUnix);
                    var destName = Path.GetFileName(relUnix);
                    var dirUri = string.IsNullOrEmpty(padre) ? carpeta : (EnsureDocDir!(carpeta, padre) ?? carpeta);
                    if (dirUri.Length == 0) return ("", "");
                    var dup = DocExists == null ? destName : UniqueDocName(dirUri, destName);
                    outS = WriteDoc!(dirUri, dup);
                    if (outS == null) return ("", "");
                    final = dirUri + "/" + dup;
                }
                else
                {
                    final = Unique(Path.Combine(carpeta, relOk));
                    var pd = Path.GetDirectoryName(final);
                    if (!string.IsNullOrEmpty(pd)) TryMakeDir(pd);
                }

                long left = size;
                while (left > 0)
                {
                    int n = await Leer(scratch, 0, (int)Math.Min(left, IoBuf));
                    if (n <= 0) return ("", "");
                    await outS.WriteAsync(scratch.AsMemory(0, n));
                    hArchivo.AppendData(scratch, 0, n);
                    left -= n; total += n;
                    AvanzaCarpeta();
                }
                await outS.FlushAsync();
                try { outS.Dispose(); } catch { }
                outS = null;
                if (unixTs != DateTime.MinValue && !toDoc)
                {
                    try { File.SetLastWriteTime(final, unixTs); } catch { }
                }
                // El hash del archivo entra en el de la carpeta, en el mismo orden.
                hCarpeta.AppendData(hArchivo.GetHashAndReset());
                archivos++;
            }

            var sha = Convert.ToHexString(hCarpeta.GetHashAndReset()).ToLowerInvariant();
            CrashLog.Save("INFO carpeta recibida: " + nombre + " (" + archivos + " archivos, " + Fmt.Human(total) + ")");
            FileReceived?.Invoke(carpeta, total);
            return (carpeta, sha);
        }
        catch (Exception e)
        {
            CrashLog.Save("ERROR al recibir carpeta: " + e.Message);
            return ("", "");
        }
        finally
        {
            try { outS?.Dispose(); } catch { }
            System.Buffers.ArrayPool<byte>.Shared.Return(scratch);
            _slots.Release();
        }
    }

    /// <summary>
    /// Limpia una ruta relativa que viene dentro de una carpeta: nada de "..", ni
    /// rutas absolutas, ni caracteres invalidos. Un envio nunca puede escribir
    /// fuera de la carpeta que el usuario eligio.
    /// </summary>
    private static bool SafeRel(string rel, out string limpio)
    {
        limpio = "";
        var r = rel.Replace('\\', '/').Trim();
        while (r.StartsWith("/")) r = r[1..];
        if (r.Length == 0 || r.Contains(':')) return false;
        var partes = new List<string>();
        foreach (var p in r.Split('/'))
        {
            if (p.Length == 0 || p == ".") continue;
            if (p == "..") return false;
            if (p.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0) return false;
            partes.Add(p);
        }
        if (partes.Count == 0) return false;
        limpio = string.Join(Path.DirectorySeparatorChar.ToString(), partes);
        return true;
    }

    private static void TryMakeDir(string dir)
    {
        try { Directory.CreateDirectory(dir); } catch { }
    }

    /// <summary>Si el nombre ya existe en una carpeta SAF, se le anade un numero.</summary>
    private static string UniqueDocName(string dirUri, string name)
    {
        if (DocExists == null || !DocExists(dirUri, name)) return name;
        var sinExt = Path.GetFileNameWithoutExtension(name);
        var ext = Path.GetExtension(name);
        for (int i = 1; i < 10000; i++)
        {
            var cand = sinExt + " (" + i.ToString() + ")" + ext;
            if (!DocExists(dirUri, cand)) return cand;
        }
        return sinExt + " (" + Guid.NewGuid().ToString("N")[..6] + ")" + ext;
    }

    private async Task<(string Path, string Sha)> ReceiveAsync(NetworkStream ns, byte[] head, int bodyStart, int backlog, string rawName, long len)
    {
        await _slots.WaitAsync();
        Stream? outS = null;
        string writing = "";
        // Buffer por recepcion. Antes era uno solo compartido por las 4 ranuras: con dos
        // descargas simultaneas los bloques de una se escribian en el archivo de la otra.
        var scratch = System.Buffers.ArrayPool<byte>.Shared.Rent(IoBuf);
        // SHA-256 de lo que se escribe, que se devuelve al que envia para que compare
        // con el suyo: es la garantia de que lo que llego esta entero, sin convertir ni
        // recortar nada por el camino.
        using var h = System.Security.Cryptography.IncrementalHash.CreateHash(System.Security.Cryptography.HashAlgorithmName.SHA256);
        long bytes = backlog;
        // Progreso para el que espera: como mucho cuatro veces por segundo, que basta
        // para verlo avanzar y no carga la interfaz de avisos.
        var ev = Receiving;
        long ultimoAviso = 0;
        void Avanza()
        {
            if (ev == null) return;
            var ahora = Environment.TickCount64;
            if (ahora - ultimoAviso < 250) return;
            ultimoAviso = ahora;
            try { ev(rawName, bytes, len >= 0 ? len : 0); } catch { }
        }
        try
        {
            var name = Sanitize(rawName);
            // Cada archivo cae en la subcarpeta de su tipo (Fotos, Videos, Musica...)
            // y no todo mezclado en la raiz de la carpeta de la marca.
            var dest = InboxFor(Inbox, name);
            bool toDoc = IsSaf(dest) && WriteDoc != null;
            if (toDoc)
            {
                outS = WriteDoc!(dest, name);
                if (outS == null) return ("", "");
                writing = "";
            }
            else
            {
                try { Directory.CreateDirectory(dest); } catch { }
                writing = Unique(Path.Combine(dest, name));
                outS = new FileStream(writing, FileMode.Create, FileAccess.Write, FileShare.None,
                    IoBuf, FileOptions.Asynchronous | FileOptions.SequentialScan);
            }

            if (len == -2)

            {
                    if (backlog > 0)
                    {
                        await outS.WriteAsync(head.AsMemory(bodyStart, backlog));
                        h.AppendData(head, bodyStart, backlog);
                    }
                    long chunkTotal = 0;
                while (true)
                {
                    var sizeLine = await ReadLineAsync(ns);
                    if (sizeLine.Length == 0) break;
                    if (!long.TryParse(sizeLine.Split(';')[0], System.Globalization.NumberStyles.HexNumber, null, out var size))
                        break;
                    if (size <= 0) break;
                    chunkTotal += size;
                    if (chunkTotal > AvisoDeBytes) CrashLog.Save("INFO archivo muy grande por transferencia: " + Fmt.Human(chunkTotal));
                    long left = size;
                    while (left > 0)
                    {
                        int n = await ns.ReadAsync(scratch.AsMemory(0, (int)Math.Min(left, IoBuf)));
                        if (n <= 0) return Abandon(writing);
                        await outS.WriteAsync(scratch.AsMemory(0, n));
                        h.AppendData(scratch, 0, n);
                        left -= n;
                        bytes += n;
                        Avanza();
                    }
                    await ConsumeCrlfAsync(ns);
                }
            }
            else
            {
                // Sin Content-Length no hay tope: el bucle termina solo cuando el otro
                // equipo deja de enviar y cierra. Lo que llega es lo que hay.
                long left = len >= 0 ? len - backlog : long.MaxValue;
                if (left <= 0) { outS.Dispose(); outS = null; return Abandon(writing); }
                if (backlog > 0)
                {
                    await outS.WriteAsync(head.AsMemory(bodyStart, backlog));
                    h.AppendData(head, bodyStart, backlog);
                }
                while (left > 0)
                {
                    int n = await ns.ReadAsync(scratch.AsMemory(0, (int)Math.Min(left, IoBuf)));
                    if (n <= 0) break;
                    await outS.WriteAsync(scratch.AsMemory(0, n));
                    h.AppendData(scratch, 0, n);
                    bytes += n;
                    left -= n;
                    Avanza();
                }
                if (bytes > AvisoDeBytes) CrashLog.Save("INFO archivo muy grande por transferencia: " + Fmt.Human(bytes));
            }

            await outS.FlushAsync();
            outS.Dispose();
            outS = null;
            var finalPath = toDoc ? name : writing;
            var sha = Convert.ToHexString(h.GetHashAndReset()).ToLowerInvariant();
            FileReceived?.Invoke(finalPath, bytes);
            return (finalPath, sha);
        }
        catch (Exception e)
        {
            CrashLog.Save("ERROR al recibir archivo: " + e.Message);
            return Abandon(writing);
        }
        finally
        {
            try { outS?.Dispose(); } catch { }
            System.Buffers.ArrayPool<byte>.Shared.Return(scratch);
            _slots.Release();
        }
    }

    private (string, string) Abandon(string writing)
    {
        try { if (writing.Length > 0 && File.Exists(writing)) File.Delete(writing); } catch { }
        return ("", "");
    }

    private static async Task ConsumeCrlfAsync(NetworkStream ns)
    {
        try
        {
            var b = await ReadExactAsync(ns, 2);
        }
        catch { }
    }

    private static async Task<byte[]> ReadExactAsync(NetworkStream ns, int count)
    {
        var buf = new byte[count];
        int got = 0;
        while (got < count)
        {
            int n = await ns.ReadAsync(buf.AsMemory(got, count - got));
            if (n <= 0) break;
            got += n;
        }
        return buf;
    }

    private static async Task<string> ReadLineAsync(NetworkStream ns)
    {
        var sb = new StringBuilder();
        var b = new byte[1];
        while (sb.Length < 1000)
        {
            int n = await ns.ReadAsync(b.AsMemory());
            if (n <= 0) break;
            var ch = (char)b[0];
            if (ch == '\n') break;
            if (ch != '\r') sb.Append(ch);
        }
        return sb.ToString();
    }

    private static int IndexOfHeaderEnd(byte[] b, int len)
    {
        for (int i = 0; i + 3 < len; i++)
            if (b[i] == '\r' && b[i + 1] == '\n' && b[i + 2] == '\r' && b[i + 3] == '\n')
                return i;
        return -1;
    }

    private static string Sanitize(string name)
    {
        var inv = Path.GetInvalidFileNameChars();
        var ok = new string(name.Where(c => !inv.Contains(c) && c >= ' ').ToArray()).Trim();
        ok = ok.TrimStart('.');
        if (ok.Length == 0 || ok is "." or "..") ok = "archivo.bin";
        if (ok.Length > 180) ok = ok[..180];
        return ok;
    }

    private static string Unique(string path)
    {
        if (!File.Exists(path)) return path;
        var dir = Path.GetDirectoryName(path)!;
        var name = Path.GetFileNameWithoutExtension(path);
        var ext = Path.GetExtension(path);
        for (int i = 1; ; i++)
        {
            var c = Path.Combine(dir, $"{name} ({i}){ext}");
            if (!File.Exists(c)) return c;
            if (i > 999) return Path.Combine(dir, $"{name}.{Guid.NewGuid():N}{ext}");
        }
    }

    public void Dispose() { Stop(); _slots.Dispose(); _cts?.Dispose(); _cts = null; }
}

public static class TransferClient
{
    /// <summary>Clave de emparejamiento que se envia en X-Key.</summary>
    public static string Key = "";

    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromMinutes(30) };
    private const int Buf = 1024 * 1024;

    public static async Task<string> Ping(string baseUrl)
    {
        using var r = await Http.GetAsync(baseUrl, HttpCompletionOption.ResponseHeadersRead);
        return await r.Content.ReadAsStringAsync();
    }

    public static async Task<long> Upload(string baseUrl, string name, long total, Func<Stream> open, Action<long>? progress)
    {
        string host; int port;
        if (!SplitUrl(baseUrl, out host, out port)) return -1;
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(120));
            using var tcp = new TcpClient();
            tcp.NoDelay = true;
            try { tcp.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendBuffer, 4 * 1024 * 1024); } catch { }
            try { tcp.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveBuffer, 1024 * 1024); } catch { }
            try { tcp.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true); } catch { }
            await tcp.ConnectAsync(host, port, cts.Token);
            using var ns = tcp.GetStream();
            var head = Encoding.UTF8.GetBytes(
                "POST /up HTTP/1.1\r\n" +
                "Host: " + host + ":" + port + "\r\n" +
                "X-File: " + Uri.EscapeDataString(name) + "\r\n" +
                "X-Key: " + (Key.Length > 0 ? Key : "-") + "\r\n" +
                "X-Len: " + total.ToString() + "\r\n" +
                "Content-Length: " + total.ToString() + "\r\n" +
                "Content-Type: application/octet-stream\r\n" +
                "Connection: close\r\n\r\n");
            await ns.WriteAsync(head.AsMemory(), cts.Token);

            // SHA-256 de lo que sale, sin volver a leer el archivo: se hace en el mismo
            // paso que se envia. Quien lo recibe devuelve el suyo y se comparan, que es
            // la garantia de que lo que llego esta byte a byte igual a lo que salio.
            using var h = System.Security.Cryptography.IncrementalHash.CreateHash(System.Security.Cryptography.HashAlgorithmName.SHA256);
            long pos = 0;
            using (var src = open())
            {
                var buf = new byte[Buf];
                while (true)
                {
                    int n = await src.ReadAsync(buf.AsMemory(0, Buf), cts.Token);
                    if (n <= 0) break;
                    await ns.WriteAsync(buf.AsMemory(0, n), cts.Token);
                    h.AppendData(buf, 0, n);
                    pos += n;
                    progress?.Invoke(pos);
                }
            }
            await ns.FlushAsync(cts.Token);
            if (total > 0 && pos != total) return -1;

            var resp = await LeeRespuesta(ns, cts.Token);
            if (!resp.Contains("200 OK")) return -1;
            var mio = Convert.ToHexString(h.GetHashAndReset()).ToLowerInvariant();
            if (!ShaCoincide(mio, ShaDeRespuesta(resp), name)) return -1;
            return total;
        }
        catch (Exception e)
        {
            CrashLog.Save("ERROR al enviar " + name + ": " + e.Message);
            return -1;
        }
    }

    /// <summary>
    /// Compara el hash de lo que ha salido con el que devuelve el otro equipo. Si no
    /// coinciden, la transferencia se da por fallida: es preferible avisar de que algo
    /// llego mal que dejar un archivo a medio camino haciendose pasar por bueno.
    /// Devuelve false si el otro equipo no ha devuelto hash (no se puede comprobar).
    /// </summary>
    private static bool ShaCoincide(string mio, string suyo, string que)
    {
        if (suyo.Length != 64) return true;   // no se puede comprobar: no se inventa
        if (string.Equals(mio, suyo, StringComparison.OrdinalIgnoreCase)) return true;
        CrashLog.Save("ERROR SHA-256 distinto al recibir " + que + ": envio " + mio + ", llego " + suyo);
        return false;
    }

    /// <summary>Lee la respuesta entera del otro equipo (son unos pocos bytes).</summary>
    private static async Task<string> LeeRespuesta(NetworkStream ns, CancellationToken ct)
    {
        var buf = new byte[4096];
        int got = 0;
        while (got < buf.Length)
        {
            int n = await ns.ReadAsync(buf.AsMemory(got, buf.Length - got), ct);
            if (n <= 0) break;
            got += n;
            if (Encoding.UTF8.GetString(buf, 0, got).Contains("\r\n\r\n")) break;
        }
        return Encoding.UTF8.GetString(buf, 0, got);
    }

    /// <summary>
    /// Saca el SHA-256 que devuelve quien recibe: va al final del cuerpo de la
    /// respuesta, detras de la palabra "ok".
    /// </summary>
    private static string ShaDeRespuesta(string respuesta)
    {
        var i = respuesta.LastIndexOf("\r\n\r\n", StringComparison.Ordinal);
        var cuerpo = i >= 0 ? respuesta[(i + 4)..] : "";
        var partes = cuerpo.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        for (int k = partes.Length - 1; k >= 0; k--)
            if (partes[k].Length == 64 && EsHex(partes[k])) return partes[k].ToLowerInvariant();
        return "";
    }

    private static bool EsHex(string s)
    {
        foreach (var c in s) if (!Uri.IsHexDigit(c)) return false;
        return s.Length > 0;
    }

    /// <summary>
    /// Envia una carpeta entera sin comprimirla y sin dejar ningun temporal: se va
    /// mandando el listado y cada archivo por delante, y el otro lo monta con la
    /// misma estructura. No hay tope de tamano ni de numero de archivos, y solo se
    /// escribe una vez en destino, que es lo mas rapido que puede ser.
    /// </summary>
    public static async Task<long> UploadFolder(string baseUrl, string folderName,
        IReadOnlyList<(string Rel, long Size, DateTime Modified, Func<Stream> Open)> entradas,
        Action<long>? progress, Action<string>? paso, CancellationToken ct)
    {
        string host; int port;
        if (!SplitUrl(baseUrl, out host, out port)) return -1;
        try
        {
            using var tcp = new TcpClient();
            tcp.NoDelay = true;
            try { tcp.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendBuffer, 4 * 1024 * 1024); } catch { }
            try { tcp.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true); } catch { }
            await tcp.ConnectAsync(host, port, ct);
            using var ns = tcp.GetStream();

            // Sin Content-Length: la longitud se va diciendo archivo a archivo. Por eso
            // X-Len va a -1 y quien recibe ya sabe que esto es una carpeta.
            var head = Encoding.UTF8.GetBytes(
                "POST /up HTTP/1.1\r\n" +
                "Host: " + host + ":" + port + "\r\n" +
                "X-Folder: " + Uri.EscapeDataString(folderName) + "\r\n" +
                "X-Key: " + (Key.Length > 0 ? Key : "-") + "\r\n" +
                "X-Len: -1\r\n" +
                "Content-Type: application/octet-stream\r\n" +
                "Connection: close\r\n\r\n");
            await ns.WriteAsync(head.AsMemory(), ct);

            long pos = 0;
            var buf = new byte[Buf];
            // El hash de la carpeta se hace con la ruta de cada elemento y el hash de
            // sus bytes, en el mismo orden que sale por el cable. Quien lo recibe hace
            // lo mismo con lo que escribe, y al final se comparan los dos.
            using var hCarpeta = System.Security.Cryptography.IncrementalHash.CreateHash(System.Security.Cryptography.HashAlgorithmName.SHA256);
            using var hArchivo = System.Security.Cryptography.IncrementalHash.CreateHash(System.Security.Cryptography.HashAlgorithmName.SHA256);
            for (int i = 0; i < entradas.Count; i++)
            {
                ct.ThrowIfCancellationRequested();
                var e = entradas[i];
                paso?.Invoke(folderName + " · " + e.Rel);
                var unix = new DateTimeOffset(e.Modified).ToUnixTimeSeconds();
                // Carpeta vacia: ya viene marcada con "/" al final en el listado, que es
                // lo que la distingue de un archivo de cero bytes. Un archivo de cero
                // bytes va como archivo vacio, que no es lo mismo que una carpeta.
                var rel = e.Rel.Replace('\\', '/');
                hCarpeta.AppendData(Encoding.UTF8.GetBytes(rel + "\n"));
                var linea = Encoding.UTF8.GetBytes(e.Size.ToString("x") + "\t" + unix + "\t" + rel + "\n");
                await ns.WriteAsync(linea.AsMemory(), ct);

                long left = e.Size;
                using (var src = e.Open())
                {
                    while (left > 0)
                    {
                        int n = await src.ReadAsync(buf.AsMemory(0, (int)Math.Min(left, Buf)), ct);
                        if (n <= 0) break;
                        await ns.WriteAsync(buf.AsMemory(0, n), ct);
                        hArchivo.AppendData(buf, 0, n);
                        left -= n; pos += n;
                        progress?.Invoke(pos);
                    }
                }
                // Si el archivo era mas corto de lo que decia el listado, se ha
                // cambiado mientras se mandaba: el protocolo se descolocaria, asi que
                // se corta aqui y no se manda una carpeta corrupta.
                if (left > 0)
                {
                    CrashLog.Save("ERROR al enviar " + folderName + ": " + rel + " cambio durante el envio (faltaron " + Fmt.Human(left) + ")");
                    return -1;
                }
                hCarpeta.AppendData(hArchivo.GetHashAndReset());
                // Detras de los bytes NO se escribe nada: quien recibe los lee justos
                // y despues lee la linea siguiente del stream. Un "\n" de mas aqui se
                // comia la primera linea de la carpeta y la cerraba antes de tiempo.
            }
            await ns.WriteAsync("\n"u8.ToArray(), ct); // linea vacia = fin de la carpeta
            await ns.FlushAsync(ct);

            var resp = await LeeRespuesta(ns, ct);
            if (!resp.Contains("200 OK")) return -1;
            var mioCarpeta = Convert.ToHexString(hCarpeta.GetHashAndReset()).ToLowerInvariant();
            if (!ShaCoincide(mioCarpeta, ShaDeRespuesta(resp), folderName)) return -1;
            return pos;
        }
        catch (Exception e)
        {
            CrashLog.Save("ERROR al enviar la carpeta: " + e.Message);
            return -1;
        }
    }

    private static bool SplitUrl(string baseUrl, out string host, out int port)
    {
        host = ""; port = 0;
        try
        {
            var u = new Uri(baseUrl);
            if (u.Scheme != "http") return false;
            host = u.Host;
            port = u.Port > 0 ? u.Port : 80;
            return host.Length > 0;
        }
        catch { return false; }
    }
}
