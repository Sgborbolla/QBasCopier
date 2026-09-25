using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace QBasCopier;

public static class Discovery
{
    public static event Action? Changed;
    public static readonly List<(string Ip, string Name, int Port)> Devices = new();
    private static readonly List<UdpClient> _rcvs = new();
    private static UdpClient? _snd;
    private static CancellationTokenSource? _cts;
    private static string _me = "";
    private static int _port;
    private static readonly Dictionary<string, long> _seen = new();
    private static readonly Dictionary<string, long> _repliedTo = new();

    // puertos en los que también escuchamos para convivir con Zapya/SHAREit/Xender
    private static readonly int[] Ports = { TransferHost.DiscoveryPort, TransferHost.LegacyDiscoveryPort };

    public static void Start(string myName, int port)
    {
        try { Stop(); } catch { }
        _me = string.IsNullOrWhiteSpace(myName) ? "dispositivo" : myName;
        _port = port;
        _cts = new CancellationTokenSource();
        foreach (var p in Ports)
        {
            var u = MakeReceiver(p);
            if (u == null) continue;
            _rcvs.Add(u);
            _ = Task.Run(() => RcvLoop(u, p, _cts!.Token));
        }
        try
        {
            _snd = new UdpClient { EnableBroadcast = true };
            ReusePort(_snd);
        }
        catch { _snd = null; }
        if (_snd != null) _ = Task.Run(() => AnnLoop(_cts!.Token));
    }

    public static void Stop()
    {
        try { _cts?.Cancel(); } catch { }
        lock (_rcvs)
        {
            foreach (var u in _rcvs) { try { u.Close(); } catch { } }
            _rcvs.Clear();
        }
        try { _snd?.Close(); _snd = null; } catch { }
        lock (Devices) { _seen.Clear(); Devices.Clear(); }
    }

    private static UdpClient? MakeReceiver(int port)
    {
        try
        {
            var u = new UdpClient(new IPEndPoint(IPAddress.Any, port)) { EnableBroadcast = true };
            ReusePort(u);
            return u;
        }
        catch { return null; }
    }

    // permite que la app y Zapya convivan en el mismo puerto (SO_REUSEPORT en Linux/Android)
    private static void ReusePort(UdpClient u)
    {
        try { u.Client.SetSocketOption(SocketOptionLevel.Socket, (SocketOptionName)15, true); } catch { }
        try { u.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true); } catch { }
    }

    public static void Announce()
    {
        try
        {
            var msg = $"{_me}|{_port}|QBT1";
            var b = Encoding.UTF8.GetBytes(msg);
            foreach (var p in Ports)
                _snd?.Send(b, b.Length, new IPEndPoint(IPAddress.Broadcast, p));
        }
        catch { }
    }

    private static void AnnLoop(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            Announce();
            ct.WaitHandle.WaitOne(3000);
        }
    }

    private static void RcvLoop(UdpClient rcv, int port, CancellationToken ct)
    {
        var ep = new IPEndPoint(IPAddress.Any, 0);
        while (!ct.IsCancellationRequested)
        {
            byte[] data;
            try { data = rcv.Receive(ref ep); } catch { break; }
            if (data.Length == 0) continue;
            var fromIp = ep.Address.ToString();
            try
            {
                var t = Encoding.UTF8.GetString(data).Split('|');
                if (t.Length >= 3 && t[2] == "QBT1")
                {
                    var name = t[0];
                    var p = int.TryParse(t[1], out var pp) ? pp : 0;
                    if (name == _me && p == _port) continue;      // nosotros
                    if (fromIp == "255.255.255.255") continue;     // sin ip de origen
                    Add(fromIp, name, p);
                    continue;
                }

                // Sondeo de otra app (Zapya/SHAREit/Xender u otra QBasCopier): respondemos con nuestra
                // firma para que nos vean en la misma red, sin exigir su formato binario.
                if (ShouldReply(fromIp))
                {
                    var rep = Encoding.UTF8.GetBytes($"{_me}|{_port}|QBT1");
                    try { rcv.Send(rep, rep.Length, new IPEndPoint(ep.Address, port)); } catch { }
                }
            }
            catch { }
        }
    }

    private static bool ShouldReply(string ip)
    {
        if (ip.Length == 0 || ip == "255.255.255.255") return false;
        lock (_repliedTo)
        {
            var now = NetTools.Now;
            if (_repliedTo.TryGetValue(ip, out var t) && now - t < 8000) return false;
            _repliedTo[ip] = now;
            if (_repliedTo.Count > 200) _repliedTo.Clear();
        }
        return true;
    }

    private static void Add(string ip, string name, int port)
    {
        if (port <= 0) return;
        var entry = (ip, name, port);
        bool changed;
        lock (Devices)
        {
            _seen[ip] = NetTools.Now;
            var i = Devices.FindIndex(d => d.Item1 == ip);
            changed = i < 0 || Devices[i].Item2 != name || Devices[i].Item3 != port;
            if (i >= 0) Devices[i] = entry;
            else Devices.Add(entry);
            var now = NetTools.Now;
            foreach (var d in Devices)
                if (now - _seen.GetValueOrDefault(d.Item1) > 15_000)
                    _seen[d.Item1] = now - 60_000;
            Devices.RemoveAll(d => now - _seen.GetValueOrDefault(d.Item1) > 15_000);
        }
        if (changed) Changed?.Invoke();
    }
}
