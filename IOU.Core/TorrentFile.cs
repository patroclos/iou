using System.Linq;

namespace IOU
{
    public class TorrentFile
    {
        public BInt Length { get; private set; }

        public BStr[] Path { get; private set; }

        public TorrentFile(BInt length, BStr[] path)
        {
            Length = length;
            Path = path;
        }

        public static bool TryParseInfo(BEnc enc, out TorrentFile? file)
        {
            file = default;
            
            if (!(enc is BDict d))
                return false;
            var len = d["length"];
            if (len == null)
                return false;

            if (!(len is BInt lenI))
                return false;

            var pth = d["path"];
            if (pth == null)
                return false;

            if (!(pth is BLst pl) || pl.Value.Any(x => !(x is BStr)))
                return false;

            var elems = pl.Value.OfType<BStr>().ToArray();

            file = new TorrentFile(lenI, elems);
            
            return true;
        }

        public override string ToString()
        {
            return $"{nameof(Length)}: {Length}, {nameof(Path)}: {string.Join("/", Path.Select(s=>s.Utf8String))}";
        }
    }
}
