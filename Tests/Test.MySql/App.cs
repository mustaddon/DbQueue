using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;

namespace Test.EFCore
{
    internal class App
    {
        public static Lazy<IHost> Official = new(static () =>
        {
            var builder = Host.CreateDefaultBuilder()
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddDbqEfc((sp, options) =>
                    {
                        options.Database.ContextConfigurator = (db) => db.UseMySQL(hostContext.Configuration.GetConnectionString("dbq"));
                    }, ServiceLifetime.Transient);
                });

            return builder.Build();
        });

        public static Lazy<IHost> Pomelo = new(static () =>
        {
            var builder = Host.CreateDefaultBuilder()
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddDbqEfc((sp, options) =>
                    {
                        var connectionString = hostContext.Configuration.GetConnectionString("dbq");
                        options.Database.ContextConfigurator = (db) => db.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
                    }, ServiceLifetime.Transient);
                });

            return builder.Build();
        });

    }
}
