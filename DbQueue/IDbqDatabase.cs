using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DbQueue
{
    public interface IDbqDatabase
    {
        Task Add(IEnumerable<string> queues, byte[] data, bool isBlob, CancellationToken cancellationToken = default);
        Task<IDbqDatabaseItem?> Get(string queue, bool desc = false, long index = 0, bool withLock = false, CancellationToken cancellationToken = default);
        Task<long> Count(string queue, CancellationToken cancellationToken = default);
        Task Unlock(string queue, long lockid, CancellationToken cancellationToken = default);
        Task<bool> Remove(string key, CancellationToken cancellationToken = default);
        IAsyncEnumerable<byte[]> Clear(string queue, CancellationToken cancellationToken = default);
    }

    public class IDbqDatabaseItem
    {
        public string Key { get; set; } = string.Empty;
        public byte[] Data { get; set; } = Dbq.BytesEmpty;
        public bool IsBlob { get; set; }
        public long? LockId { get; set; }

        public override int GetHashCode() => Key.GetHashCode();
        public override bool Equals(object obj) => Key == (obj as IDbqDatabaseItem)?.Key;
    }
}
