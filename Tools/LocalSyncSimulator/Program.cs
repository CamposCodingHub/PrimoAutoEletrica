using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

string id = "StationA";
int port = 52000;
int duration = 8; // seconds

for (int i = 0; i < args.Length; i++)
{
    switch (args[i])
    {
        case "--id": if (i + 1 < args.Length) id = args[++i]; break;
        case "--port": if (i + 1 < args.Length && int.TryParse(args[++i], out var p)) port = p; break;
        case "--duration": if (i + 1 < args.Length && int.TryParse(args[++i], out var d)) duration = d; break;
    }
}

const string multicast = "239.0.0.222";
var cts = new CancellationTokenSource(TimeSpan.FromSeconds(duration + 2));

using var listener = new UdpClient();
try
{
    listener.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
    listener.Client.Bind(new IPEndPoint(IPAddress.Any, port));
    listener.JoinMulticastGroup(IPAddress.Parse(multicast));
}
catch (Exception ex)
{
    Console.WriteLine($"[{id}] Listener start failed: {ex.Message}");
}

_ = Task.Run(async () =>
{
    var remoteEp = new IPEndPoint(IPAddress.Any, 0);
    while (!cts.IsCancellationRequested)
    {
        try
        {
            var res = await listener.ReceiveAsync(cts.Token).ConfigureAwait(false);
            var text = Encoding.UTF8.GetString(res.Buffer);
            Console.WriteLine($"[{id}] RCV from {res.RemoteEndPoint}: {text}");
            var logDir = System.IO.Path.Combine(AppContext.BaseDirectory, "logs");
            System.IO.Directory.CreateDirectory(logDir);
            System.IO.File.AppendAllText(System.IO.Path.Combine(logDir, $"{id}.log"), $"{DateTime.Now:O} RCV {res.RemoteEndPoint} {text}\n");
        }
        catch (OperationCanceledException) { break; }
        catch (Exception ex)
        {
            Console.WriteLine($"[{id}] Listener error: {ex.Message}");
            await Task.Delay(200, cts.Token).ConfigureAwait(false);
        }
    }
}, cts.Token);

using var sender = new UdpClient();
try
{
    var multicastEp = new IPEndPoint(IPAddress.Parse(multicast), port);
    var sw = System.Diagnostics.Stopwatch.StartNew();
    while (sw.Elapsed.TotalSeconds < duration)
    {
        var message = $"SIM:{id}:{DateTime.Now:O}";
        var bytes = Encoding.UTF8.GetBytes(message);
        try
        {
            await sender.SendAsync(bytes, bytes.Length, multicastEp).ConfigureAwait(false);
            await sender.SendAsync(bytes, bytes.Length, new IPEndPoint(IPAddress.Broadcast, port)).ConfigureAwait(false);
            Console.WriteLine($"[{id}] SENT: {message}");
            var logDir = System.IO.Path.Combine(AppContext.BaseDirectory, "logs");
            System.IO.Directory.CreateDirectory(logDir);
            System.IO.File.AppendAllText(System.IO.Path.Combine(logDir, $"{id}.log"), $"{DateTime.Now:O} SENT {message}\n");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[{id}] Send error: {ex.Message}");
        }

        await Task.Delay(1000, cts.Token).ConfigureAwait(false);
    }
}
catch (OperationCanceledException) { }
finally
{
    cts.Cancel();
    Console.WriteLine($"[{id}] Exiting simulator.");
}
