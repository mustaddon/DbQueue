using DbQueue;
using DbQueue.EntityFrameworkCore;
using System;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class DbqEfcExtensions
    {
        public static IServiceCollection AddDbqEfc(this IServiceCollection services,
            Action<IServiceProvider, DbqEfcOptions> optionsBuilder,
            ServiceLifetime lifetime = ServiceLifetime.Scoped)
        {
            services.Add(new ServiceDescriptor(typeof(IDbQueue), x => CreateDbq(x, optionsBuilder, false), lifetime));
            services.Add(new ServiceDescriptor(typeof(IDbStack), x => CreateDbq(x, optionsBuilder, true), lifetime));
            return services;
        }

        public static IServiceCollection AddKeyedDbqEfc(this IServiceCollection services,
            object? serviceKey,
            Action<IServiceProvider, DbqEfcOptions> optionsBuilder,
            ServiceLifetime lifetime = ServiceLifetime.Scoped)
        {
            services.Add(new ServiceDescriptor(typeof(IDbQueue), serviceKey, (x, k) => CreateDbq(x, optionsBuilder, false), lifetime));
            services.Add(new ServiceDescriptor(typeof(IDbStack), serviceKey, (x, k) => CreateDbq(x, optionsBuilder, true), lifetime));
            return services;
        }


        static DbqEfc CreateDbq(IServiceProvider x, Action<IServiceProvider, DbqEfcOptions> optionsBuilder, bool stackMode)
        {
            var options = new DbqEfcOptions();
            optionsBuilder?.Invoke(x, options);

            return new(
                new DbqDatabase(options.Database),
                new DbqBlobStorage(options.BlobStorage),
                options.Queue)
            {
                StackMode = stackMode
            };
        }
    }

}
