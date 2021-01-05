using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;

namespace IOU.Peer
{
	public class PeerConnectionTests
	{
		[Test]
		public async Task Handshake_ParsedOutputEqualsInput()
		{
			using var rcv = new MemoryStream();
			using var trx = new MemoryStream();

			using var connection = new PeerConnection(rcv, trx);

			var handshake = new Handshake(
					Enumerable.Repeat<byte>(0xff, 8).ToArray(),
					Enumerable.Repeat<byte>(0x41, 20).ToArray(),
					Enumerable.Repeat<byte>(0x42, 20).ToArray()
					);
			var buf = new byte[68];
			await connection.SendMessage(handshake);

			trx.Seek(0, SeekOrigin.Begin);
			var readBytes = await trx.ReadAsync(buf, 0, 68);
			Assert.AreEqual(68, readBytes);
			Assert.IsTrue(buf.SequenceEqual(handshake.ToByteArray()));

			var parsed = Handshake.TryParse(buf);
			Assert.IsNotNull(parsed);
			Assert.AreEqual(handshake, parsed);
		}
	}
}
