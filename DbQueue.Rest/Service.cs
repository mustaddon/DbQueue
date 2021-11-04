using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;

namespace DbQueue.Rest
{
    static class Service
    {
        private static readonly ConcurrentDictionary<string, Func<bool, Task>> _acks = new();

        public static Task Push(HttpContext context, string queue, string? type, DateTime? availableAfter, DateTime? removeAfter, string? separator)
        {
            context.Response.Headers.AddNoCache();

            return context.RequestServices.GetRequiredService<IDbQueue>().Push(
                queues: queue.Split(separator ?? ",", StringSplitOptions.RemoveEmptyEntries),
                data: context.Request.Body,
                type: string.IsNullOrEmpty(type) ? null : type,
                availableAfter: availableAfter,
                removeAfter: removeAfter,
                cancellationToken: context.RequestAborted);
        }

        public static async Task Peek(HttpContext context, bool stackMode, string queue, long? index)
        {
            context.Response.Headers.AddNoCache();

            var dbq = stackMode ? context.RequestServices.GetRequiredService<IDbStack>()
                : context.RequestServices.GetRequiredService<IDbQueue>();

            var data = await dbq.Peek(
                queue: queue,
                index: Math.Max(0, index ?? 0),
                cancellationToken: context.RequestAborted);

            if (data == null)
            {
                context.Response.StatusCode = 204;
                return;
            }

            context.Response.ContentType = context.Request.ContentType ?? _defaultContentType;

            while (await data.MoveNextAsync())
                await context.Response.Body.WriteAsync(data.Current, 0, data.Current.Length, context.RequestAborted);
        }

        public static async Task Pop(HttpContext context, bool stackMode, string queue, bool? useAck, int? lockTimeout)
        {
            context.Response.Headers.AddNoCache();

            var extraScope = context.RequestServices.CreateScope();

            var dbq = stackMode ? extraScope.ServiceProvider.GetRequiredService<IDbStack>()
                : extraScope.ServiceProvider.GetRequiredService<IDbQueue>();

            var ack = await dbq.Pop(
                queue: queue,
                cancellationToken: context.RequestAborted);

            if (ack == null)
            {
                context.Response.StatusCode = 204;
                extraScope.Dispose();
                return;
            }

            try
            {
                var key = Guid.NewGuid().ToString("n");

                if (useAck == true)
                    context.Response.Headers.Add("ack-key", key);

                context.Response.ContentType = context.Request.ContentType ?? _defaultContentType;

                while (await ack.Data.MoveNextAsync())
                    await context.Response.Body.WriteAsync(ack.Data.Current, 0, ack.Data.Current.Length, context.RequestAborted);

                if (useAck != true)
                {
                    await ack.Commit();
                    extraScope.Dispose();
                    return;
                }

                var autoUnlockCts = new CancellationTokenSource();

                if (!_acks.TryAdd(key, async commit =>
                {
                    using (extraScope)
                    using (ack)
                    {
                        autoUnlockCts.Cancel();
                        if (commit) await ack.Commit();
                    }
                })) throw new Exception("failed to add acknowledgement");

                _ = Task.Run(async () =>
                {
                    await Task.Delay(NormTimeout(lockTimeout));
                    if (!autoUnlockCts.Token.IsCancellationRequested && _acks.Remove(key, out var commit))
                        await commit(false);
                }, autoUnlockCts.Token);
            }
            catch
            {
                await ack.DisposeAsync();
                extraScope.Dispose();
                throw;
            }
        }

        public static async Task Ack(HttpContext context, string key, bool value)
        {
            context.Response.Headers.AddNoCache();

            if (_acks.Remove(key, out var commit))
                await commit(value);
            else
                throw new KeyNotFoundException();
        }

        public static Task<long> Count(HttpContext context, string queue)
        {
            context.Response.Headers.AddNoCache();

            return context.RequestServices.GetRequiredService<IDbQueue>().Count(
                queue: queue,
                cancellationToken: context.RequestAborted);
        }

        public static Task Clear(HttpContext context, string queue, string? type, string? separator)
        {
            context.Response.Headers.AddNoCache();

            return context.RequestServices.GetRequiredService<IDbQueue>().Clear(
                queue: queue,
                types: string.IsNullOrEmpty(type) ? null
                    : type.Split(separator ?? ",", StringSplitOptions.RemoveEmptyEntries),
                cancellationToken: context.RequestAborted);
        }


        private static void AddNoCache(this IHeaderDictionary headers)
        {
            headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
            headers["Pragma"] = "no-cache";
            headers["Expires"] = "0";
        }

        private static int NormTimeout(int? lockTimeout)
        {
            return Math.Max(5000, Math.Min(86400000, lockTimeout ?? 300000));
        }

        private static readonly string _defaultContentType = "text/plain;charset=utf-8";
    }
}
