﻿using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace DbQueue.MongoDB
{
    internal class MongoItem
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = string.Empty;
        public string Queue { get; set; } = string.Empty;
        public byte[] Data { get; set; } = DbqDatabase.BytesEmpty;
        public long Hash { get; set; }
        public bool IsBlob { get; set; }
        public long? LockId { get; set; }

        public override int GetHashCode() => Id.GetHashCode();
        public override bool Equals(object obj) => Id == (obj as MongoItem)?.Id;
    }
}