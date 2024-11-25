﻿using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;

namespace Test.EFCore
{
    internal class App
    {
        public static Lazy<IHost> Instance = new(static () =>
        {
            var builder = Host.CreateDefaultBuilder()
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddDbqEfc(options =>
                    {
                        options.Database.ContextConfigurator = (db) => db.UseNpgsql(hostContext.Configuration.GetConnectionString("dbq"));
                    });
                });

            return builder.Build();
        });

    }
}
