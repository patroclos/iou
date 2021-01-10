using System.Collections.Generic;

namespace IOU.MetaInfo {
	public struct TorrentFileDto {
		[MetaInfoProperty("announce")]
		public string Announce { get; set; }

		[MetaInfoProperty("announce-list")]
		public List<string[]> AnnounceList { get; set; }

		[MetaInfoProperty("info")]
		public InfoDto Info { get; set; }
	}
}
