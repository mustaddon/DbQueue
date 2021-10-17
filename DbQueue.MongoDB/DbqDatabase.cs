using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace DbQueue.MongoDB
{
    public class DbqDatabase : IDbqDatabase
    {
        public DbqDatabase(DbqDbSettings? settings = null)
        {
            _settings = settings ?? new();

            _dbItems = new(() =>
            {
                var client = new MongoClient(_settings.ConnectionString);
                var database = client.GetDatabase(_settings.DatabaseName, _settings.DatabaseSettings);
                return database.GetCollection<MongoItem>(_settings.CollectionName, _settings.CollectionSettings);
            });
        }

        readonly DbqDbSettings _settings;
        readonly Lazy<IMongoCollection<MongoItem>> _dbItems;


        public async Task Add(IEnumerable<string> queues, byte[] data, bool isBlob, CancellationToken cancellationToken = default)
        {
            await _dbItems.Value.InsertManyAsync(queues.Select(queue => new MongoItem
            {
                Queue = queue,
                IsBlob = isBlob,
                Data = data,
                Hash = GetHash(data),
            }), null, cancellationToken);
        }

        public Task<IDbqDatabaseItem?> Get(string queue, bool desc = false, long index = 0, bool withLock = false, CancellationToken cancellationToken = default)
        {
            return WithRetry(_settings.LockRetries, async i =>
            {
                var lockid = withLock ? DateTime.Now.Ticks : (long?)null;
                var autoUnlock = DateTime.Now.Add(-_settings.AutoUnlockDelay).Ticks;

                var entity = await _dbItems.Value
                    .Find(x => x.Queue == queue
                        && (x.LockId == null || x.LockId == lockid || x.LockId < autoUnlock))
                    .Sort(desc ? Builders<MongoItem>.Sort.Descending(x => x.Id) : Builders<MongoItem>.Sort.Ascending(x => x.Id))
                    .Skip((int)index)
                    .FirstOrDefaultAsync(cancellationToken);

                if (entity == null)
                    return null;

                if (lockid.HasValue)
                {
                    var result = await _dbItems.Value.UpdateOneAsync(
                        filter: x => x.Id == entity.Id && x.LockId == entity.LockId,
                        update: Builders<MongoItem>.Update.Set(x => x.LockId, lockid),
                        cancellationToken: cancellationToken);

                    if (result.ModifiedCount == 0)
                        throw new Exception(LockFailed);
                }

                return new IDbqDatabaseItem()
                {
                    Key = entity.Id,
                    Queue = entity.Queue,
                    Data = entity.Data,
                    IsBlob = entity.IsBlob,
                    LockId = lockid,
                };
            });
        }

        public async Task Unlock(string queue, long lockid, CancellationToken cancellationToken = default)
        {
            await _dbItems.Value.UpdateManyAsync(
                filter: x => x.Queue == queue && x.LockId == lockid,
                update: Builders<MongoItem>.Update.Set(x => x.LockId, null),
                cancellationToken: cancellationToken);
        }

        public Task<long> Count(string queue, CancellationToken cancellationToken = default)
        {
            return _dbItems.Value.CountDocumentsAsync(x => x.Queue == queue, null, cancellationToken);
        }


        public async Task<bool> Remove(string key, CancellationToken cancellationToken = default)
        {
            var entity = await _dbItems.Value.FindOneAndDeleteAsync(x => x.Id == key, null, cancellationToken)
                ?? throw new KeyNotFoundException();

            // completely
            return !entity.IsBlob
                || 0 == await _dbItems.Value.CountDocumentsAsync(x => x.Hash == entity.Hash, null, cancellationToken);
        }

        public async IAsyncEnumerable<byte[]> Clear(string queue, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var batchSize = 1000;

            while (true)
            {
                var entities = await _dbItems.Value
                    .Find(x => x.Queue == queue)
                    .Limit(batchSize)
                    .ToListAsync(cancellationToken);

                var ids = entities.Select(x => x.Id).ToArray();
                await _dbItems.Value.DeleteManyAsync(x => ids.Contains(x.Id), cancellationToken);

                var blobs = entities.Where(x => x.IsBlob).GroupBy(x => x.Hash).ToDictionary(g => g.Key, g => g.First().Data);

                if (blobs.Any())
                {
                    var busy = new HashSet<long>(await _dbItems.Value
                        .Find(x => x.IsBlob && blobs.Keys.Contains(x.Hash))
                        .Project(x => x.Hash)
                        .ToListAsync(cancellationToken));

                    foreach (var kvp in blobs)
                        if (!busy.Contains(kvp.Key))
                            yield return kvp.Value;
                }

                if (entities.Count < batchSize)
                    break;
            }
        }

        private static async Task<T?> WithRetry<T>(int retries, Func<int, Task<T>> task)
        {
            for (var i = 0; i <= retries; i++)
                try
                {
                    return await task(i);
                }
                catch
                {
                    if (i == retries) throw;
                }

            return default;
        }

        private static long GetHash(byte[] data)
        {
            using var sha = SHA256.Create();
            return BitConverter.ToInt64(sha.ComputeHash(data));
        }

        private static readonly string LockFailed = "Failed to lock queue item";
        internal static readonly byte[] BytesEmpty = new byte[0];
    }
}
