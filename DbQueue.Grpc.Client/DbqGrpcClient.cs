using DbQueue.Grpc;
using Google.Protobuf;
using Grpc.Core;
using Grpc.Net.Client;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DbQueue
{
    public class DbqGrpcClient : IDbqBoth, IDisposable
    {
        public DbqGrpcClient(string address, DbqGrpcClientSettings? settings = null)
        {
            _settings = settings ?? new();
            _channel = new Lazy<GrpcChannel>(() => GrpcChannel.ForAddress(address, _settings.GrpcChannel));
            StackMode = _settings.StackMode;
        }

        private readonly DbqGrpcClientSettings _settings;
        private readonly Lazy<GrpcChannel> _channel;

        public Metadata DefaultRequestHeaders => _settings.DefaultRequestHeaders;

        public bool StackMode { get; set; }

        public void Dispose()
        {
            if (_channel.IsValueCreated) 
                _channel.Value.Dispose();

            GC.SuppressFinalize(this);
        }

        public async Task Push(IEnumerable<string> queues, IAsyncEnumerator<byte[]> data, string? type = null, DateTime? availableAfter = null, DateTime? removeAfter = null, CancellationToken cancellationToken = default)
        {
            using var push = CreateClient().Push(
                   headers: DefaultRequestHeaders,
                   cancellationToken: cancellationToken);

            var args = new PushChunk
            {
                Type = type ?? string.Empty,
                AvailableAfter = availableAfter?.ToUniversalTime().Ticks ?? 0,
                RemoveAfter = removeAfter?.ToUniversalTime().Ticks ?? 0,
            };

            foreach (var queue in queues)
                args.Queue.Add(queue);

            // send args
            await push.RequestStream.WriteAsync(args);

            // send data
            while (await data.MoveNextAsync())
                await push.RequestStream.WriteAsync(new()
                {
                    Data = ByteString.CopyFrom(data.Current)
                });

            await push.RequestStream.CompleteAsync();
            await push.ResponseAsync;
        }

        public async Task<IAsyncEnumerator<byte[]>?> Peek(string queue, long index = 0, CancellationToken cancellationToken = default)
        {
            var peek = CreateClient().Peek(
                request: new() { 
                    StackMode = StackMode, 
                    Queue = queue, 
                    Index = index 
                },
                headers: DefaultRequestHeaders,
                cancellationToken: cancellationToken);

            var enumerator = ToEnumerator(peek.ResponseStream, x => peek.Dispose(), cancellationToken);

            if (!await enumerator.MoveNextAsync())
                return null;

            return enumerator;
        }

        public async Task<IDbqAcknowledgement<IAsyncEnumerator<byte[]>>?> Pop(string queue, CancellationToken cancellationToken = default)
        {
            var pop = CreateClient().Pop(
                headers: DefaultRequestHeaders,
                cancellationToken: cancellationToken);
            
            // send args
            await pop.RequestStream.WriteAsync(new()
            {
                StackMode = StackMode,
                Queue = queue,
            });

            var allDataReaded = false;
            var data = ToEnumerator(pop.ResponseStream, x => allDataReaded = x, cancellationToken);

            if (!await data.MoveNextAsync())
                return null;

            Task? committing = null;
            var commit = new Func<bool, Task>(async (x) =>
            {
                await pop.RequestStream.WriteAsync(new() { Commit = x });

                if (!allDataReaded)
                    while (await pop.ResponseStream.MoveNext(cancellationToken)
                        && !pop.ResponseStream.Current.Data.IsEmpty) { }

                if (!await pop.ResponseStream.MoveNext(cancellationToken) || !pop.ResponseStream.Current.Data.IsEmpty)
                    throw new Exception($"Failed to {(x ? "commit" : "unlock")}");

                await pop.RequestStream.CompleteAsync();
                await data.DisposeAsync();
            });

            return new DbqAck<IAsyncEnumerator<byte[]>>(data,
                commit: async () => await (committing = commit(true)),
                dispose: async () =>
                {
                    await (committing ?? commit(false));
                    pop.Dispose();
                });
        }

        public async Task<long> Count(string queue, CancellationToken cancellationToken = default)
        {
            var responce = await CreateClient().CountAsync(
                   request: new() { Queue = queue },
                   headers: DefaultRequestHeaders,
                   cancellationToken: cancellationToken);

            return responce.Count;
        }

        public async Task Clear(string queue, IEnumerable<string>? types = null, CancellationToken cancellationToken = default)
        {
            var request = new ClearRequest { Queue = queue };

            if (types != null)
                foreach (var type in types)
                    request.Type.Add(type);

            var responce = await CreateClient().ClearAsync(
                request: request,
                headers: DefaultRequestHeaders,
                cancellationToken: cancellationToken);
        }


        private Endpoint.EndpointClient CreateClient()
        {
            return new Endpoint.EndpointClient(_channel.Value);
        }

        private static async IAsyncEnumerator<byte[]> ToEnumerator(IAsyncStreamReader<DataChunk> data, Action<bool>? onFinal, CancellationToken cancellationToken = default)
        {
            var completed = false;

            if (await data.MoveNext(cancellationToken))
                try
                {
                    yield return BytesEmpty;
                    yield return data.Current.Data.ToByteArray();

                    while (await data.MoveNext(cancellationToken) && !data.Current.Data.IsEmpty)
                        yield return data.Current.Data.ToByteArray();

                    completed = true;
                }
                finally
                {
                    onFinal?.Invoke(completed);
                }
        }

        private static readonly byte[] BytesEmpty = new byte[0];
    }
}
