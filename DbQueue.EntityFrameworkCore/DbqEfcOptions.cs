namespace DbQueue.EntityFrameworkCore
{
    public class DbqEfcOptions
    {
        public DbqSettings Queue { get; } = new();
        public DbqDbSettings Database { get; } = new();
        public DbqBlobStorageSettings BlobStorage { get; } = new();
    }
}
