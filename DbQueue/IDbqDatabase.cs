using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DbQueue
{
    public interface IDbqDatabase
    {
        Task Add(IEnumerable<string> queues, byte[] data, bool isBlob, string? type = null, DateTime? availableAfter = null, DateTime? removeAfter = null, CancellationToken cancellationToken = default);
        Task<DbqDatabaseItem?> Get(string queue, bool desc = false, long index = 0, bool withLock = false, CancellationToken cancellationToken = default);
        Task<long> Count(string queue, CancellationToken cancellationToken = default);
        Task Unlock(string queue, long lockid, CancellationToken cancellationToken = default);
        Task<bool> Remove(string key, CancellationToken cancellationToken = default);
        IAsyncEnumerable<byte[]> Clear(string queue, IEnumerable<string>? types = null, CancellationToken cancellationToken = default);
    }

    public class DbqDatabaseItem
    {
        public string Id { get; set; } = string.Empty;
        public string Queue { get; set; } = string.Empty;
        public byte[] Data { get; set; } = BytesEmpty;
        public bool IsBlob { get; set; }
        public long? LockId { get; set; }
        public DateTime? RemoveAfter { get; set; }

        public override int GetHashCode() => Id.GetHashCode();
        public override bool Equals(object? obj) => string.Equals(Id, (obj as DbqDatabaseItem)?.Id);

        private static readonly byte[] BytesEmpty = new byte[0];
    }
}
