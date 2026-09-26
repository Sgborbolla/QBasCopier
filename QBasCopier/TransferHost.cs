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

    // Ganchos SAF (los define MainActivity en Android). Permiten servir listados y descargas
    // cuando la carpeta de recibidos es un árbol content:// y no una ruta del sistema de archivos.
    public static Func<string, Stream>? OpenDoc;
    public static Func<string, (string Name, long Size)>? DocInfo;
    public static Func<string, List<(string Uri, string Name, bool IsDir, long Size)>>? ListDoc;

    // Escribe directo dentro del árbol SAF (Android): una sola escritura, sin temporal.
    public static Func<string, string, Stream>? WriteDoc;

    public static string DeviceName = "";

    /// <summary>
    /// Clave de emparejamiento. Si esta vacia el servidor es abierto (util en una red
    /// domestica de confianza); si tiene valor, sin la clave correcta no se sube ni se
    /// descarga nada. Antes se mostraba en el QR y jamas se comprobaba.
    /// </summary>
    public static string Key = "";

    /// <summary>Tope de seguridad: nadie necesita un archivo de mas de 1 TB por HTTP.</summary>
    public const long MaxFileBytes = 1024L * 1024 * 1024 * 1024;

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
                foreach (var raw in lines.Skip(1))
                {
                    var line = raw.TrimEnd('\r');
                    var ci = line.IndexOf(':');
                    if (ci <= 0) continue;
                    var k = line.Substring(0, ci).Trim();
                    var v = line.Substring(ci + 1).Trim();
                    if (k.Equals("X-File", StringComparison.OrdinalIgnoreCase))
                        name = Uri.UnescapeDataString(v);
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

                if (len > MaxFileBytes)
                {
                    await WriteTextAsync(ns, "error: archivo demasiado grande");
                    return;
                }

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
                    var finalPath = await ReceiveAsync(ns, head, bodyStart, backlog, name, len);
                    await WriteTextAsync(ns, finalPath.Length > 0 ? "ok" : "error");
                    return;
                }

                if (method is "GET" or "HEAD")
                {
                    if (await ServeGetAsync(ns, path, query, method == "HEAD")) return;
                }

                await WriteTextAsync(ns, "QBasCopier y Transfer listo");
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
                sb.Append("{\"app\":\"QBasCopier y Transfer\",\"inbox\":\"").Append(JsonStr(Inbox)).Append("\",\"files\":[");
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
          .Append("<title>QBasCopier y Transfer</title><style>")
          .Append("body{font-family:system-ui,sans-serif;background:#eaf2ff;color:#0d1b3e;margin:0;padding:16px}")
          .Append("h1{font-size:19px;margin:0 0 4px}p{font-size:13px;margin:4px 0 14px}")
          .Append("table{border-collapse:collapse;width:100%;background:#fff;border-radius:10px;overflow:hidden}")
          .Append("th,td{font-size:13px;padding:8px 10px;border-bottom:1px solid #d3e0ff;text-align:left}")
          .Append("a{color:#0b3ea8}input,button{font-size:14px;padding:8px;border-radius:8px;border:1px solid #b9cdf5}")
          .Append("</style></head><body>");
        sb.Append("<h1>QBasCopier y Transfer</h1>");
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

    private async Task<string> ReceiveAsync(NetworkStream ns, byte[] head, int bodyStart, int backlog, string rawName, long len)
    {
        await _slots.WaitAsync();
        Stream? outS = null;
        string writing = "";
        // Buffer por recepcion. Antes era uno solo compartido por las 4 ranuras: con dos
        // descargas simultaneas los bloques de una se escribian en el archivo de la otra.
        var scratch = System.Buffers.ArrayPool<byte>.Shared.Rent(IoBuf);
        try
        {
            var name = Sanitize(rawName);
            bool toDoc = Inbox.StartsWith("content://", StringComparison.OrdinalIgnoreCase) && WriteDoc != null;
            if (toDoc)
            {
                outS = WriteDoc!(Inbox, name);
                if (outS == null) return "";
                writing = "";
            }
            else
            {
                try { Directory.CreateDirectory(Inbox); } catch { }
                writing = Unique(Path.Combine(Inbox, name));
                outS = new FileStream(writing, FileMode.Create, FileAccess.Write, FileShare.None,
                    IoBuf, FileOptions.Asynchronous | FileOptions.SequentialScan);
            }

            long bytes = backlog;
            if (len == -2)
            {
                if (backlog > 0) await outS.WriteAsync(head.AsMemory(bodyStart, backlog));
                long chunkTotal = 0;
                while (true)
                {
                    var sizeLine = await ReadLineAsync(ns);
                    if (sizeLine.Length == 0) break;
                    if (!long.TryParse(sizeLine.Split(';')[0], System.Globalization.NumberStyles.HexNumber, null, out var size))
                        break;
                    if (size <= 0) break;
                    chunkTotal += size;
                    if (chunkTotal > MaxFileBytes) { outS.Dispose(); outS = null; return Abandon(writing); }
                    long left = size;
                    while (left > 0)
                    {
                        int n = await ns.ReadAsync(scratch.AsMemory(0, (int)Math.Min(left, IoBuf)));
                        if (n <= 0) return Abandon(writing);
                        await outS.WriteAsync(scratch.AsMemory(0, n));
                        left -= n;
                        bytes += n;
                    }
                    await ConsumeCrlfAsync(ns);
                }
            }
            else
            {
                // len < 0 = longitud desconocida: sin tope, un cliente que nunca cierra
                // la conexion llenaria el disco. Se corta al pasar del maximo permitido.
                long left = len >= 0 ? len - backlog : MaxFileBytes - backlog;
                if (left <= 0) { outS.Dispose(); outS = null; return Abandon(writing); }
                while (left > 0)
                {
                    int n = await ns.ReadAsync(scratch.AsMemory(0, (int)Math.Min(left, IoBuf)));
                    if (n <= 0) break;
                    await outS.WriteAsync(scratch.AsMemory(0, n));
                    bytes += n;
                    left -= n;
                }
            }

            await outS.FlushAsync();
            outS.Dispose();
            outS = null;
            var finalPath = toDoc ? name : writing;
            FileReceived?.Invoke(finalPath, bytes);
            return finalPath;
        }
        catch { return Abandon(writing); }
        finally
        {
            try { outS?.Dispose(); } catch { }
            System.Buffers.ArrayPool<byte>.Shared.Return(scratch);
            _slots.Release();
        }
    }

    private string Abandon(string writing)
    {
        try { if (writing.Length > 0 && File.Exists(writing)) File.Delete(writing); } catch { }
        return "";
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

            long pos = 0;
            using (var src = open())
            {
                var buf = new byte[Buf];
                while (true)
                {
                    int n = await src.ReadAsync(buf.AsMemory(0, Buf), cts.Token);
                    if (n <= 0) break;
                    await ns.WriteAsync(buf.AsMemory(0, n), cts.Token);
                    pos += n;
                    progress?.Invoke(pos);
                }
            }
            await ns.FlushAsync(cts.Token);
            if (total > 0 && pos != total) return -1;

            var resp = new byte[1024];
            int got = 0;
            while (got < resp.Length)
            {
                int n = await ns.ReadAsync(resp.AsMemory(got, resp.Length - got), cts.Token);
                if (n <= 0) break;
                got += n;
                if (Encoding.UTF8.GetString(resp, 0, got).Contains("200 OK")) break;
            }
            return Encoding.UTF8.GetString(resp, 0, got).Contains("200 OK") ? total : -1;
        }
        catch { return -1; }
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
