using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace DbQueue
{
    public static partial class DbqExtensions
    {
        public static async Task Push(this IDbQueue dbq,
               IEnumerable<string> queues, Stream data, CancellationToken cancellationToken = default)
        {
            await using var enumerator = data.GetAsyncEnumerator(cancellationToken);
            await dbq.Push(queues, enumerator, cancellationToken);
        }

        public static async Task Push(this IDbQueue dbq,
               IEnumerable<string> queues, byte[] data, CancellationToken cancellationToken = default)
        {
            await using var enumerator = data.GetChunkEnumerator();
            await dbq.Push(queues, enumerator, cancellationToken);
        }

        public static async Task Push(this IDbQueue dbq,
               IEnumerable<string> queues, IAsyncEnumerable<byte[]> data, CancellationToken cancellationToken = default)
        {
            await using var enumerator = data.GetAsyncEnumerator(cancellationToken);
            await dbq.Push(queues, enumerator, cancellationToken);
        }

        public static async Task Push(this IDbQueue dbq,
               IEnumerable<string> queues, object? data, CancellationToken cancellationToken = default)
        {
            if (data == null || data is byte[])
            {
                await Push(dbq, queues, (data as byte[]) ?? BytesEmpty, cancellationToken);
                return;
            }

            if (data is Stream)
            {
                await Push(dbq, queues, data as Stream ?? new MemoryStream(), cancellationToken);
                return;
            }

            using var ms = new MemoryStream();

            using (var writer = new StreamWriter(ms, TextEncoding, 4096, true))
            {
                if(data is string)
                    await writer.WriteAsync(data as string);
                else
                    JsonSerializer.Create(JsonSerializerSettings).Serialize(writer, data);
            }

            ms.Position = 0;

            await Push(dbq, queues, ms as Stream, cancellationToken);
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

        public static Task Push(this IDbQueue dbq,
               string queue, object? data, CancellationToken cancellationToken = default)
        {
            return Push(dbq, new[] { queue }, data, cancellationToken);
        }

        private static async IAsyncEnumerator<byte[]> GetAsyncEnumerator(this Stream stream, CancellationToken cancellationToken = default)
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

        private static async IAsyncEnumerator<byte[]> GetChunkEnumerator(this byte[] data)
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
    }
}
