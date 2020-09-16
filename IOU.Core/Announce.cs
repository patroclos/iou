using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using BinaryEncoding;


namespace IOU
{
    public class Announce
    {
        private readonly IPEndPoint EndPoint;
        private readonly UdpClient _client;

        public Announce(IPEndPoint endPoint)
        {
            EndPoint = endPoint;
            _client = new UdpClient();
            _client.Connect(EndPoint);
        }

        private async Task<UdpReceiveResult> ReceiveTimeoutAsync(TimeSpan timeout, CancellationToken token = default)
        {
            var rcvT = _client.ReceiveAsync();
            var timeoutT = Task.Delay(timeout, token);

            await Task.WhenAny(rcvT, timeoutT);

            if (rcvT.IsCompleted)
                return rcvT.Result;
            
            _client.Dispose();
            throw new TimeoutException($"Receiving from UDP endpoint {EndPoint} timed out after {timeout}");
        }

        public async Task<IPEndPoint[]> AnnounceAsync(byte[] infoHash)
        {
            var conreq = BuildConnectionRequest(0);
            await _client.SendAsync(conreq, conreq.Length);

            // var d = Task.Delay(TimeSpan.FromSeconds(5));
            // var x = Task.FromException<TimeoutException>(new TimeoutException()).;
            // var response = await Task.WhenAny(client.ReceiveAsync(), x);
            // Task.Delay(TimeSpan.FromSeconds(5)).ContinueWith(_=>client.Close());
            var response = await ReceiveTimeoutAsync(TimeSpan.FromSeconds(3));
            var resp = ReadConnectionResponse(response.Buffer);

            var peerId = Encoding.ASCII.GetBytes("-SLPSTR/BRNCLE-AAAAAAAAAAAAAAAA");

            var announceReq = BuildAnnounceRequest(resp.ConnectionId, resp.TransactionId, infoHash, peerId, 0, 0, 0, 2,
                0, 0, 0);

            await _client.SendAsync(announceReq, announceReq.Length);

            response = await ReceiveTimeoutAsync(TimeSpan.FromSeconds(3));
            var announceResponse = ReadAnnounceResponse(response.Buffer);
            
            foreach(var ep in announceResponse.Endpoints)
                Console.WriteLine($"Announce Con: {ep}");

            return null;
        }

        private byte[] BuildConnectionRequest(int transactionId)
        {
            var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);

            var be = Binary.BigEndian;
            var protocolId = be.GetBytes(0x41727101980L);
            writer.Write(protocolId);
            writer.Write(be.GetBytes(0));
            writer.Write(be.GetBytes(transactionId));

            return stream.ToArray();
        }

        private byte[] BuildAnnounceRequest(ulong connectionId, uint transactionId, byte[] infoHash,
            byte[] peerId, ulong downed, ulong left, ulong upped, uint evt, uint key, uint y, ushort port)
        {
            var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);

            var be = Binary.BigEndian;

            writer.Write(be.GetBytes(connectionId));
            writer.Write(be.GetBytes(1)); // action
            writer.Write(be.GetBytes(transactionId));
            writer.Write(infoHash);
            writer.Write(peerId);
            writer.Write(be.GetBytes(downed));
            writer.Write(be.GetBytes(left));
            writer.Write(be.GetBytes(upped));
            writer.Write(be.GetBytes(evt));
            writer.Write(be.GetBytes(0)); // ip
            writer.Write(be.GetBytes(key));
            writer.Write(be.GetBytes(-1));
            writer.Write(be.GetBytes(port));

            return stream.ToArray();
        }

        private struct ConnectResponse
        {
            public uint TransactionId;
            public ulong ConnectionId;
            public uint Action;
        }

        private ConnectResponse ReadConnectionResponse(byte[] buffer)
        {
            var reader = new BinaryReader(new MemoryStream(buffer));

            var be = Binary.BigEndian;

            var action = be.GetUInt32(reader.ReadBytes(4));
            var transactionId = be.GetUInt32(reader.ReadBytes(4));
            var connectionId = be.GetUInt64(reader.ReadBytes(8));

            return new ConnectResponse {Action = action, TransactionId = transactionId, ConnectionId = connectionId};
        }
        
        private struct AnnounceResponse
        {
            public List<IPEndPoint> Endpoints { get; set; }
        }

        private AnnounceResponse ReadAnnounceResponse(byte[] buffer)
        {
            using var stream = new MemoryStream(buffer);
            using var reader = new BinaryReader(stream);

            var be = Binary.BigEndian;


            var action = be.GetUInt32(reader.ReadBytes(4));
            var trnId = be.GetUInt32(reader.ReadBytes(4));
            var interval = be.GetUInt32(reader.ReadBytes(4));
            var leechers = be.GetUInt32(reader.ReadBytes(4));
            var seeders = be.GetUInt32(reader.ReadBytes(4));
            
            var endpoints = new List<IPEndPoint>();
            while (reader.BaseStream.Position != reader.BaseStream.Length)
            {
                var ip = new IPAddress(reader.ReadBytes(4));
                var port = be.GetInt16(reader.ReadBytes(2));
                endpoints.Add(new IPEndPoint(ip, port));
            }
            
            return new AnnounceResponse
            {
                Endpoints = endpoints
            };
        }
    }
}