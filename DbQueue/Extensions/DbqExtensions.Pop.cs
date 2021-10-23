using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace DbQueue
{
    public static partial class DbqExtensions
    {
        public static async Task<IDbqAcknowledgement<T?>?> Pop<T>(this IDbQueue dbq,
               string queue, CancellationToken cancellationToken = default)
        {
            var ack = await dbq.Pop(queue, cancellationToken);
            return ack == null ? null : await ack.ConvertTo<T>();
        }

        public static async IAsyncEnumerable<IDbqAcknowledgement<IAsyncEnumerator<byte[]>>> PopMany(this IDbQueue dbq,
                   string queue, long? count = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            for (var i = 0L; count == null || i < count; i++)
                await using (var ack = await dbq.Pop(queue, cancellationToken))
                {
                    if (ack == null)
                        break;

                    yield return ack;
                }
        }

        public static async IAsyncEnumerable<IDbqAcknowledgement<T?>> PopMany<T>(this IDbQueue dbq,
                string queue, long? count = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await foreach (var ack in PopMany(dbq, queue, count, cancellationToken))
                yield return await ack.ConvertTo<T>();
        }

    }
}
