using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;


namespace Microsoft.Extensions.DependencyInjection
{
    public static class GrpcFileStorageExtensions
    {
        public static GrpcServiceEndpointConventionBuilder MapGrpcDbQueue(this IEndpointRouteBuilder builder)
        {
            return builder.MapGrpcService<DbQueue.Grpc.DbqGrpcService>();
        }
    }
}
