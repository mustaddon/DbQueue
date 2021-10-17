using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DbQueue.Abstractions
{
    public interface IDbQueue
    {
        Task Push(IEnumerable<string> queues, IAsyncEnumerator<byte[]> data, CancellationToken cancellationToken = default);
        Task<IAsyncEnumerator<byte[]>?> Peek(string queue, long index = 0, CancellationToken cancellationToken = default);
        Task<IAsyncEnumerator<byte[]>?> Pop(string queue, CancellationToken cancellationToken = default);
        Task<long> Count(string queue, CancellationToken cancellationToken = default);
        Task Clear(string queue, CancellationToken cancellationToken = default);
    }
}
