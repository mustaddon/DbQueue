using DbQueue;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;

namespace Test.RestClient
{
    internal class App
    {
        public static Lazy<IHost> Instance = new(static () =>
        {
            var builder = Host.CreateDefaultBuilder()
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddTransient(x => new DbqRestClient("https://localhost:7273"));

                    services.AddTransient<IDbQueue>(x=> {
                        var client = x.GetRequiredService<DbqRestClient>();
                        client.StackMode = false;
                        return client;
                    });

                    services.AddTransient<IDbStack>(x => {
                        var client = x.GetRequiredService<DbqRestClient>();
                        client.StackMode = true;
                        return client;
                    });
                });

            return builder.Build();
        });

    }
}
