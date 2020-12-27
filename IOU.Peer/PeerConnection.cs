using System;
using System.Net;
using System.Text;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Diagnostics;

namespace IOU.Peer
{
    public class PeerConnection : IDisposable
    {
        private readonly IPEndPoint EndPoint;
        private readonly TcpClient _client;

        public bool IsChoked { get; private set; } = true;
        public bool IsInterested { get; private set; } = false;

        private PeerConnection(IPEndPoint endPoint, TcpClient client)
        {
            EndPoint = endPoint;
            _client = client;
        }

        public static async Task<PeerConnection> EstablishConnection(IPEndPoint endpoint, TimeSpan timeout)
        {
            var client = new TcpClient();
            var connectTask = client.ConnectAsync(endpoint.Address, endpoint.Port);
            var cancelTask = Task.Delay(timeout);

            await await Task.WhenAny(connectTask, cancelTask);

            if (!connectTask.IsCompleted)
            {
                client.Dispose();
                throw new TimeoutException(
                    $"{nameof(EstablishConnection)} exeeded timeout of {timeout} while connecting to {endpoint}");
            }

            if (connectTask.IsFaulted)
            {
                client.Dispose();
                throw new AggregateException($"Failed establishing peer-connection with {endpoint}",
                    connectTask.Exception!);
            }

            return new PeerConnection(endpoint, client);
        }

        // TODO: where does the infohash and peerid come from (constructor inject?)
        public async Task DoHandshake()
        {
            throw new NotImplementedException();
            /*
            var stream = _client.GetStream();
            var handshake = BuildHandshake();
            await stream.WriteAsync(handshake, 0, handshake.Length);
            throw new NotImplementedException();
            */
        }

        private async void StartMessageLoop() {
            var stream = _client.GetStream();

            byte[] readBuf = new byte[4096];

            throw new NotImplementedException();
        }

        public void Dispose()
        {
            _client.Dispose();
        }

        private static byte[] BuildHandshake(byte[] infoHash, byte[] peerId) {
            Debug.Assert(infoHash.Length == 20);
            Debug.Assert(peerId.Length == 20);

            Span<byte> buf = stackalloc byte[68];
            buf[0]=19;
            Encoding.UTF8.GetBytes("BitTorrent protocol", buf.Slice(1));
            infoHash.AsSpan().CopyTo(buf.Slice(28));
            peerId.AsSpan().CopyTo(buf.Slice(48));

            return buf.ToArray();
        }
    }
}
