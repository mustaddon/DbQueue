using Microsoft.AspNetCore.Routing;

namespace Microsoft.AspNetCore.Builder
{
    public static class DbqGrpcExtensions
    {
        public static GrpcServiceEndpointConventionBuilder MapDbqGrpc(this IEndpointRouteBuilder builder)
        {
            return builder.MapGrpcService<DbQueue.Grpc.DbqGrpcService>();
        }
    }
}
