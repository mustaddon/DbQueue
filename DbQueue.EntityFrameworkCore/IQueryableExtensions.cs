using DbQueue.EntityFrameworkCore;
using System;
using System.Linq;
using System.Linq.Expressions;

namespace DbQueue.EntityFrameworkCore
{
    internal static class IQueryableExtensions
    {
        public static IOrderedQueryable<TSource> OrderBy<TSource, TKey>(this IQueryable<TSource> source, Expression<Func<TSource, TKey>> keySelector, bool desc)
        {
            return desc ? source.OrderByDescending(keySelector) : source.OrderBy(keySelector);
        }
    }
}
