using System.Buffers;
using System.Linq;
using NUnit.Framework;

namespace IOU.Peer
{
    public class HandshakeTests
    {
        [Test]
        public void ParseHandshakeFail_Empty()
        {
            var bytes = new byte[] { };
            var seq = new ReadOnlySequence<byte>(bytes);
            var parsed = Handshake.TryParse(seq);

            Assert.IsNull(parsed);
        }

        [Test]
        public void HandshakeMagicIs20BytesLong()
        {
            Assert.AreEqual(20, Handshake.Magic.Length);
        }

        [Test]
        public void ParseHandshake_Success()
        {
            var bytes = Handshake.Magic.Concat(
                    new byte[48]
                    ).ToArray();
            var seq = new ReadOnlySequence<byte>(bytes);
            var parsed = Handshake.TryParse(seq);

            Assert.AreEqual(Handshake.ByteLength, seq.Length);
            Assert.IsNotNull(parsed);
        }

        [Test]
        public void Handshake_Encode_Decode_Equality()
        {
            var handshake = new Handshake(new byte[8], new byte[20], new byte[20]);
            var encoded = handshake.ToByteArray();
            var decoded = Handshake.TryParse(new ReadOnlySequence<byte>(encoded));

            Assert.AreEqual(handshake, decoded);
        }
    }
}
