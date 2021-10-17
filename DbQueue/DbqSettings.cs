namespace DbQueue
{
    public class DbqSettings
    {
        public bool StackMode { get; set; } = false;
        public int MinBlobSize { get; set; } = 4096;
        public bool DisableLocking { get; set; } = false;
    }
}
