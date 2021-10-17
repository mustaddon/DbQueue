﻿using DbQueue.Abstractions;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DbQueue
{
    public class Dbq : IDbQueue
    {
        public Dbq(IDbqDatabase database, IDbqBlobStorage blobStorage, DbqSettings? settings = null)
        {
            _database = database;
            _blobStorage = blobStorage;
            _settings = settings ?? new();
        }

        readonly IDbqDatabase _database;
        readonly IDbqBlobStorage _blobStorage;
        readonly DbqSettings _settings;

        public async Task Push(IEnumerable<string> queues, IAsyncEnumerator<byte[]> data, CancellationToken cancellationToken = default)
        {
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
                await _database.Add(queues, dbData, isBlob, cancellationToken);
            }
            catch
            {
                if (isBlob) 
                    await _blobStorage.Delete(GetBlobId(dbData));

                throw;
            }
        }

        public Task<long> Count(string queue, CancellationToken cancellationToken = default)
        {
            return _database.Count(queue, cancellationToken);
        }

        public async Task Clear(string queue, CancellationToken cancellationToken = default)
        {
            await foreach (var blobKey in _database.Clear(queue, cancellationToken))
                await _blobStorage.Delete(GetBlobId(blobKey));
        }

        public async IAsyncEnumerable<byte[]> Pop(string queue, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var item = await _database.Get(queue, _settings.StackMode,
                withLock: !_settings.DisableLocking,
                cancellationToken: cancellationToken);

            var complete = false;

            if (item != null)
                try
                {
                    await foreach (var chunk in GetData(item, cancellationToken))
                        yield return chunk;

                    if (await _database.Remove(item.Key, cancellationToken) && item.IsBlob)
                        await _blobStorage.Delete(GetBlobId(item.Data));

                    complete = true;
                }
                finally
                {
                    if (!complete && item.LockId.HasValue)
                        await _database.Unlock(queue, item.LockId.Value);
                }
        }

        public async IAsyncEnumerable<byte[]> Peek(string queue, long index = 0, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var item = await _database.Get(queue, _settings.StackMode,
                index: index,
                cancellationToken: cancellationToken);

            if (item != null)
                await foreach (var chunk in GetData(item, cancellationToken))
                    yield return chunk;
        }

        private async IAsyncEnumerable<byte[]> GetData(IDbqDatabaseItem item, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var lastLength = 0;

            if (!item.IsBlob)
            {
                lastLength = item.Data.Length;
                yield return item.Data;
            }
            else
                await foreach (var chunk in _blobStorage.Get(GetBlobId(item.Data), cancellationToken))
                {
                    lastLength = chunk.Length;
                    yield return chunk;
                }

            if (lastLength > 0)
                yield return BytesEmpty;
        }


        internal static readonly byte[] BytesEmpty = new byte[0];
        private static byte[] GetBytes(string blobId) => Encoding.UTF8.GetBytes(blobId);
        private static string GetBlobId(byte[] bytes) => Encoding.UTF8.GetString(bytes);
        private static async IAsyncEnumerator<byte[]> Concatenate(byte[] first, IAsyncEnumerator<byte[]> data)
        {
            yield return first;
            while (await data.MoveNextAsync())
                yield return data.Current;
        }
    }
}