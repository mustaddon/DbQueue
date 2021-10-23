using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace DbQueue
{
    public static partial class DbqExtensions
    {
        public static async Task<T?> Peek<T>(this IDbQueue dbq,
               string queue, long index = 0, CancellationToken cancellationToken = default)
        {
            return await dbq.Peek(queue, index, cancellationToken).ConvertTo<T>();
        }

        public static async IAsyncEnumerable<IAsyncEnumerator<byte[]>> PeekMany(this IDbQueue dbq,
                string queue, long? count = null, long skip = 0, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            count = skip + count;
            for (var i = skip; count == null || i < count; i++)
                await using (var enumerator = await dbq.Peek(queue, i, cancellationToken))
                {
                    if (enumerator == null)
                        break;

                    yield return enumerator;
                }
        }

        public static async IAsyncEnumerable<T?> PeekMany<T>(this IDbQueue dbq,
               string queue, long? count = null, long skip = 0, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await foreach (var item in PeekMany(dbq, queue, count, skip, cancellationToken))
                yield return await item.ConvertTo<T>();
        }

    }
}
