using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace DbQueue
{
    public class DbqBlobStorage : IDbqBlobStorage
    {
        public DbqBlobStorage(DbqBlobStorageSettings? settings)
        {
            _settings = settings ?? new();
        }

        readonly DbqBlobStorageSettings _settings;

        public async Task<string> Add(IAsyncEnumerator<byte[]> data, CancellationToken cancellationToken = default)
        {
            var path = _settings.PathBuilder(Guid.NewGuid().ToString("n"));

            (new FileInfo(path)).Directory.Create();

            using var file = File.Create(path);

            while (await data.MoveNextAsync())
                await file.WriteAsync(data.Current, 0, data.Current.Length, cancellationToken);

            return path;
        }

        public async IAsyncEnumerable<byte[]> Get(string path, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var buffer = new byte[4096];

            using var file = new FileStream(path,
                FileMode.Open, FileAccess.Read, FileShare.Read,
                bufferSize: buffer.Length, useAsync: true);

            var count = 0;
            while ((count = await file.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
            {
                var chunk = new byte[count];
                Array.Copy(buffer, 0, chunk, 0, count);
                yield return chunk;
            }
        }


        public Task Delete(string path, CancellationToken cancellationToken = default)
        {
            if (File.Exists(path))
                File.Delete(path);

            return Task.CompletedTask;
        }
    }
}
