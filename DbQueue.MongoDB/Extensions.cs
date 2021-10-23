using DbQueue;
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

            services.AddTransient(x => x.GetRequiredService<DbqMongoOptions>().Database);
            services.AddTransient(x => x.GetRequiredService<DbqMongoOptions>().BlobStorage);
            services.AddTransient(x => x.GetRequiredService<DbqMongoOptions>().Queue);
            services.AddTransient<Dbq>();

            services.TryAdd(new ServiceDescriptor(typeof(IDbqDatabase), typeof(DbqDatabase), lifetime));
            services.TryAdd(new ServiceDescriptor(typeof(IDbqBlobStorage), typeof(DbqBlobStorage), lifetime));
            services.Add(new ServiceDescriptor(typeof(IDbQueue), typeof(Dbq), lifetime));

            services.Add(new ServiceDescriptor(typeof(IDbQueue), x =>
            {
                var dbq = x.GetRequiredService<Dbq>();
                dbq.StackMode = false;
                return dbq;
            }, lifetime));

            services.Add(new ServiceDescriptor(typeof(IDbStack), x =>
            {
                var dbq = x.GetRequiredService<Dbq>();
                dbq.StackMode = true;
                return dbq;
            }, lifetime));

            return services;
        }
    }

}
