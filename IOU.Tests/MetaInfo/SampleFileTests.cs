using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using NUnit.Framework;

namespace IOU.Tests.MetaInfo
{
    public class SampleFileTests
    {
        private readonly AnnounceTests _announceTests = new AnnounceTests();
        public const string SAMPLE_TORRENT = "../../../Samples/testerino.torrent";
        
        [Test]
        public async Task SampleTorrentFileRoundtripTest()
        {
            var content = await File.ReadAllBytesAsync(SAMPLE_TORRENT);
            Assert.IsTrue(BEnc.TryParseExpr(content.AsSpan(), out var value, out var consumed));
            Assert.AreEqual(content.Length, consumed);
            Assert.IsNotNull(value);
            Console.WriteLine(value.ToString());

            var recoded = BEnc.EncodeBuffer(value);
            
            Assert.AreEqual(content, recoded);
            
        }

        struct InfoDto
        {
            [MetaInfoProperty("files")]
            public List<SampleFileTests.FileDto> Files { get; set; }

            public override string ToString()
            {
                return $"{nameof(Files)}: {string.Join(", ", Files)}";
            }
        }

        struct FileDto
        {
            [MetaInfoProperty("length")]
            public long Length { get; set; }
            [MetaInfoProperty("path")]
            public string[] Path { get; set; }

            public override string ToString()
            {
                return $"{nameof(Length)}: {Length}, {nameof(Path)}: {string.Join("/",Path)}";
            }
        }
        
        [Test]
        public async Task DeserializeToDtoTest()
        {
            var content = await File.ReadAllBytesAsync(SAMPLE_TORRENT);
            Assert.IsTrue(BEnc.TryParseExpr(content.AsSpan(), out var value, out var consumed));
            Assert.AreEqual(content.Length, consumed);
            Assert.IsNotNull(value);
            
            var info = MetaInfoSerializer.Deserialize<SampleFileTests.InfoDto>(((BDict) value)["info"]!);
            Assert.IsNotNull(info);
            Assert.IsNotNull(info.Files);
            Assert.IsNotEmpty(info.Files);
            Assert.IsNotNull(info.Files[0].Path);
        }
    }
}