using MongoDB.Driver;
using System;

namespace DbQueue.MongoDB
{
    public class DbqDbSettings
    {
        public string ConnectionString { get; set; } = "mongodb://localhost:27017";
        public string DatabaseName { get; set; } = "dbqueue";
        public MongoDatabaseSettings? DatabaseSettings { get; set; }
        public string CollectionName { get; set; } = "queues";
        public MongoCollectionSettings? CollectionSettings { get; set; }
        public TimeSpan AutoUnlockDelay { get; set; } = TimeSpan.FromMinutes(5);
    }
}
