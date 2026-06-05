using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PrimoAutoEletrica.Services
{
    /// <summary>
    /// Servico simples de sincronizacao local entre estacoes usando UDP.
    /// Envia mensagens de texto em UTF8 no endereco multicast 239.0.0.222.
    /// Projetado apenas para anunciar eventos leves (ex: "RegistroAlterado:Clientes:GUID").
    /// </summary>
    public class LocalSyncService : IDisposable
    {
        private const string MulticastAddress = "239.0.0.222";
        private readonly int _port;
        private UdpClient? _client;
        private IPEndPoint? _remoteEndPoint;
        private CancellationTokenSource? _cts;
        private Task? _listenTask;

        public event Action<string, IPEndPoint>? MessageReceived;

        public LocalSyncService(int port = 52000)
        {
            _port = port;
        }

        public void Start()
        {
            if (_client != null) return;

            _cts = new CancellationTokenSource();
            _client = new UdpClient();
            _client.EnableBroadcast = true;

            _remoteEndPoint = new IPEndPoint(IPAddress.Parse(MulticastAddress), _port);

            try
            {
                _client.JoinMulticastGroup(IPAddress.Parse(MulticastAddress));
            }
            catch
            {
                // Ignore if OS/network doesn't support join; still allow broadcast style sends
            }

            _listenTask = Task.Run(() => ListenLoop(_cts.Token), _cts.Token);
        }

        public void Stop()
        {
            try
            {
                _cts?.Cancel();
                _listenTask?.Wait(500);
            }
            catch { }
            finally
            {
                try { if (_client != null) { _client.DropMulticastGroup(IPAddress.Parse(MulticastAddress)); } } catch { }
                _client?.Close();
                _client = null;
                _cts?.Dispose();
                _cts = null;
            }
        }

        private async Task ListenLoop(CancellationToken token)
        {
            try
            {
                using var listener = new UdpClient(_port);
                var localEp = new IPEndPoint(IPAddress.Any, _port);

                while (!token.IsCancellationRequested)
                {
                    UdpReceiveResult result;
                    try
                    {
                        result = await listener.ReceiveAsync().ConfigureAwait(false);
                    }
                    catch (ObjectDisposedException)
                    {
                        break;
                    }
                    catch (Exception)
                    {
                        await Task.Delay(100, token).ConfigureAwait(false);
                        continue;
                    }

                    var text = Encoding.UTF8.GetString(result.Buffer);
                    MessageReceived?.Invoke(text, result.RemoteEndPoint);
                }
            }
            catch (Exception)
            {
                // swallow - service is best-effort
            }
        }

        public void Send(string message)
        {
            try
            {
                if (_client == null)
                {
                    using var temp = new UdpClient();
                    var bytes = Encoding.UTF8.GetBytes(message);
                    temp.Send(bytes, bytes.Length, new IPEndPoint(IPAddress.Broadcast, _port));
                    return;
                }

                var bytesLocal = Encoding.UTF8.GetBytes(message);
                if (_remoteEndPoint != null)
                {
                    _client.Send(bytesLocal, bytesLocal.Length, _remoteEndPoint);
                }
                else
                {
                    _client.Send(bytesLocal, bytesLocal.Length, new IPEndPoint(IPAddress.Broadcast, _port));
                }
            }
            catch
            {
                // ignore send failures
            }
        }

        public void Dispose()
        {
            Stop();
        }
    }
}
