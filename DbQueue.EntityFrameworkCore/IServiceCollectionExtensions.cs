using DbQueue;
using DbQueue.EntityFrameworkCore;
using System;

namespace Microsoft.Extensions.DependencyInjection;

public static class DbqEfcExtensions
{
    public static IServiceCollection AddDbqEfc(this IServiceCollection services,
        DbqEfcOptions options,
        ServiceLifetime lifetime = ServiceLifetime.Transient)
    {
        services.Add(new ServiceDescriptor(typeof(IDbQueue), x => CreateDbq(options, false), lifetime));
        services.Add(new ServiceDescriptor(typeof(IDbStack), x => CreateDbq(options, true), lifetime));
        return services;
    }

    public static IServiceCollection AddDbqEfc(this IServiceCollection services,
        Action<DbqEfcOptions> optionsBuilder,
        ServiceLifetime lifetime = ServiceLifetime.Transient)
    {
        var options = new DbqEfcOptions();
        optionsBuilder?.Invoke(options);
        return AddDbqEfc(services, options, lifetime);
    }

    public static IServiceCollection AddDbqEfc(this IServiceCollection services,
        Action<IServiceProvider, DbqEfcOptions> optionsBuilder,
        ServiceLifetime lifetime = ServiceLifetime.Transient)
    {
        services.Add(new ServiceDescriptor(typeof(IDbQueue), x => CreateDbq(x, optionsBuilder, false), lifetime));
        services.Add(new ServiceDescriptor(typeof(IDbStack), x => CreateDbq(x, optionsBuilder, true), lifetime));
        return services;
    }

    public static IServiceCollection AddKeyedDbqEfc(this IServiceCollection services,
        object? serviceKey,
        DbqEfcOptions options,
        ServiceLifetime lifetime = ServiceLifetime.Transient)
    {
        services.Add(new ServiceDescriptor(typeof(IDbQueue), serviceKey, (x, k) => CreateDbq(options, false), lifetime));
        services.Add(new ServiceDescriptor(typeof(IDbStack), serviceKey, (x, k) => CreateDbq(options, true), lifetime));
        return services;
    }

    public static IServiceCollection AddKeyedDbqEfc(this IServiceCollection services,
        object? serviceKey,
        Action<DbqEfcOptions> optionsBuilder,
        ServiceLifetime lifetime = ServiceLifetime.Transient)
    {
        var options = new DbqEfcOptions();
        optionsBuilder?.Invoke(options);
        return AddKeyedDbqEfc(services, serviceKey, options, lifetime);
    }

    public static IServiceCollection AddKeyedDbqEfc(this IServiceCollection services,
        object? serviceKey,
        Action<IServiceProvider, DbqEfcOptions> optionsBuilder,
        ServiceLifetime lifetime = ServiceLifetime.Transient)
    {
        services.Add(new ServiceDescriptor(typeof(IDbQueue), serviceKey, (x, k) => CreateDbq(x, optionsBuilder, false), lifetime));
        services.Add(new ServiceDescriptor(typeof(IDbStack), serviceKey, (x, k) => CreateDbq(x, optionsBuilder, true), lifetime));
        return services;
    }

    static DbqEfc CreateDbq(IServiceProvider x, Action<IServiceProvider, DbqEfcOptions> optionsBuilder, bool stackMode)
    {
        var options = new DbqEfcOptions();
        optionsBuilder?.Invoke(x, options);
        return CreateDbq(options, stackMode);
    }

    static DbqEfc CreateDbq(DbqEfcOptions options, bool stackMode)
    {
        return new(
            new DbqDatabase(options.Database),
            new DbqBlobStorage(options.BlobStorage),
            options.Queue)
        {
            StackMode = stackMode
        };
    }
}
