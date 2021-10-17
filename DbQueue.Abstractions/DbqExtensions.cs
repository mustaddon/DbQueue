using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DbQueue.Abstractions
{
    public static class DbqExtensions
    {

        public static Task Push(this IDbQueue dbq,
               IEnumerable<string> queues, Stream data, CancellationToken cancellationToken = default)
        {
            return dbq.Push(queues, GetAsyncEnumerator(data, cancellationToken), cancellationToken);
        }

        public static Task Push(this IDbQueue dbq,
               IEnumerable<string> queues, byte[] data, CancellationToken cancellationToken = default)
        {
            return dbq.Push(queues, GetChunkEnumerator(data), cancellationToken);
        }

        public static Task Push(this IDbQueue dbq,
               IEnumerable<string> queues, IAsyncEnumerable<byte[]> data, CancellationToken cancellationToken = default)
        {
            return dbq.Push(queues, data.GetAsyncEnumerator(cancellationToken), cancellationToken);
        }

        public static Task Push<T>(this IDbQueue dbq,
               IEnumerable<string> queues, T data, CancellationToken cancellationToken = default)
        {
            if (data is Stream)
                return Push(dbq, queues, (data as Stream) ?? new MemoryStream(), cancellationToken);

            return Push(dbq, queues, Serialize(data), cancellationToken);
        }

        public static Task Push(this IDbQueue dbq,
               string queue, IAsyncEnumerator<byte[]> data, CancellationToken cancellationToken = default)
        {
            return dbq.Push(new[] { queue }, data, cancellationToken);
        }

        public static Task Push(this IDbQueue dbq,
               string queue, Stream data, CancellationToken cancellationToken = default)
        {
            return Push(dbq, new[] { queue }, data, cancellationToken);
        }

        public static Task Push(this IDbQueue dbq,
               string queue, byte[] data, CancellationToken cancellationToken = default)
        {
            return Push(dbq, new[] { queue }, data, cancellationToken);
        }

        public static Task Push(this IDbQueue dbq,
               string queue, IAsyncEnumerable<byte[]> data, CancellationToken cancellationToken = default)
        {
            return Push(dbq, new[] { queue }, data, cancellationToken);
        }

        public static Task Push<T>(this IDbQueue dbq,
               string queue, T data, CancellationToken cancellationToken = default)
        {
            return Push(dbq, new[] { queue }, data, cancellationToken);
        }

        public static async Task<T?> Pop<T>(this IDbQueue dbq,
               string queue, CancellationToken cancellationToken = default)
        {
            return await Convert<T>(await dbq.Pop(queue, cancellationToken));
        }

        public static async Task<T?> Peek<T>(this IDbQueue dbq,
               string queue, long index = 0, CancellationToken cancellationToken = default)
        {
            return await Convert<T>(await dbq.Peek(queue, index, cancellationToken));
        }

        public static async IAsyncEnumerable<IAsyncEnumerator<byte[]>> PopMany(this IDbQueue dbq,
                   string queue, long? count = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            for (var i = 0L; count == null || i < count; i++)
            {
                var enumerator = await dbq.Pop(queue, cancellationToken);

                if (enumerator == null)
                    break;

                yield return enumerator;
            }
        }

        public static async IAsyncEnumerable<IAsyncEnumerator<byte[]>> PeekMany(this IDbQueue dbq,
                string queue, long? count = null, long skip = 0, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            count = skip + count;
            for (var i = skip; count == null || i < count; i++)
            {
                var enumerator = await dbq.Peek(queue, i, cancellationToken);

                if (enumerator == null)
                    break;

                yield return enumerator;
            }
        }

        public static async IAsyncEnumerable<T?> PopMany<T>(this IDbQueue dbq,
                string queue, long? count = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await foreach (var item in PopMany(dbq, queue, count, cancellationToken))
                yield return await Convert<T>(item);
        }

        public static async IAsyncEnumerable<T?> PeekMany<T>(this IDbQueue dbq,
               string queue, long? count = null, long skip = 0, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await foreach (var item in PeekMany(dbq, queue, count, skip, cancellationToken))
                yield return await Convert<T>(item);
        }



        private static async IAsyncEnumerator<byte[]> GetAsyncEnumerator(Stream stream, CancellationToken cancellationToken = default)
        {
            var buffer = new byte[4096];
            int count;

            while ((count = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
            {
                if (count == buffer.Length)
                {
                    yield return buffer;
                    continue;
                }

                var chunk = new byte[count];
                Array.Copy(buffer, 0, chunk, 0, count);
                yield return chunk;
            }
        }

        private static async IAsyncEnumerator<byte[]> GetChunkEnumerator(byte[] data)
        {
            var bufferSize = 4096;

            if (data.Length <= bufferSize)
            {
                yield return await Task.FromResult(data);
            }
            else
            {
                var buffer = new byte[bufferSize];
                for (var i = 0; i < data.Length; i += buffer.Length)
                {
                    var count = Math.Min(buffer.Length, data.Length - i);
                    var chunk = count == buffer.Length ? buffer : new byte[count];
                    Array.Copy(data, i, chunk, 0, chunk.Length);
                    yield return chunk;
                }
            }
        }

        private static async Task<T?> Convert<T>(IAsyncEnumerator<byte[]>? data)
        {
            var result = default(T?);

            if (data == null)
                return result;

            using var ms = new MemoryStream();

            while (await data.MoveNextAsync())
            {
                ms.Write(data.Current);

                if (data.Current.Length == 0)
                    result = Deserialize<T>(ms.ToArray());
            }

            return result;
        }

        private static byte[] Serialize<T>(T? data)
        {
            if (data == null || typeof(T) == typeof(byte[]))
                return data as byte[] ?? BytesEmpty;

            var str = typeof(T) == typeof(string) ? data as string
                : JsonConvert.SerializeObject(data, JsonSerializerSettings);

            return TextEncoding.GetBytes(str ?? string.Empty);
        }

        private static T? Deserialize<T>(string? data)
        {
            return data == null ? default(T?)
                : typeof(T) == typeof(string) ? (T?)(data as object)
                : JsonConvert.DeserializeObject<T>(data, JsonSerializerSettings);
        }

        private static T? Deserialize<T>(byte[] data)
        {
            return typeof(T) == typeof(byte[]) ? (T?)(data as object)
                : Deserialize<T>(TextEncoding.GetString(data));
        }

        private static readonly byte[] BytesEmpty = new byte[0];

        public static Encoding TextEncoding = Encoding.UTF8;

        public static JsonSerializerSettings JsonSerializerSettings = new()
        {
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            TypeNameHandling = TypeNameHandling.Auto,
        };
    }
}
