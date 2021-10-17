﻿using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace DbQueue.EntityFrameworkCore
{
    public class DbqDatabase : IDbqDatabase
    {
        public DbqDatabase(DbqDbSettings? settings = null)
        {
            _settings = settings ?? new();
            _context = new(_settings.ContextConfigurator);
        }

        readonly DbqDbContext _context;
        readonly DbqDbSettings _settings;

        public async Task Add(IEnumerable<string> queues, byte[] data, bool isBlob, CancellationToken cancellationToken = default)
        {
            foreach (var queue in queues)
                await _context.DbQueue.AddAsync(new()
                {
                    Queue = queue,
                    Data = data,
                    IsBlob = isBlob,
                }, cancellationToken);

            await _context.SaveChangesAsync(cancellationToken);
        }

        public Task<IDbqDatabaseItem?> Get(string queue, bool desc = false, long index = 0, bool withLock = false, CancellationToken cancellationToken = default)
        {
            return WithRetry(_settings.LockRetries, async (i) =>
            {
                var lockid = withLock ? DateTime.Now.Ticks : (long?)null;

                var entity = await _context.DbQueue
                    .Where(x => x.Queue == queue)
                    .Where(IsUnlocked(lockid))
                    .OrderBy(x => x.Id, desc)
                    .Skip((int)index)
                    .FirstOrDefaultAsync(cancellationToken);

                if (entity == null)
                    return null;

                if (lockid.HasValue)
                {
                    entity.LockId = lockid;
                    await _context.SaveChangesAsync(cancellationToken);
                }

                return Map(entity);
            });
        }

        public Task<long> Count(string queue, CancellationToken cancellationToken = default)
        {
            return _context.DbQueue.Where(x => x.Queue == queue).LongCountAsync(cancellationToken);
        }

        public async Task<bool> Remove(string key, CancellationToken cancellationToken = default)
        {
            var id = long.Parse(key);

            var entity = await _context.DbQueue.SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
                ?? throw new KeyNotFoundException();

            _context.Remove(entity);

            await _context.SaveChangesAsync(cancellationToken);

            // completely
            return !entity.IsBlob
                || !await _context.DbQueue.AnyAsync(x => x.Data == entity.Data, cancellationToken);
        }

        public async IAsyncEnumerable<byte[]> Clear(string queue, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var batchSize = 1000;

            while (true)
            {
                var entities = await _context.DbQueue.Take(batchSize).ToListAsync(cancellationToken);

                _context.RemoveRange(entities);

                await _context.SaveChangesAsync(cancellationToken);

                foreach (var entity in entities)
                    if (entity.IsBlob)
                        yield return entity.Data;

                if (entities.Count < batchSize)
                    break;
            }
        }

        public async Task Unlock(string queue, long lockid, CancellationToken cancellationToken = default)
        {
            var entities = await _context.DbQueue
                .Where(x => x.Queue == queue && x.LockId == lockid)
                .ToListAsync(cancellationToken);

            if (!entities.Any())
                return;

            foreach (var entity in entities)
                entity.LockId = null;

            await _context.SaveChangesAsync(cancellationToken);
        }

        private IDbqDatabaseItem Map(DbItem entity)
        {
            return new IDbqDatabaseItem()
            {
                Key = entity.Id.ToString(),
                Data = entity.Data,
                IsBlob = entity.IsBlob,
                LockId = entity.LockId,
            };
        }

        private Expression<Func<DbItem, bool>> IsUnlocked(long? lockid)
        {
            var autoUnlock = DateTime.Now.Add(-_settings.AutoUnlockDelay).Ticks;
            return x => x.LockId == null || x.LockId == lockid || x.LockId < autoUnlock;
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

        internal static readonly byte[] BytesEmpty = new byte[0];
    }
}