using DbQueue;
using DbQueue.Abstractions;
using DbQueue.MongoDB;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class DfsEfcExtensions
    {
        public static IServiceCollection AddDbqMongo(this IServiceCollection services,
            Action<IServiceProvider, DbqMongoOptions>? optionsBuilder = null,
            ServiceLifetime lifetime = ServiceLifetime.Scoped)
        {
            services.Add(new ServiceDescriptor(typeof(DbqMongoOptions), x =>
            {
                var options = new DbqMongoOptions();
                optionsBuilder?.Invoke(x, options);
                return options;
            }, lifetime));

            services.AddTransient(x => x.GetRequiredService<DbqMongoOptions>().Queue);
            services.AddTransient(x => x.GetRequiredService<DbqMongoOptions>().Database);
            services.AddTransient(x => x.GetRequiredService<DbqMongoOptions>().BlobStorage);

            services.TryAdd(new ServiceDescriptor(typeof(IDbqDatabase), typeof(DbqDatabase), lifetime));
            services.TryAdd(new ServiceDescriptor(typeof(IDbqBlobStorage), typeof(DbqBlobStorage), lifetime));
            services.Add(new ServiceDescriptor(typeof(IDbQueue), typeof(Dbq), lifetime));

            return services;
        }
    }

}
