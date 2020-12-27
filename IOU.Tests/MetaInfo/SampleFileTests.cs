using System;
using System.IO;
using System.Threading.Tasks;
using IOU.MetaInfo;
using NUnit.Framework;

namespace IOU.Tests.MetaInfo
{
    public class SampleFileTests
    {
        // private readonly AnnounceTests _announceTests = new AnnounceTests();
        public const string SAMPLE_TORRENT = "../../../Samples/testerino.torrent";
        
        [Test]
        public async Task SampleTorrentFileRoundtripTest()
        {
            var content = await File.ReadAllBytesAsync(SAMPLE_TORRENT);
            Assert.IsTrue(BEnc.TryParseExpr(content.AsSpan(), out var value, out var consumed));
            Assert.AreEqual(content.Length, consumed);
            Assert.IsNotNull(value);
            Console.WriteLine(value!.ToString());

            var recoded = BEnc.EncodeBuffer(value);
            
            Assert.AreEqual(content, recoded);
            
        }

        [Test]
        public async Task DeserializeToDtoTest()
        {
            var content = await File.ReadAllBytesAsync(SAMPLE_TORRENT);
            Assert.IsTrue(BEnc.TryParseExpr(content.AsSpan(), out var value, out var consumed));
            Assert.AreEqual(content.Length, consumed);
            Assert.IsNotNull(value);

            var file = MetaInfoSerializer.Deserialize<TorrentFileDto>(value!);
            Assert.IsNotNull(file);

            var info = file.Info;
            
            Assert.IsNotNull(info);
            Assert.IsNotNull(info.Files);
            Assert.IsNotEmpty(info.Files);
            Assert.IsNotNull(info.Files![0].Path);
        }
    }
}
