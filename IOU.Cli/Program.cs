using System;
using System.Threading.Tasks;
using System.IO;
using System.CommandLine;
using System.CommandLine.Invocation;
using IOU.MetaInfo;
using IOU.DHT;
using System.Net;
using System.Linq;

namespace IOU.Cli {
	class Program {
		static Task<int> Main(string[] args)
			=> CreateRootCommand().InvokeAsync(args);

		private static RootCommand CreateRootCommand() {
			var torrentOpt =
				new Option<FileInfo>(
						new[] { "--torrent", "-t" },
						"The .torrent file to operate on"
				) { IsRequired = true };

			var prog = new Program();
			var rootCmd = new RootCommand("IOU torrent CLI");

			var summaryCommand = new Command("summary", "Summarize torrent info");
			summaryCommand.AddArgument(new Argument<FileInfo>("torrent"));
			summaryCommand.Handler = CommandHandler.Create<FileInfo>(prog.SummarizeTorrent);

			var dhtCommand = new Command("dht", "Dump DHT info");
			dhtCommand.Handler = CommandHandler.Create(async () => {
				var ip = await AskPublicIpAddressAsync();
				Console.WriteLine($"IP: {ip}");
			});

			rootCmd.AddCommand(new TorrentCommand());
			rootCmd.AddCommand(dhtCommand);

			return rootCmd;
		}

		async Task SummarizeTorrent(FileInfo torrent) {
			var content = await File.ReadAllBytesAsync(torrent.FullName);
			if (!BEnc.TryParseExpr(content, out var expr, out int read)
					|| read != content.Length
					|| expr == null) {
				Console.WriteLine($"Something went wrong reading {torrent.FullName} at offset {read}");
				return;
			}

			var fileInfo = MetaInfoSerializer.Deserialize<TorrentFileDto>(expr);
			byte[] infoHash = expr["info"]!.Hash();

			Console.WriteLine($"Summary for '{fileInfo.Info.Name}'");
			Console.WriteLine($"InfoHash: {Convert.ToHexString(infoHash)}");
			var files = fileInfo.Info.Files;
			if (files != null) {
				var maxNameLen = files.Select(f => string.Join('/', f.Path)).Max(name => name.Length);
				foreach (var file in files) {
					var path = string.Join('/', file.Path);
					Console.WriteLine($"{path.PadRight(maxNameLen + 2)} {Utils.FormatBytesize(file.Length)}");
				}
			}
			Console.WriteLine($"Trackers:\n{string.Join("\n", fileInfo.AnnounceList.Select(t => string.Join(", ", t)))}");
		}

		async static Task<NodeId> CreateNodeIdFromPublicIpAsync() {
			var ip = await AskPublicIpAddressAsync();

			using var stream = new MemoryStream();
			using var writer = new BinaryWriter(stream);

			writer.Write($"{ip} (IOU)");
			stream.Seek(0, SeekOrigin.Begin);

			using var sha = System.Security.Cryptography.SHA1.Create();
			return new NodeId(await sha.ComputeHashAsync(stream));
		}

		async static Task<IPAddress> AskPublicIpAddressAsync() {
			var req = WebRequest.CreateHttp("https://api.ipify.org");

			using var response = await req.GetResponseAsync();
			using var stream = response.GetResponseStream();
			using var reader = new StreamReader(stream);

			var text = await reader.ReadToEndAsync();
			return IPAddress.Parse(text);
		}

	}
}
