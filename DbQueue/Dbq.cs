using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DbQueue
{
    public class Dbq : IDbqBoth
    {
        public Dbq(IDbqDatabase database, IDbqBlobStorage blobStorage, DbqSettings? settings = null)
        {
            _database = database;
            _blobStorage = blobStorage;
            _settings = settings ?? new();
            StackMode = _settings.StackMode;
        }

        readonly IDbqDatabase _database;
        readonly IDbqBlobStorage _blobStorage;
        readonly DbqSettings _settings;

        public bool StackMode { get; set; }

        public async Task Push(IEnumerable<string> queues, IAsyncEnumerator<byte[]> data, string? type = null, DateTime? availableAfter = null, DateTime? removeAfter = null, CancellationToken cancellationToken = default)
        {
            availableAfter = availableAfter?.ToUniversalTime();
            removeAfter = removeAfter?.ToUniversalTime();

            if (removeAfter <= (availableAfter ?? DateTime.UtcNow))
                return;

            queues = queues.Select(NormQueueName).Distinct().ToList();

            if (!queues.Any())
                throw new ArgumentException($"Need to specify the target {QueueStack()}");

            using var ms = new MemoryStream();
            var isBlob = false;

            while (await data.MoveNextAsync())
            {
                ms.Write(data.Current);

                if (ms.Position >= _settings.MinBlobSize)
                {
                    isBlob = true;
                    break;
                }
            }

            var dbData = ms.ToArray();

            if (isBlob)
                dbData = GetBytes(await _blobStorage.Add(Concatenate(dbData, data), cancellationToken));

            try
            {
                await _database.Add(queues, dbData, isBlob, type, availableAfter, removeAfter, cancellationToken);
            }
            catch
            {
                if (isBlob)
                    await _blobStorage.Remove(GetBlobId(dbData));

                throw;
            }
        }

        public Task<long> Count(string queue, CancellationToken cancellationToken = default)
        {
            return _database.Count(NormQueueName(queue), cancellationToken);
        }

        public async Task Clear(string queue, IEnumerable<string>? types = null, CancellationToken cancellationToken = default)
        {
            await foreach (var blobKey in _database.Clear(NormQueueName(queue), types, cancellationToken))
                await _blobStorage.Remove(GetBlobId(blobKey));
        }

        public async Task<IAsyncEnumerator<byte[]>?> Peek(string queue, long index = 0, CancellationToken cancellationToken = default)
        {
            var item = await Get(NormQueueName(queue), index, false, cancellationToken);

            return item == null ? null : GetData(item, cancellationToken);
        }

        public async Task<IDbqAcknowledgement<IAsyncEnumerator<byte[]>>?> Pop(string queue, CancellationToken cancellationToken = default)
        {
            var item = await Get(NormQueueName(queue), 0, !_settings.DisableLocking, cancellationToken);

            if (item == null)
                return null;

            var result = GetData(item, cancellationToken);
            var commited = false;

            return new DbqAck<IAsyncEnumerator<byte[]>>(result,
                commit: async () =>
                {
                    await Remove(item, cancellationToken);
                    commited = true;
                },
                dispose: async () =>
                {
                    if (!commited && item.LockId.HasValue)
                        await _database.Unlock(item.Queue, item.LockId.Value);
                });
        }

        private async Task<DbqDatabaseItem?> Get(string queue, long index, bool withLock, CancellationToken cancellationToken)
        {
            var item = await _database.Get(queue, StackMode, index, withLock, cancellationToken);

            while (item != null && item.RemoveAfter?.ToUniversalTime() < DateTime.UtcNow)
            {
                await Remove(item, cancellationToken);
                item = await _database.Get(queue, StackMode, index, withLock, cancellationToken);
            }

            return item;
        }

        private async Task Remove(DbqDatabaseItem item, CancellationToken cancellationToken)
        {
            if (await _database.Remove(item.Id, cancellationToken) && item.IsBlob)
                await _blobStorage.Remove(GetBlobId(item.Data));
        }

        private async IAsyncEnumerator<byte[]> GetData(DbqDatabaseItem item, CancellationToken cancellationToken)
        {
            if (!item.IsBlob)
                yield return item.Data;
            else
                await foreach (var chunk in _blobStorage.Get(GetBlobId(item.Data), cancellationToken))
                    yield return chunk;
        }

        private static byte[] GetBytes(string blobId) => Encoding.UTF8.GetBytes(blobId);
        private static string GetBlobId(byte[] bytes) => Encoding.UTF8.GetString(bytes);
        private static async IAsyncEnumerator<byte[]> Concatenate(byte[] first, IAsyncEnumerator<byte[]> enumerator)
        {
            try
            {
                yield return first;

                while (await enumerator.MoveNextAsync())
                    yield return enumerator.Current;
            }
            finally
            {
                await enumerator.DisposeAsync();
            }
        }

        private string QueueStack() => StackMode ? "stack" : "queue";

        private string NormQueueName(string queue)
        {
            if (string.IsNullOrWhiteSpace(queue))
                throw new ArgumentException($"Target {QueueStack()} cannot be null or empty");

            return _settings.IgnoreCase ? queue.Trim().ToLower() : queue.Trim();
        }
    }
}
