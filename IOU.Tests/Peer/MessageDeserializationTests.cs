using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using IOU.Peer;
using NUnit.Framework;

namespace IOU.Tests.Peer
{
    public class MessageDeserializationTests
    {
        [Test]
        public void ParseKeepAlive()
        {
            var buffer = new byte[] { 0, 0, 0, 0 };

            var result = ProtocolSerialization.TryParseMessage(new ReadOnlySequence<byte>(buffer));
            Assert.IsTrue(result.HasValue);
            Assert.IsInstanceOf(typeof(KeepAlive), result!.Value.Message);
        }

        [Test]
        public void ParseChoke()
        {
            var buffer = new byte[] { 0, 0, 0, 1, 0 };

            var result = ProtocolSerialization.TryParseMessage(new ReadOnlySequence<byte>(buffer));
            Assert.IsTrue(result.HasValue);
            Assert.IsInstanceOf(typeof(Choke), result!.Value.Message);
        }

        [Test]
        public void ParseUnchoke()
        {
            var buffer = new byte[] { 0, 0, 0, 1, 1 };

            var result = ProtocolSerialization.TryParseMessage(new ReadOnlySequence<byte>(buffer));
            Assert.IsTrue(result.HasValue);
            Assert.IsInstanceOf(typeof(Unchoke), result!.Value.Message);
        }

        [Test]
        public void ParseInterested()
        {
            var buffer = new byte[] { 0, 0, 0, 1, 2 };

            var result = ProtocolSerialization.TryParseMessage(new ReadOnlySequence<byte>(buffer));
            Assert.IsTrue(result.HasValue);
            Assert.IsInstanceOf(typeof(Interested), result!.Value.Message);
        }

        [Test]
        public void ParseNotInterested()
        {
            var buffer = new byte[] { 0, 0, 0, 1, 3 };

            var result = ProtocolSerialization.TryParseMessage(new ReadOnlySequence<byte>(buffer));
            Assert.IsTrue(result.HasValue);
            Assert.IsInstanceOf(typeof(NotInterested), result!.Value.Message);
        }

        [Test]
        public void ParseHave()
        {
            // u32 len, 1b type, u32 piece_idx
            var buffer = new byte[] { 0, 0, 0, 5, 4, 0, 0, 0, 100 };

            var result = ProtocolSerialization.TryParseMessage(new ReadOnlySequence<byte>(buffer));
            Assert.IsTrue(result.HasValue);
            Assert.IsInstanceOf(typeof(Have), result!.Value.Message);
            Assert.AreEqual(100u, ((Have)result.Value.Message).PieceIndex);
        }

        [Test]
        public void FailParsingIncompleteHave()
        {
            // 2 bytes short
            var buffer = new byte[] { 0, 0, 0, 5, 4, 0, 0 };

            var result = ProtocolSerialization.TryParseMessage(new ReadOnlySequence<byte>(buffer));
            Assert.IsFalse(result.HasValue);
        }

        [Test]
        public void ParseHaveWithRemainder()
        {
            var buffer = new byte[] { 0, 0, 0, 5, 4, 0, 0, 0, 100, 20, 30 };

            var result = ProtocolSerialization.TryParseMessage(new ReadOnlySequence<byte>(buffer));
            Assert.IsTrue(result.HasValue);
            Assert.AreEqual(9, result!.Value.Position.GetInteger());
        }
    }
}
