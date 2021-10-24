using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DbQueue
{
    public interface IDbQueue
    {
        Task Push(IEnumerable<string> queues, IAsyncEnumerator<byte[]> data, string? type = null, DateTime? availableAfter = null, DateTime? removeAfter = null, CancellationToken cancellationToken = default);
        Task<IAsyncEnumerator<byte[]>?> Peek(string queue, long index = 0, CancellationToken cancellationToken = default);
        Task<IDbqAcknowledgement<IAsyncEnumerator<byte[]>>?> Pop(string queue, CancellationToken cancellationToken = default);
        Task<long> Count(string queue, CancellationToken cancellationToken = default);
        Task Clear(string queue, IEnumerable<string>? types = null, CancellationToken cancellationToken = default);
    }
}
