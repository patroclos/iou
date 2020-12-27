using System;
using System.Buffers;
using System.IO;
using System.Linq;
using BinaryEncoding;

namespace IOU.Peer
{
    public static class ProtocolSerialization
    {
        public struct ParsedMessage
        {
            public IProtocolMessage Message { get; set; }
            public SequencePosition Position { get; set; }
        }

        public static byte[] SerializeMessage(IProtocolMessage message)
        {
            var be = Binary.BigEndian;
            switch (message)
            {
                case ISelfSerialize selfSerialize:
                    return selfSerialize.ToByteArray();
                case KeepAlive:
                    return new byte[4] { 0, 0, 0, 0 };
                case Choke:
                    return new byte[5] { 0, 0, 0, 1, 0 };
                case Unchoke:
                    return new byte[5] { 0, 0, 0, 1, 1 };
                case Interested:
                    return new byte[5] { 0, 0, 0, 1, 2 };
                case NotInterested:
                    return new byte[5] { 0, 0, 0, 1, 3 };
                case Have have:
                    return new byte[] { 0, 0, 0, 5, 4 }
                        .Concat(be.GetBytes(have.PieceIndex))
                        .ToArray();
                case Bitfield bitfield:
                    return be.GetBytes(bitfield.Bits.Length + 1)
                        .Concat(new byte[] { 5 })
                        .Concat(bitfield.Bits)
                        .ToArray();
                default:
                    throw new NotImplementedException($"No serialization implemented for protocol message type {message.GetType()}");
            }
        }

        public static ParsedMessage? TryParseMessage(ReadOnlySequence<byte> buf)
        {
            var be = Binary.BigEndian;

            if (buf.Length < 4)
                return null;

            var off = 4;
            var len = be.GetUInt32(buf.Slice(0, 4).ToArray());

            if (len == 0)
                return new ParsedMessage
                {
                    Message = new KeepAlive(),
                    Position = buf.GetPosition(off)
                };

            if (len + off > buf.Length)
                return null;

            var type = buf.Slice(off++, 1).FirstSpan[0];

            IProtocolMessage? parsed = null;
            switch (type)
            {
                case 0:
                    parsed = new Choke();
                    break;
                case 1:
                    parsed = new Unchoke();
                    break;
                case 2:
                    parsed = new Interested();
                    break;
                case 3:
                    parsed = new NotInterested();
                    break;
                case 4:
                    {
                        var idx = be.GetUInt32(buf.Slice(off, 4).ToArray());
                        off += 4;
                        parsed = new Have { PieceIndex = idx };
                        break;
                    }
                case 5:
                    {
                        var bitfieldLen = (int)(len - 1);
                        var bits = buf.Slice(off, bitfieldLen).ToArray();
                        off += bitfieldLen;
                        parsed = new Bitfield { Bits = bits };
                        break;
                    }
                case 6:
                    throw new NotImplementedException("Request");
                case 7:
                    throw new NotImplementedException("Piece");
                case 8:
                    throw new NotImplementedException("Cancel");
                case 20:
                    throw new NotImplementedException("Extension Message");
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), $"Invalid Peer Message type {type}");
            }

            if (parsed != null)
                return new ParsedMessage
                {
                    Message = parsed,
                    Position = buf.GetPosition(off)
                };

            return null;
        }
    }
}
