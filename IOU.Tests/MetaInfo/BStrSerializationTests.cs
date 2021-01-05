using System;
using System.Text;
using NUnit.Framework;

namespace IOU.Tests.MetaInfo
{
	public class BStrSerializationTests
	{
		[Test]
		public void BinaryOutputTest()
		{
			var testValue = "testerino";
			var encoded = new BStr(testValue);

			var got = BEnc.EncodeBuffer(encoded);
			
			Assert.AreEqual((byte)($"{testValue.Length}"[0]), got[0]);
			Assert.AreEqual(':', got[1]);
			Assert.AreEqual(Encoding.UTF8.GetBytes(testValue), got[2..]);
		}

		[Test]
		public void RandomBufferRoundtripTest()
		{
			var rnd = new Random();
			Span<byte> bytes = stackalloc byte[4096];
			rnd.NextBytes(bytes);
			
			var val = new BStr(bytes.ToArray());
			var encoded = BEnc.EncodeBuffer(val);
			
			Assert.AreEqual(4096 + 5, encoded.Length);
			
			Assert.IsTrue(BStr.TryParse(encoded, out var parsed, out var consumedBytes));
			Assert.IsNotNull(parsed);
			Assert.AreEqual(4096 + 5, consumedBytes);
			
			Assert.AreEqual(bytes.ToArray(), parsed!.Value.ToArray());
		}

	}
}
