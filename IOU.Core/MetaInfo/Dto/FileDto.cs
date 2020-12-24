namespace IOU.MetaInfo
{
    public struct FileDto
    {
        [MetaInfoProperty("length")]
        public long Length { get; set; }

        [MetaInfoProperty("path")]
        public string[] Path { get; set; }
    }
}
