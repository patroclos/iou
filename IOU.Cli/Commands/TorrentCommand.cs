using System;
using System.CommandLine;
using System.IO;
using System.Threading.Tasks;
using IOU.MetaInfo;

namespace IOU.Cli
{
    public partial class TorrentCommand : Command
    {
        private abstract class BaseCommand : Command
        {
            protected BaseCommand(string name, string? description = null) : base(name, description)
            {
            }

            protected static async Task<(TorrentFileDto, BEnc)> GetTorrentFileDtoAsync(FileInfo torrent)
            {
                var content = await File.ReadAllBytesAsync(torrent.FullName);
                if (!BEnc.TryParseExpr(content, out var expr, out var read)
                        || expr == null
                        || read != content.Length)
                    throw new ArgumentOutOfRangeException(nameof(torrent));

                var dto = MetaInfoSerializer.Deserialize<TorrentFileDto>(expr);
                return (dto, expr);
            }
        }

        public TorrentCommand() : base("torrent", "Handle .torrent files")
        {
            this.AddCommand(new DownloadCommand());
        }
    }
}
