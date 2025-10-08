using System.Collections.Immutable;
using AutomaticInterface;
using Microsoft.Extensions.DependencyInjection;

namespace PipelineNet.InterceptorsFeatureSample.Middlewares;

/// <summary>
/// Registry that maps middleware types to their interceptors
/// </summary>
[GenerateAutomaticInterface]
public sealed class InterceptorRegistry : IInterceptorRegistry
{
    private readonly Dictionary<Type, ImmutableList<Type>> _middlewareInterceptors;

    public InterceptorRegistry(IServiceScopeFactory serviceScopeFactory)
    {
        using IServiceScope scope = serviceScopeFactory.CreateScope();
        IEnumerable<IMiddlewareInterceptorConfiguration> configurations =
            scope.ServiceProvider.GetServices<IMiddlewareInterceptorConfiguration>();

        this._middlewareInterceptors = new Dictionary<Type, ImmutableList<Type>>();

        // Group all configurations by middleware type
        foreach (IMiddlewareInterceptorConfiguration config in configurations)
        {
            if (this._middlewareInterceptors.ContainsKey(config.MiddlewareType))
            {
                // Merge interceptors if multiple configurations exist for same middleware
                this._middlewareInterceptors[config.MiddlewareType] =
                    this._middlewareInterceptors[config.MiddlewareType].AddRange(config.InterceptorTypes);
            }
            else
            {
                this._middlewareInterceptors[config.MiddlewareType] = config.InterceptorTypes;
            }
        }
    }

    public ImmutableList<Type> GetInterceptorsForMiddleware(Type middlewareType)
    {
        // Get specific interceptors for this middleware
        ImmutableList<Type> specificInterceptors =
            this._middlewareInterceptors.TryGetValue(middlewareType, out ImmutableList<Type>? interceptors)
                ? interceptors
                : ImmutableList<Type>.Empty;

        // Get global interceptors (bound to null/typeof(object))
        ImmutableList<Type> globalInterceptors =
            this._middlewareInterceptors.TryGetValue(typeof(object), out ImmutableList<Type>? globals)
                ? globals
                : ImmutableList<Type>.Empty;

        // Combine: global first, then specific
        return globalInterceptors.AddRange(specificInterceptors);
    }

    public bool HasInterceptors(Type middlewareType)
    {
        return this.GetInterceptorsForMiddleware(middlewareType).Any();
    }
}