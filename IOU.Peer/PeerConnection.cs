using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.IO.Pipelines;
using System.Buffers;
using System.Linq;
using System.Threading;
using System.IO;

namespace IOU.Peer
{
    public class PeerConnection : IDisposable
    {
        private readonly Stream _stream;
        private readonly Stream _outStream;
        private readonly PipeReader _reader;
        private readonly CancellationTokenSource _cancelTokenSource;

        public Handshake? PeerHandshake { get; private set; }
        public Bitfield? PeerBitfield { get; private set; }

        public bool IsChoked { get; private set; } = true;
        public bool IsInterested { get; private set; } = false;

        public event Action<IProtocolMessage> MessageReceived = delegate { };

        public PeerConnection(Stream stream, Stream? outputStream = null)
        {
            _reader = PipeReader.Create(stream);
            _stream = stream;
            _outStream = outputStream ?? stream;
            _cancelTokenSource = new CancellationTokenSource();

            _ = RunMessageLoop();
        }

        public static async Task<PeerConnection> EstablishConnection(IPEndPoint endpoint, TimeSpan timeout)
        {
            var sock = new Socket(endpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            var connectTask = sock.ConnectAsync(endpoint.Address, endpoint.Port);
            var cancelTask = Task.Delay(timeout);

            await await Task.WhenAny(connectTask, cancelTask);

            if (!connectTask.IsCompleted)
            {
                sock.Dispose();
                throw new TimeoutException(
                    $"{nameof(EstablishConnection)} exeeded timeout of {timeout} while connecting to {endpoint}");
            }

            if (connectTask.IsFaulted)
            {
                sock.Dispose();
                throw new AggregateException($"Failed establishing peer-connection with {endpoint}",
                    connectTask.Exception!);
            }

            return new PeerConnection(new NetworkStream(sock, ownsSocket: true));
        }

        public Task SendMessage(IProtocolMessage msg)
        {
            var buf = ProtocolSerialization.SerializeMessage(msg);
            return this._outStream.WriteAsync(buf, 0, buf.Length);
        }

        private async Task RunMessageLoop()
        {
            var cancelToken = this._cancelTokenSource.Token;

            this.PeerHandshake = await PeerConnection.ReadHandshake(_reader);

            while (true)
            {
                if (cancelToken.IsCancellationRequested)
                    cancelToken.ThrowIfCancellationRequested();
                var result = await _reader.ReadAsync(cancelToken);
                var buffer = result.Buffer;

                while (true)
                {
                    var msg = ProtocolSerialization.TryParseMessage(buffer);
                    if (!msg.HasValue)
                        break;

                    buffer = buffer.Slice(msg.Value.Position);
                    _reader.AdvanceTo(msg.Value.Position);

                    if (msg.Value.Message is Bitfield bf)
                        this.PeerBitfield = bf;

                    MessageReceived(msg.Value.Message);
                }
            }
        }

        private static async Task<Handshake> ReadHandshake(PipeReader reader)
        {
            while (true)
            {
                var result = await reader.ReadAsync();
                var buf = result.Buffer;

                var handshake = Handshake.TryParse(buf);

                if (handshake != null)
                {
                    reader.AdvanceTo(buf.GetPosition(Handshake.ByteLength));
                    return handshake;
                }

                if (result.IsCompleted)
                {
                    Console.WriteLine(Utils.HexDump(buf.ToArray(), 32));
                    throw new Exception($"EOF before handshake after {buf.Length} bytes");
                }

                continue;

            }
        }

        public void Dispose()
        {
            _reader.Complete();
            _outStream.Dispose();
            _stream.Dispose();
            _cancelTokenSource.Dispose();
        }
    }
}
