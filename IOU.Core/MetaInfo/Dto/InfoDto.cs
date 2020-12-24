using System.Collections.Generic;

namespace IOU.MetaInfo
{
    public struct InfoDto
    {
        [MetaInfoProperty("name")]
        public string Name { get; set; }

        [MetaInfoProperty("files")]
        public List<FileDto> Files { get; set; }
    }
}
