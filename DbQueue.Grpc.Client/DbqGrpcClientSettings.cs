using Grpc.Core;
using Grpc.Net.Client;

namespace DbQueue
{
    public class DbqGrpcClientSettings
    {
        public GrpcChannelOptions GrpcChannel { get; set; } = new();

        public Metadata DefaultRequestHeaders { get; set; } = new();

        public bool StackMode { get; set; }

    }
}
