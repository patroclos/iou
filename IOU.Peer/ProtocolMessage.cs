using System.IO;

namespace IOU.Peer
{
    public interface IProtocolMessage
    {
    }

    public interface ISelfSerialize {
        byte[] ToByteArray();
    }

    public struct KeepAlive : IProtocolMessage
    {
    }

    public struct Choke : IProtocolMessage
    {
    }

    public struct Unchoke : IProtocolMessage
    {
    }

    public struct Interested : IProtocolMessage
    {
    }

    public struct NotInterested : IProtocolMessage
    {
    }

    public struct Have : IProtocolMessage
    {
        public uint PieceIndex;
    }

    public struct Bitfield : IProtocolMessage
    {
        public byte[] Bits;
    }

    public struct Request : IProtocolMessage
    {
        public uint PieceIndex;
        public uint Begin;
        public uint Length;
    }

    public struct Piece : IProtocolMessage
    {
        public uint PieceIndex;
        public uint Begin;
        public byte[] Content;
    }

    public struct Cancel : IProtocolMessage
    {
        public uint PieceIndex;
        public uint Begin;
        public uint Length;
    }

    public abstract class Extended : IProtocolMessage
    {
        public string ExtensionName { get; }

        protected Extended(string extensionName)
        {
            ExtensionName = extensionName;
        }

        public abstract void WriteTo(BinaryWriter writer);
    }
}
