using DbQueue;
using DbQueue.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;
using System.Linq;
using System.Linq.Expressions;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class DbqEfcExtensions
    {
        public static IServiceCollection AddDbqEfc(this IServiceCollection services,
            Action<IServiceProvider, DbqEfcOptions> optionsBuilder,
            ServiceLifetime lifetime = ServiceLifetime.Scoped)
        {
            services.Add(new ServiceDescriptor(typeof(DbqEfcOptions), x =>
            {
                var options = new DbqEfcOptions();
                optionsBuilder?.Invoke(x, options);
                return options;
            }, lifetime));

            services.AddTransient(x => x.GetRequiredService<DbqEfcOptions>().Database);
            services.AddTransient(x => x.GetRequiredService<DbqEfcOptions>().BlobStorage);
            services.AddTransient(x => x.GetRequiredService<DbqEfcOptions>().Queue);
            services.AddTransient<Dbq>();

            services.TryAdd(new ServiceDescriptor(typeof(IDbqDatabase), typeof(DbqDatabase), lifetime));
            services.TryAdd(new ServiceDescriptor(typeof(IDbqBlobStorage), typeof(DbqBlobStorage), lifetime));

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


namespace DbQueue.EntityFrameworkCore
{
    internal static class LinqExtensions
    {
        public static IOrderedQueryable<TSource> OrderBy<TSource, TKey>(this IQueryable<TSource> source, Expression<Func<TSource, TKey>> keySelector, bool desc)
        {
            return desc ? source.OrderByDescending(keySelector) : source.OrderBy(keySelector);
        }
    }
}
