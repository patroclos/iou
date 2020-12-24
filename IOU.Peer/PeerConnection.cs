using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace IOU.Peer
{
    public class PeerConnection : IDisposable
    {
        private readonly IPEndPoint EndPoint;
        private readonly TcpClient _client;

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
                    connectTask.Exception);
            }

            return new PeerConnection(endpoint, client);
        }

        public Task DoHandshake()
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            _client.Dispose();
        }
    }
}