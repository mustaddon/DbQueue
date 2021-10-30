using Google.Protobuf;
using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;

namespace DbQueue.Grpc
{
    public class DbqGrpcService : Endpoint.EndpointBase
    {
        public DbqGrpcService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        private readonly IServiceProvider _serviceProvider;

        public override async Task<Empty> Push(IAsyncStreamReader<PushChunk> requestStream, ServerCallContext context)
        {
            if (await requestStream.MoveNext(context.CancellationToken))
                await _serviceProvider.GetRequiredService<IDbQueue>().Push(
                    queues: requestStream.Current.Queue,
                    data: GetContent(requestStream, context.CancellationToken),
                    type: ToNullableString(requestStream.Current.Type),
                    availableAfter: ToDateTime(requestStream.Current.AvailableAfter),
                    removeAfter: ToDateTime(requestStream.Current.RemoveAfter),
                    cancellationToken: context.CancellationToken);

            return new();
        }

        public override async Task Peek(PeekRequest request, IServerStreamWriter<DataChunk> responseStream, ServerCallContext context)
        {
            var dbq = request.StackMode ? _serviceProvider.GetRequiredService<IDbStack>()
                : _serviceProvider.GetRequiredService<IDbQueue>();

            var data = await dbq.Peek(
                queue: request.Queue,
                index: request.Index,
                cancellationToken: context.CancellationToken);

            if (data == null)
                return;

            while (await data.MoveNextAsync())
                await responseStream.WriteAsync(new()
                {
                    Data = ByteString.CopyFrom(data.Current, 0, data.Current.Length),
                });
        }

        public override async Task Pop(IAsyncStreamReader<PopRequest> requestStream, IServerStreamWriter<DataChunk> responseStream, ServerCallContext context)
        {
            if (!await requestStream.MoveNext(context.CancellationToken))
                return;

            var dbq = requestStream.Current.StackMode ? _serviceProvider.GetRequiredService<IDbStack>()
                : _serviceProvider.GetRequiredService<IDbQueue>();

            await using var ack = await dbq.Pop(
                queue: requestStream.Current.Queue,
                cancellationToken: context.CancellationToken);

            if (ack == null)
                return;

            var commit = Task.Run(async () =>
            {
                if (!await requestStream.MoveNext(context.CancellationToken))
                    return;

                if (requestStream.Current.Commit)
                    await ack.Commit();
                else
                    await ack.DisposeAsync();
            }, context.CancellationToken);

            while (await ack.Data.MoveNextAsync() && !commit.IsCompleted)
                await responseStream.WriteAsync(new()
                {
                    Data = ByteString.CopyFrom(ack.Data.Current, 0, ack.Data.Current.Length),
                });

            await responseStream.WriteAsync(new() { Data = ByteString.Empty });
            await commit;
            await responseStream.WriteAsync(new() { Data = ByteString.Empty });
        }

        public override async Task<CountResponse> Count(CountRequest request, ServerCallContext context)
        {
            var count = await _serviceProvider.GetRequiredService<IDbQueue>().Count(
                queue: request.Queue,
                cancellationToken: context.CancellationToken);

            return new() { Count = count };
        }

        public override async Task<Empty> Clear(ClearRequest request, ServerCallContext context)
        {
            await _serviceProvider.GetRequiredService<IDbQueue>().Clear(
                queue: request.Queue,
                types: request.Type,
                cancellationToken: context.CancellationToken);

            return new();
        }




        private static string? ToNullableString(string str)
        {
            return string.IsNullOrEmpty(str) ? null : str;
        }

        private static DateTime? ToDateTime(long ticks)
        {
            return ticks > 0 ? new DateTime(ticks, DateTimeKind.Utc) : null;
        }

        private static async IAsyncEnumerator<byte[]> GetContent(IAsyncStreamReader<PushChunk> requestStream, CancellationToken cancellationToken)
        {
            do
            {
                if (requestStream.Current.Data.Length > 0)
                    yield return requestStream.Current.Data.ToByteArray();
            } while (await requestStream.MoveNext(cancellationToken));
        }

    }
}