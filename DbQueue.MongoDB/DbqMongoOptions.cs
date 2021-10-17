namespace DbQueue.MongoDB
{
    public class DbqMongoOptions
    {
        public DbqSettings Queue { get; } = new();
        public DbqDbSettings Database { get; } = new();
        public DbqBlobStorageSettings BlobStorage { get; } = new();
    }
}
