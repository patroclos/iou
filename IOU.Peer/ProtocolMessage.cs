using System.IO;

namespace IOU.Peer
{
    // TODO: should all these message definitions be interfaces instead?
    public interface IProtocolMessage
    {
    }

    public class KeepAlive : IProtocolMessage
    {
    }

    public class Choke : IProtocolMessage
    {
    }

    public class Unchoke : IProtocolMessage
    {
    }

    public class Interested : IProtocolMessage
    {
    }

    public class NotInterested : IProtocolMessage
    {
    }

    public class Have : IProtocolMessage
    {
        public uint PieceIndex;
    }

    public class Bitfield : IProtocolMessage
    {
        public byte[] Bits;
    }

    public class Request : IProtocolMessage
    {
        public uint PieceIndex;
        public uint Begin;
        public uint Length;
    }

    public class Piece : IProtocolMessage
    {
        public uint PieceIndex;
        public uint Begin;
        public byte[] Content;
    }

    public class Cancel : IProtocolMessage
    {
        public uint PieceIndex;
        public uint Begin;
        public uint Length;
    }

    public abstract class Extended : IProtocolMessage
    {
        public string ExtensionName { get; }
        public abstract void WriteTo(BinaryWriter writer);
    }
}
