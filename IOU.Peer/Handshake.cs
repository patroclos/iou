using System;
using System.Buffers;
using System.Diagnostics;
using System.Linq;

namespace IOU.Peer
{
    public class Handshake : IEquatable<Handshake>, IProtocolMessage, ISelfSerialize
    {
        public const int ByteLength = 68;
        public static readonly byte[] Magic = new char[]{
            (char)19, 'B','i','t','T','o','r','r','e','n', 't',' ', 'p','r','o','t','o','c','o','l'
        }
        .Select(x => (byte)x)
        .ToArray();

        public byte[] Reserved;
        public byte[] PeerId;
        public byte[] InfoHash;

        public Handshake(byte[] reserved, byte[] infoHash, byte[] peerId)
        {
            Debug.Assert(reserved.Length == 8);
            Debug.Assert(infoHash.Length == 20);
            Debug.Assert(peerId.Length == 20);

            Reserved = reserved;
            PeerId = peerId;
            InfoHash = infoHash;
        }

        public byte[] ToByteArray()
        {
            var buf = new byte[ByteLength];
            Magic.CopyTo(buf, 0);
            Reserved.CopyTo(buf, 20);
            InfoHash.CopyTo(buf, 28);
            PeerId.CopyTo(buf, 48);

            return buf;
        }

        public static Handshake? TryParse(byte[] buffer)
            => TryParse(new ReadOnlySequence<byte>(buffer));

        public static Handshake? TryParse(ReadOnlySequence<byte> buffer)
        {
            if (buffer.Length < ByteLength)
                return null;

            Span<byte> bytes = buffer.Slice(0, ByteLength).ToArray();

            if (!bytes.Slice(0, Magic.Length).SequenceEqual(Magic))
                throw new ArgumentOutOfRangeException(nameof(buffer), "Buffer doesn't start w/ magic sequence");

            var reserved = bytes.Slice(Magic.Length, 8);
            var infoHash = bytes.Slice(Magic.Length + 8, 20);
            var peerId = bytes.Slice(Magic.Length + 8 + 20, 20);

            return new Handshake(reserved.ToArray(), infoHash.ToArray(), peerId.ToArray());
        }

        public bool Equals(Handshake? other)
        {
            if (ReferenceEquals(null, other))
                return false;
            if (other.GetType() != this.GetType())
                return false;

            return Reserved.SequenceEqual(other.Reserved)
                && InfoHash.SequenceEqual(other.InfoHash)
                && PeerId.SequenceEqual(other.PeerId);
        }

        public override bool Equals(object? obj)
        {
            return Equals(obj as Handshake);
        }

        public override int GetHashCode()
            => HashCode.Combine(Reserved, InfoHash, PeerId);
    }
}
