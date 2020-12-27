using System;
using System.Threading.Tasks;
using System.IO;
using System.CommandLine;
using System.CommandLine.Invocation;
using IOU.MetaInfo;
using IOU.DHT;
using System.Net;
using System.Linq;
using System.Text;

namespace IOU.Cli
{
    class Program
    {
        static Task<int> Main(string[] args)
            => CreateRootCommand().InvokeAsync(args);

        private static RootCommand CreateRootCommand()
        {
            var torrentOpt =
                new Option<FileInfo>(
                        new[] { "--torrent", "-t" },
                        "The .torrent file to operate on"
                )
                { IsRequired = true };

            var prog = new Program();
            var rootCmd = new RootCommand("IOU torrent CLI");

            var torrentCommand = new Command("torrent", "Handle .torrent files");

            var summaryCommand = new Command("summary", "Summarize torrent info");
            summaryCommand.AddArgument(new Argument<FileInfo>("torrent"));
            summaryCommand.Handler = CommandHandler.Create<FileInfo>(prog.SummarizeTorrent);

            var downloadCommand = new Command("download", "Download torrent files");
            downloadCommand.AddArgument(new Argument<FileInfo>("torrent"));
            downloadCommand.AddOption(
                new Option<FileInfo>(new[] { "--output", "-o" }, "Target File/Directory")
                { IsRequired = false }
            );
            downloadCommand.Handler = CommandHandler.Create<FileInfo, FileInfo>(prog.Download);
            torrentCommand.AddCommand(summaryCommand);
            torrentCommand.AddCommand(downloadCommand);

            var dhtCommand = new Command("dht", "Dump DHT info");
            dhtCommand.Handler = CommandHandler.Create(async () =>
            {
                var ip = await AskPublicIpAddressAsync();
                Console.WriteLine($"IP: {ip}");
            });

            rootCmd.AddCommand(torrentCommand);
            rootCmd.AddCommand(dhtCommand);

            return rootCmd;
        }

        async Task SummarizeTorrent(FileInfo torrent)
        {
            var content = await File.ReadAllBytesAsync(torrent.FullName);
            if (!BEnc.TryParseExpr(content, out var expr, out int read)
                    || read != content.Length
                    || expr == null)
            {
                Console.WriteLine($"Something went wrong reading {torrent.FullName} at offset {read}");
                return;
            }

            var fileInfo = MetaInfoSerializer.Deserialize<TorrentFileDto>(expr);
            // Console.WriteLine(expr);
            Console.WriteLine($"Summary for '{fileInfo.Info.Name}'");
            var files = fileInfo.Info.Files;
            if (files != null)
            {
                var maxNameLen = files.Select(f => string.Join('/', f.Path)).Max(name => name.Length);
                foreach (var file in files)
                {
                    var path = string.Join('/', file.Path);
                    Console.WriteLine($"{path.PadRight(maxNameLen + 2)} {Utils.FormatBytesize(file.Length)}");
                }
            }
            Console.WriteLine($"Trackers:\n{string.Join("\n", fileInfo.AnnounceList.Select(t => string.Join(", ", t)))}");
        }

        async Task Download(FileInfo torrent, FileInfo output)
        {
            var content = await File.ReadAllBytesAsync(torrent.FullName);
            if (!BEnc.TryParseExpr(content, out var expr, out int read)
                    || read != content.Length
                    || expr == null)
            {
                Console.WriteLine($"Something went wrong reading {torrent.FullName} at offset {read}");
                return;
            }

            var fileInfo = MetaInfoSerializer.Deserialize<TorrentFileDto>(expr);

            foreach (var tier in fileInfo.AnnounceList)
            {
                foreach (var announcer in tier)
                {
                    try
                    {
                        var uri = new Uri(announcer);
                        if (uri.Scheme != "udp")
                            continue;

                        var ip = (await Dns.GetHostEntryAsync(uri.Host)).AddressList[0];
                        Console.WriteLine($"[announce] {uri} ({ip})");
                        var announce = new UdpAnnounce(new IPEndPoint(ip, uri.Port), Encoding.ASCII.GetBytes("-AZ2200-6wfG2wk6wWLc"));
                        var result = await announce.AnnounceAsync(expr["info"]!.Hash());

                        foreach (var peerIp in result)
                        {
                            Console.WriteLine($"Peer: {peerIp}");
                        }
                    }
                    catch (Exception e)
                    {
                        Console.Error.WriteLine(e.Message);
                        continue;
                    }
                }
            }

            // TODO: dto -> model
            // TODO: announce -> stream endpoints to "download manager"?
            throw new NotImplementedException();
        }

        async static Task<NodeId> CreatePublicIpNodeId()
        {
            var ip = await AskPublicIpAddressAsync();

            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);

            writer.Write($"{ip} (IOU)");
            stream.Seek(0, SeekOrigin.Begin);

            using var sha = System.Security.Cryptography.SHA1.Create();
            return new NodeId(await sha.ComputeHashAsync(stream));
        }

        async static Task<IPAddress> AskPublicIpAddressAsync()
        {
            var req = WebRequest.CreateHttp("https://api.ipify.org");

            using var response = await req.GetResponseAsync();
            using var stream = response.GetResponseStream();
            using var reader = new StreamReader(stream);

            var text = await reader.ReadToEndAsync();
            return IPAddress.Parse(text);
        }
    }
}
