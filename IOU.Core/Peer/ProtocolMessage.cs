using System;
using System.IO;

namespace IOU.Peer
{
    public abstract class ProtocolMessage
    {
    }

    public class Handshake : ProtocolMessage
    {
        public byte[] InfoHash;
        public byte[] PeerId;
        public byte[] Reserved;
    }
    
    public class KeepAlive : ProtocolMessage {}
    public class Choke : ProtocolMessage {}
    public class Unchoke : ProtocolMessage {}
    public class Interested : ProtocolMessage {}
    public class NotInterested : ProtocolMessage {}

    public class Have : ProtocolMessage
    {
        public uint PieceIndex;
    }

    public class Bitfield : ProtocolMessage
    {
        public byte[] Bits;
    }

    public class Request : ProtocolMessage
    {
        public uint PieceIndex;
        public uint Begin;
        public uint Length;
    }
    
    public class Piece : ProtocolMessage
    {
        public uint PieceIndex;
        public uint Begin;
        public byte[] Content;
    }

    public class Cancel : ProtocolMessage
    {
        public uint PieceIndex;
        public uint Begin;
        public uint Length;
    }

    public abstract class Extended : ProtocolMessage
    {
        public string ExtensionName { get; }
        public abstract void WriteTo(BinaryWriter writer);
    }


    public class ProtocolSerializer
    {
        public static void WriteMessage(ProtocolMessage message, BinaryWriter writer)
        {
            throw new NotImplementedException();
        }

        public static bool TryParseMessage(ReadOnlySpan<byte> buf, out ProtocolMessage message, out int consumed)
        {
            throw new NotImplementedException();
        }
    }
}