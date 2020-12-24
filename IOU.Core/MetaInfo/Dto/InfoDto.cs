using System;
using System.Collections.Generic;

namespace IOU.MetaInfo
{
    public struct InfoDto
    {
        [MetaInfoProperty("name")]
        public string Name { get; set; }

        [MetaInfoProperty("piece length")]
        public int PieceLength { get; set; }

        [MetaInfoProperty("pieces")]
        public ReadOnlyMemory<byte> Pieces { get; set; }

        [MetaInfoProperty("files")]
        public List<FileDto>? Files { get; set; }

        [MetaInfoProperty("length")]
        public long? Length { get; set; }

        [MetaInfoProperty("path")]
        public string[] Path { get; set; }
    }
}
