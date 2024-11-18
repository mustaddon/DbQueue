using DbQueue;
using DbQueue.MongoDB;
using System;

namespace Microsoft.Extensions.DependencyInjection;

public static class DbqMongoExtensions
{
    public static IServiceCollection AddDbqMongo(this IServiceCollection services,
        Action<IServiceProvider, DbqMongoOptions>? optionsBuilder = null,
        ServiceLifetime lifetime = ServiceLifetime.Transient)
    {
        services.Add(new ServiceDescriptor(typeof(IDbQueue), x => CreateDbq(x, optionsBuilder, false), lifetime));
        services.Add(new ServiceDescriptor(typeof(IDbStack), x => CreateDbq(x, optionsBuilder, true), lifetime));
        return services;
    }

    public static IServiceCollection AddKeyedDbqMongo(this IServiceCollection services,
        object? serviceKey,
        Action<IServiceProvider, DbqMongoOptions>? optionsBuilder = null,
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

        return new(
            new DbqDatabase(options.Database),
            new DbqBlobStorage(options.BlobStorage),
            options.Queue)
        {
            StackMode = stackMode
        };
    }
}
