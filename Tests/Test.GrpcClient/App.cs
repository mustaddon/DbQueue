using DbQueue;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;

namespace Test.GrpcClient
{
    internal class App
    {
        public static Lazy<IHost> Instance = new(static () =>
        {
            var builder = Host.CreateDefaultBuilder()
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddTransient(x => new DbqGrpcClient("https://localhost:7271"));

                    services.AddTransient<IDbQueue>(x=> {
                        var client = x.GetRequiredService<DbqGrpcClient>();
                        client.StackMode = false;
                        return client;
                    });

                    services.AddTransient<IDbStack>(x => {
                        var client = x.GetRequiredService<DbqGrpcClient>();
                        client.StackMode = true;
                        return client;
                    });
                });

            return builder.Build();
        });

    }
}
