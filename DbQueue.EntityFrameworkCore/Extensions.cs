using DbQueue;
using DbQueue.Abstractions;
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
                optionsBuilder(x, options);
                return options;
            }, lifetime));

            services.AddTransient(x => x.GetRequiredService<DbqEfcOptions>().Queue);
            services.AddTransient(x => x.GetRequiredService<DbqEfcOptions>().Database);
            services.AddTransient(x => x.GetRequiredService<DbqEfcOptions>().BlobStorage);

            services.TryAdd(new ServiceDescriptor(typeof(IDbqDatabase), typeof(DbqDatabase), lifetime));
            services.TryAdd(new ServiceDescriptor(typeof(IDbqBlobStorage), typeof(DbqBlobStorage), lifetime));
            services.Add(new ServiceDescriptor(typeof(IDbQueue), typeof(Dbq), lifetime));

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
