using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace DbQueue
{
    public static partial class DbqExtensions
    {
        public static Encoding TextEncoding = Encoding.UTF8;

        public static JsonSerializerSettings JsonSerializerSettings = new()
        {
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            TypeNameHandling = TypeNameHandling.Auto,
        };

        public static Stream ToStream(this IAsyncEnumerator<byte[]> enumerator)
        {
            return new DbqStream(enumerator);
        }

        public static async Task<Stream?> ToStream(this Task<IAsyncEnumerator<byte[]>?> task)
        {
            var enumerator = await task;
            return enumerator == null ? null : new DbqStream(enumerator);
        }

        public static async IAsyncEnumerable<Stream> ToStream(this IAsyncEnumerable<IAsyncEnumerator<byte[]>> enumerators)
        {
            await foreach (var enumerator in enumerators)
                yield return enumerator.ToStream();
        }

        public static async Task<byte[]> ToArray(this IAsyncEnumerator<byte[]> enumerator)
        {
            var ms = new MemoryStream();

            while (await enumerator.MoveNextAsync())
                ms.Write(enumerator.Current);

            return ms.ToArray();
        }

        public static async Task<byte[]?> ToArray(this Task<IAsyncEnumerator<byte[]>?> task)
        {
            var enumerator = await task;
            return enumerator == null ? null : await enumerator.ToArray();
        }

        public static async IAsyncEnumerable<byte[]> ToArray(this IAsyncEnumerable<IAsyncEnumerator<byte[]>> enumerators)
        {
            await foreach (var enumerator in enumerators)
                yield return await enumerator.ToArray();
        }

        public static async Task<T?> ConvertTo<T>(this IAsyncEnumerator<byte[]> enumerator)
        {
            var type = typeof(T);

            if (type == typeof(Stream))
                return (T)(enumerator.ToStream() as object);

            if (type == typeof(byte[]))
                return (T)(await enumerator.ToArray() as object);

            using var stream = enumerator.ToStream();
            using var reader = new StreamReader(stream, TextEncoding, true);

            if (type == typeof(string))
                return (T)(await reader.ReadToEndAsync() as object);

            using var jsonTextReader = new JsonTextReader(reader);

            return JsonSerializer.Create(JsonSerializerSettings)
                .Deserialize<T>(jsonTextReader);
        }

        public static async Task<T?> ConvertTo<T>(this Task<IAsyncEnumerator<byte[]>?> task)
        {
            var enumerator = await task;
            return enumerator == null ? default : await enumerator.ConvertTo<T>();
        }

        public static async IAsyncEnumerable<T?> ConvertTo<T>(this IAsyncEnumerable<IAsyncEnumerator<byte[]>> enumerators)
        {
            await foreach (var enumerator in enumerators)
                yield return await enumerator.ConvertTo<T>();
        }

        public static async Task<IDbqAcknowledgement<T?>> ConvertTo<T>(this IDbqAcknowledgement<IAsyncEnumerator<byte[]>> ack)
        {
            try
            {
                return new DbqAck<T?>(await ack.Data.ConvertTo<T>(), ack.Commit, ack.DisposeAsync);
            }
            catch
            {
                await ack.DisposeAsync();
                throw;
            }
        }
    }
}
