namespace DbQueue
{
    public class DbqSettings
    {
        public int MinBlobSize { get; set; } = 4096;
        public bool DisableLocking { get; set; } = false;
        public bool StackMode { get; set; } = false;
        public bool IgnoreCase { get; set; } = true;
    }
}
