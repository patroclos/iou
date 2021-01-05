using System;
using NUnit.Framework;

namespace IOU.Tests
{
	public class HexDumpTests
	{
		[Test]
		public void Blub()
		{
			var dump = Utils.HexDump(new byte[] { 0x41, 0x42, 0x43, 0x44 }, 2);
			Assert.AreEqual("00000000   41 42   AB\n00000002   43 44   CD\n", dump);
			Console.WriteLine(dump);
		}
	}
}
