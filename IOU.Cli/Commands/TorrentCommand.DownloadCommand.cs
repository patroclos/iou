using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Linq;
using System.Net;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;

using IOU.MetaInfo;
using IOU.Peer;

namespace IOU.Cli {
	public partial class TorrentCommand {
		private class DownloadCommand : BaseCommand {
			public DownloadCommand() : base("download") {
				this.AddArgument(new Argument<FileInfo>("torrent"));
				this.Handler = CommandHandler.Create<FileInfo>(this.Run);
			}

			private async Task Run(FileInfo torrent) {
				var (dto, benc) = await BaseCommand.GetTorrentFileDtoAsync(torrent);
				var infoHash = benc["info"]!.Hash();
				byte[] peerId = CreateRandomPeerId();

				Console.WriteLine($"torrent download v2 hash: {Convert.ToHexString(infoHash)}");

				var endpoints = await DownloadCommand.GetTrackerPeersAsync(dto, infoHash, peerId);

				Console.WriteLine($"endpoints: {endpoints.Length}");

				foreach (var peerEndpoint in endpoints) {
					try {
						using var peer = await PeerConnection.EstablishConnection(peerEndpoint, TimeSpan.FromSeconds(1.5));
						peer.MessageReceived += msg => Console.WriteLine($"{peerEndpoint} => {msg.GetType()}");
						await peer.SendMessage(
								new Handshake(new byte[8], infoHash, peerId)
						);
						await peer.SendMessage(new Unchoke());
						await peer.SendMessage(new Interested());
						await Task.Delay(20000);
					}
					catch (Exception e) {
						Console.ForegroundColor = ConsoleColor.Red;
						Console.Error.WriteLine($"[{peerEndpoint}]: ERR\n{e.Message} {e.StackTrace}\n");
						Console.ResetColor();
						continue;
					}
				}
			}

			private static byte[] CreateRandomPeerId() {
				var peerId = new byte[20];
				Encoding.ASCII.GetBytes("-IO0100-").CopyTo(peerId, 0);
				new Random().NextBytes(peerId.AsSpan().Slice(8));
				return peerId;
			}

			private static async Task<IPEndPoint[]> GetTrackerPeersAsync(TorrentFileDto dto, byte[] infoHash, byte[] peerId) {
				var errors = new List<Exception>();
				foreach (var announcer in dto.AnnounceList.SelectMany(x => x)) {
					var uri = new Uri(announcer);
					if (uri.Scheme != "udp")
						continue;

					var ip = (await Dns.GetHostEntryAsync(uri.Host)).AddressList[0];
					var endpoint = new IPEndPoint(ip, uri.Port);
					var announce = new UdpAnnounce(endpoint, peerId);

					try {
						var result = await announce.AnnounceAsync(infoHash);
						return result.ToArray();
					}
					catch (Exception e) {
						errors.Add(e);
						continue;
					}
				}

				throw new AggregateException("Couldn't get peers from trackers", errors);
			}
		}
	}
}

