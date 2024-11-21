using DbQueue;
using DbQueue.MongoDB;
using System;

namespace Microsoft.Extensions.DependencyInjection;

public static class DbqMongoExtensions
{
    public static IServiceCollection AddDbqMongo(this IServiceCollection services,
        DbqMongoOptions options,
        ServiceLifetime lifetime = ServiceLifetime.Transient)
    {
        if (options == null) 
            throw new ArgumentNullException(nameof(options));

        services.Add(new ServiceDescriptor(typeof(IDbQueue), x => CreateDbq(options, false), lifetime));
        services.Add(new ServiceDescriptor(typeof(IDbStack), x => CreateDbq(options, true), lifetime));
        return services;
    }

    public static IServiceCollection AddDbqMongo(this IServiceCollection services,
        Action<DbqMongoOptions>? optionsBuilder = null,
        ServiceLifetime lifetime = ServiceLifetime.Transient)
    {
        var options = new DbqMongoOptions();
        optionsBuilder?.Invoke(options);
        return AddDbqMongo(services, options, lifetime);
    }

    public static IServiceCollection AddDbqMongo(this IServiceCollection services,
        Action<IServiceProvider, DbqMongoOptions>? optionsBuilder,
        ServiceLifetime lifetime = ServiceLifetime.Transient)
    {
        services.Add(new ServiceDescriptor(typeof(IDbQueue), x => CreateDbq(x, optionsBuilder, false), lifetime));
        services.Add(new ServiceDescriptor(typeof(IDbStack), x => CreateDbq(x, optionsBuilder, true), lifetime));
        return services;
    }


    public static IServiceCollection AddKeyedDbqMongo(this IServiceCollection services,
        object? serviceKey,
        DbqMongoOptions options,
        ServiceLifetime lifetime = ServiceLifetime.Transient)
    {
        if (options == null) 
            throw new ArgumentNullException(nameof(options));

        services.Add(new ServiceDescriptor(typeof(IDbQueue), serviceKey, (x, k) => CreateDbq(options, false), lifetime));
        services.Add(new ServiceDescriptor(typeof(IDbStack), serviceKey, (x, k) => CreateDbq(options, true), lifetime));
        return services;
    }

    public static IServiceCollection AddKeyedDbqMongo(this IServiceCollection services,
        object? serviceKey,
        Action<DbqMongoOptions>? optionsBuilder = null,
        ServiceLifetime lifetime = ServiceLifetime.Transient)
    {
        var options = new DbqMongoOptions();
        optionsBuilder?.Invoke(options);
        return AddKeyedDbqMongo(services, serviceKey, options, lifetime);
    }

    public static IServiceCollection AddKeyedDbqMongo(this IServiceCollection services,
        object? serviceKey,
        Action<IServiceProvider, DbqMongoOptions>? optionsBuilder,
        ServiceLifetime lifetime = ServiceLifetime.Transient)
    {
        services.Add(new ServiceDescriptor(typeof(IDbQueue), serviceKey, (x, k) => CreateDbq(x, optionsBuilder, false), lifetime));
        services.Add(new ServiceDescriptor(typeof(IDbStack), serviceKey, (x, k) => CreateDbq(x, optionsBuilder, true), lifetime));
        return services;
    }



    static DbqMongo CreateDbq(IServiceProvider x, Action<IServiceProvider, DbqMongoOptions>? optionsBuilder, bool stackMode)
    {
        var options = new DbqMongoOptions();
        optionsBuilder?.Invoke(x, options);
        return CreateDbq(options, stackMode);
    }

    static DbqMongo CreateDbq(DbqMongoOptions options, bool stackMode)
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
