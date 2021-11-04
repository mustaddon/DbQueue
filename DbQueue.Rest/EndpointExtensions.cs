#if NET6_0_OR_GREATER
using DbQueue.Rest;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Microsoft.AspNetCore.Builder
{
    public static class DbqRestExtensions
    {
        public static IEndpointConventionBuilder MapDbqRest(this IEndpointRouteBuilder builder)
        {
            return new EndpointConventionBuilder(new List<IEndpointConventionBuilder>()
            {
                // push
                builder.MapPost("dbq/queue/{name:required}", (HttpContext context, string name, string? type, DateTime? availableAfter, DateTime? removeAfter, string? separator)
                    => Service.Push(context, name, type, availableAfter, removeAfter, separator)),
                builder.MapPost("dbq/stack/{name:required}", (HttpContext context, string name, string? type, DateTime? availableAfter, DateTime? removeAfter, string? separator)
                    => Service.Push(context, name, type, availableAfter, removeAfter, separator)),
                
                // pop
                builder.MapGet("dbq/queue/{name:required}", (HttpContext context, string name, bool? useAck, int? lockTimeout)
                    => Service.Pop(context, false, name, useAck, lockTimeout)),
                builder.MapGet("dbq/stack/{name:required}", (HttpContext context, string name, bool? useAck, int? lockTimeout)
                    => Service.Pop(context, true, name, useAck, lockTimeout)),

                // peek
                builder.MapGet("dbq/queue/{name:required}/peek", (HttpContext context, string name, int? index)
                    => Service.Peek(context, false, name, index)),
                builder.MapGet("dbq/stack/{name:required}/peek", (HttpContext context, string name, int? index)
                    => Service.Peek(context, true, name, index)),

                // count
                builder.MapGet("dbq/queue/{name:required}/count", (HttpContext context, string name)
                    => Service.Count(context, name)),
                builder.MapGet("dbq/stack/{name:required}/count", (HttpContext context, string name)
                    => Service.Count(context, name)),

                // clear
                builder.MapDelete("dbq/queue/{name:required}", (HttpContext context, string name, string? type, string? separator)
                    => Service.Clear(context, name, type, separator)),
                builder.MapDelete("dbq/stack/{name:required}", (HttpContext context, string name, string? type, string? separator)
                    => Service.Clear(context, name, type, separator)),

                // ack
                builder.MapPost("dbq/ack/{key:required}", (HttpContext context, string key)
                    => Service.Ack(context, key, true)),
                builder.MapDelete("dbq/ack/{key:required}", (HttpContext context, string key)
                    => Service.Ack(context, key, false)),
            });
        }
    }
}
#endif