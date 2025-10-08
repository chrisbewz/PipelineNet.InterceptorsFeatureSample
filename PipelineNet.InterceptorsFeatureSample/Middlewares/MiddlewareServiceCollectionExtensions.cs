using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection;
using Ardalis.GuardClauses;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using PipelineNet.InterceptorsFeatureSample.Middlewares.Decorators;
using PipelineNet.InterceptorsFeatureSample.Middlewares.Interceptors;
using PipelineNet.Middleware;
using PipelineNet.ServiceProvider;

namespace PipelineNet.InterceptorsFeatureSample.Middlewares;

[PublicAPI]
public static class MiddlewareServiceCollectionExtensions
{
    #region Interceptor Registration

    /// <summary>
    /// Registers a interceptor that will be applied to all container registered middlewares
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <typeparam name="TInterceptor">Type of the interceptor to register</typeparam>
    /// <returns></returns>
    public static IServiceCollection AddInterceptor<TInterceptor>(
        this IServiceCollection services)
        where TInterceptor : class, IInterceptor
    {
        return services.AddInterceptor(typeof(TInterceptor), typeof(object), true); // typeof(object) = global
    }

    /// <summary>
    /// Registers an interceptor by type, optionally bound to a specific middleware
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="interceptorType">Type of the interceptor to register</param>
    /// <param name="middlewareType">Interceptor associated middleware</param>
    /// <param name="registerConfiguration">Whether to additionally register a configuration instance containing the specified interceptor and middleware types information.
    /// Defaults to false, use this parameters when attempting to registration global interceptors that will be consumed by all registered middlewares </param>
    /// <returns></returns>
    /// <exception cref="ArgumentException">Invalid interceptor/middleware type specified</exception>
    internal static IServiceCollection AddInterceptor(
        this IServiceCollection services,
        Type interceptorType,
        Type? middlewareType = null,
        bool registerConfiguration = false)
    {
        if (!MiddlewareHelpers.ImplementsInterceptor(interceptorType))
            throw new ArgumentException($"Type {interceptorType.Name} is not a valid interceptor",
                nameof(interceptorType));

        // Use typeof(object) to represent global interceptors
        Type boundType = middlewareType ?? typeof(object);

        services.Scan(s => s
            .FromAssembliesOf(interceptorType)
            .AddClasses(c => c.Where(t => t == interceptorType))
            .AsImplementedInterfaces()
            .AsSelf()
            .WithScopedLifetime());


        if (!registerConfiguration)
            return services;

        // Add configuration entry
        return services.AddScoped<IMiddlewareInterceptorConfiguration>(_ =>
            new MiddlewareInterceptorConfiguration(boundType, ImmutableList.Create(interceptorType)));
    }

    /// <summary>
    /// Adds global logging interceptor
    /// </summary>
    public static IServiceCollection AddLogging(this IServiceCollection services) =>
        services.AddInterceptor(typeof(LoggingMiddlewareInterceptor<,>), typeof(object), true);

    #endregion

    #region Middleware Registration

    /// <summary>
    /// Registers a middleware without interceptor configuration
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="lifetime">Lifetime used to register the middleware on container</param>
    /// <typeparam name="TMiddleware">Type associated with the middleware to be registered</typeparam>
    public static IServiceCollection AddMiddleware<TMiddleware>(
        this IServiceCollection services,
        ServiceLifetime lifetime = ServiceLifetime.Scoped)
        where TMiddleware : class
    {
        return services.AddMiddleware<TMiddleware>(lifetime, null);
    }


    /// <summary>
    /// Registers a middleware with interceptor configuration
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="lifetime">Lifetime used to register the middleware on container</param>
    /// <param name="configure">action to configure interceptions for current registering middleware.
    /// </param>
    /// <typeparam name="TMiddleware">Type associated with the middleware to be registered</typeparam>
    public static IServiceCollection AddMiddleware<TMiddleware>(
        this IServiceCollection services,
        ServiceLifetime lifetime,
        Action<IMiddlewareInterceptorOptionsBuilder>? configure)
        where TMiddleware : class
    {
        Type middlewareType = typeof(TMiddleware);

        // Find all IAsyncMiddleware<,> interfaces implemented by the middleware
        Type[] middlewareInterfaces = middlewareType
            .GetInterfaces()
            .Where(MiddlewareHelpers.IsMiddlewareType)
            .ToArray();

        Guard.Against.Zero(middlewareInterfaces.Length, nameof(middlewareInterfaces));

        // Register with all implemented interfaces
        foreach (Type interfaceType in middlewareInterfaces)
            AddMiddleware(services, middlewareType, interfaceType, lifetime, configure);

        return services;
    }

    /// <summary>
    /// Registers a middleware with interceptor configuration and custom inputs and outputs
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="lifetime">Lifetime used to register the middleware on container</param>
    /// <param name="configure">action to configure interceptions for current registering middleware.
    /// </param>
    /// <typeparam name="TMiddleware">Type associated with the middleware to be registered</typeparam>
    /// <typeparam name="TParameter">Type associated with middleware expected inputs</typeparam>
    /// <typeparam name="TReturn">Type associated with the outcome generated from the middleware execution</typeparam>
    public static IServiceCollection AddMiddleware<TMiddleware, TParameter, TReturn>(
        this IServiceCollection services,
        ServiceLifetime lifetime = ServiceLifetime.Scoped,
        Action<IMiddlewareInterceptorOptionsBuilder>? configure = null)
        where TMiddleware : class, IAsyncMiddleware<TParameter, TReturn>
        => AddMiddleware(services, typeof(TMiddleware), typeof(IAsyncMiddleware<TParameter, TReturn>), lifetime,
            configure);

    /// <summary>
    /// Performs service registration and interceptors configuration for a given middleware
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="middlewareType">Middleware type to be registered on container</param>
    /// <param name="interfaceType">Interface type to register the middleware</param>
    /// <param name="lifetime">Lifetime used to register the middleware</param>
    /// <param name="configure">action to configure interceptions for current registering middleware.
    /// </param>
    /// <returns></returns>
    private static IServiceCollection AddMiddleware(
        this IServiceCollection services,
        Type middlewareType,
        Type interfaceType,
        ServiceLifetime lifetime = ServiceLifetime.Scoped,
        Action<IMiddlewareInterceptorOptionsBuilder>? configure = null)
    {
        // Register the middleware with its interface using Scrutor
        services.Scan(s => s
            .FromAssembliesOf(middlewareType)
            .AddClasses(c => c.Where(t => t == middlewareType))
            .As(interfaceType)
            .AsSelf()
            .WithLifetime(lifetime));

        // Configure local interceptors if provided
        if (configure == null)
            return services;

        MiddlewareInterceptorOptionsBuilder interceptorBuilder = new(services, middlewareType);
        configure.Invoke(interceptorBuilder);

        // Add interceptors configuration
        services.AddScoped<IMiddlewareInterceptorConfiguration>(_ => interceptorBuilder.BuildConfiguration());
        return services;
    }

    #endregion

    #region Assembly Scanning

    /// <summary>
    /// Registers all middlewares from multiple assemblies
    /// </summary>
    public static IServiceCollection AddMiddlewaresFromAssemblies(
        this IServiceCollection services,
        IEnumerable<Assembly> assemblies,
        ServiceLifetime lifetime = ServiceLifetime.Scoped)
    {
        return services.Scan(s => s.FromAssemblies(assemblies.ToArray())
            .AddClasses(c => c.Where(MiddlewareHelpers.ImplementsMiddlewareInterface))
            .AsSelfWithInterfaces(MiddlewareHelpers.IsMiddlewareType));
    }

    /// <summary>
    /// Registers all middlewares from the assembly containing the specified type
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="lifetime">Lifetime used to register interceptors found</param>
    /// <typeparam name="T">Type to get assembly from</typeparam>
    public static IServiceCollection AddMiddlewaresFromAssemblyContaining<T>(
        this IServiceCollection services,
        ServiceLifetime lifetime = ServiceLifetime.Scoped)
    {
        return services.AddMiddlewaresFromAssemblies(
            [typeof(T).Assembly],
            lifetime);
    }

    #endregion

    #region Interceptor Assembly Scanning

    /// <summary>
    /// Registers all middlewares from current entry assembly
    /// </summary>
    /// <param name="services">The service collection</param>
    public static IServiceCollection AddInterceptorsFromEntryAssembly(
        this IServiceCollection services)
    {
        Assembly entryAssembly = Guard.Against.Null(Assembly.GetEntryAssembly());
        return services.AddInterceptorsFromAssembly(entryAssembly);
    }

    /// <summary>
    /// Registers interceptors from the specified assembly
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="assembly">Assembly to lookup for interceptor types</param>
    /// <param name="includeInternals">Whether to also register interceptor types marked as internal. Defaults to false</param>
    public static IServiceCollection AddInterceptorsFromAssembly(
        this IServiceCollection services,
        Assembly assembly,
        bool includeInternals = false) =>
        services.AddInterceptorsFromAssemblies(includeInternals, [assembly]);

    /// <summary>
    /// Registers all interceptors from a given collection of assemblies
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="includeInternals">Whether to also register interceptor types marked as internal. Defaults to false</param>
    /// <param name="assemblies">Assemblies to lookup for interceptor types</param>
    public static IServiceCollection AddInterceptorsFromAssemblies(
        this IServiceCollection services,
        bool includeInternals = false,
        params Assembly[] assemblies)
    {
        assemblies ??= [];

        // Collect all interceptor types found in the assemblies
        ImmutableList<Type> interceptorTypes = assemblies
            .SelectMany(a => a.GetTypes())
            .Where(MiddlewareHelpers.ImplementsInterceptor)
            .ToImmutableList();

        // Register the global configuration with typeof(object) as middleware type
        if (!interceptorTypes.IsEmpty)
        {
            MiddlewareInterceptorConfiguration globalInterceptors = new MiddlewareInterceptorConfiguration(
                typeof(object),
                interceptorTypes);

            services.AddSingleton<IMiddlewareInterceptorConfiguration>(globalInterceptors);


            // Register all interceptor types in DI
            return services.Scan(s => s
                .FromTypes(interceptorTypes)
                .AddClasses() // No need to filter here since we already did when inspecting assembly types
                .AsImplementedInterfaces(MiddlewareHelpers.IsInterceptorType)
                .AsSelf()
                .WithScopedLifetime());
        }

        Debug.WriteLine("No interceptors found on specified assemblies, skipping registration");
        return services;
    }

    /// <summary>
    /// Registers all interceptors from a type containing assembly
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="includeInternals">Whether to also register interceptor types marked as internal. Defaults to false</param>
    public static IServiceCollection AddInterceptorsFromAssemblyContaining<T>(
        this IServiceCollection services,
        bool includeInternals = false)
    {
        return services.AddInterceptorsFromAssembly(typeof(T).Assembly, includeInternals);
    }

    #endregion
}