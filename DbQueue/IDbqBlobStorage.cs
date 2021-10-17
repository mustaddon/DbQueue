using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DbQueue
{
    public interface IDbqBlobStorage
    {
        Task<string> Add(IAsyncEnumerator<byte[]> data, CancellationToken cancellationToken = default);
        IAsyncEnumerable<byte[]> Get(string key, CancellationToken cancellationToken = default);
        Task Delete(string key, CancellationToken cancellationToken = default);
    }
}
