using System;
using System.Buffers;
using System.IO;
using BinaryEncoding;

namespace IOU.Peer
{
    public static class ProtocolSerialization
    {
        public struct ParsedMessage
        {
            public ProtocolMessage Message { get; set; }
            public SequencePosition Position { get; set; }
        }
        
        public static void WriteMessage(ProtocolMessage message, BinaryWriter writer)
        {
            throw new NotImplementedException();
        }

        public static ParsedMessage? TryParseMessage(ReadOnlySequence<byte> buf)
        {
            if (buf.Length < 4)
                return null;

            var off = 4;
            var be = Binary.BigEndian;
            var len = be.GetUInt32(buf.Slice(0, 4).ToArray());

            if (len == 0)
                return new ParsedMessage
                {
                    Message = new KeepAlive(),
                    Position = buf.GetPosition(off)
                };

            var type = buf.Slice(off++, 1).FirstSpan[0];

            var parsed = null as ProtocolMessage;
            switch (type)
            {
                case 0:
                    parsed= new Choke();
                    break;
                case 1:
                    parsed= new Unchoke();
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
                    parsed = new Have{PieceIndex = idx};
                    break;
                }
                case 5:
                    throw new NotImplementedException("Bitfield");
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
            
            if(parsed != null)
                return new ParsedMessage
                {
                    Message = parsed,
                    Position = buf.GetPosition(off)
                };

            return null;
        }
    }
}