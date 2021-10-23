using System.Collections.Generic;
using System.Threading.Tasks;

namespace DbQueue
{
    public static partial class DbqExtensions
    {
        public static async Task<T?> WithAutoAck<T>(this Task<IDbqAcknowledgement<T?>?> ackTask)
        {
            var ack = await ackTask;
            return ack == null ? default : await ack.WithAutoAck();
        }

        public static async IAsyncEnumerable<T?> WithAutoAck<T>(this IAsyncEnumerable<IDbqAcknowledgement<T?>> acks)
        {
            await foreach (var ack in acks)
                yield return await ack.WithAutoAck();
        }

        public static async Task<T?> WithAutoAck<T>(this IDbqAcknowledgement<T?> ack)
        {
            var stream = ack.Data as DbqStream;

            if (stream == null)
                await ack.Commit();
            else
            {
                stream.OnComplete += (s, e) => ack.Commit().Wait();
                stream.OnDispose += (s, e) => ack.Dispose();
            }

            return ack.Data;
        }

        public static async IAsyncEnumerable<IAsyncEnumerator<byte[]>> WithAutoAck(this IAsyncEnumerable<IDbqAcknowledgement<IAsyncEnumerator<byte[]>>> acks)
        {
            await foreach (var ack in acks)
            {
                var enumerator = AckEnumeratorWrapper(ack);
                await enumerator.MoveNextAsync();
                yield return enumerator;
            }
        }

        public static async Task<IAsyncEnumerator<byte[]>?> WithAutoAck(this Task<IDbqAcknowledgement<IAsyncEnumerator<byte[]>>?> ackTask)
        {
            var ack = await ackTask;
            if (ack == null) return null;
            var enumerator = AckEnumeratorWrapper(ack);
            await enumerator.MoveNextAsync();
            return enumerator;
        }

        private static async IAsyncEnumerator<byte[]> AckEnumeratorWrapper(IDbqAcknowledgement<IAsyncEnumerator<byte[]>> ack)
        {
            try
            {
                yield return BytesEmpty;

                while (await ack.Data.MoveNextAsync())
                    yield return ack.Data.Current;

                await ack.Commit();
            }
            finally
            {
                await ack.DisposeAsync();
            }
        }

        private static readonly byte[] BytesEmpty = new byte[0];
    }
}
