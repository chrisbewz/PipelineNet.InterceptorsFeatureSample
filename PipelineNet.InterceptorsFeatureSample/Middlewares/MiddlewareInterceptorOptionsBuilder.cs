using System.Collections.Immutable;
using AutomaticInterface;
using Microsoft.Extensions.DependencyInjection;
using PipelineNet.InterceptorsFeatureSample.Middlewares.Decorators;
using PipelineNet.InterceptorsFeatureSample.Middlewares.Interceptors;

namespace PipelineNet.InterceptorsFeatureSample.Middlewares;

[GenerateAutomaticInterface]
public sealed class MiddlewareInterceptorOptionsBuilder(IServiceCollection services, Type middlewareType)
    : IMiddlewareInterceptorOptionsBuilder
{
    private ImmutableList<Type> _interceptors = ImmutableList<Type>.Empty;

    /// <summary>
    /// Adds an interceptor by type
    /// </summary>
    public IMiddlewareInterceptorOptionsBuilder AddInterceptor(Type interceptorType)
    {
        // Forward to service collection extension with middleware type binding
        services.AddInterceptor(interceptorType, middlewareType);

        if (!_interceptors.Contains(interceptorType))
        {
            _interceptors = _interceptors.Add(interceptorType);
        }

        return this;
    }

    /// <summary>
    /// Adds an interceptor by generic type
    /// </summary>
    public IMiddlewareInterceptorOptionsBuilder AddInterceptor<TInterceptor>()
        where TInterceptor : class, IInterceptor
    {
        return AddInterceptor(typeof(TInterceptor));
    }

    /// <summary>
    /// Adds an interceptor with specific parameter and return types
    /// </summary>
    public IMiddlewareInterceptorOptionsBuilder AddInterceptor<TInterceptor, TParameter, TReturn>()
        where TInterceptor : class, IInterceptorMiddlewareDecorator<TParameter, TReturn>
    {
        return AddInterceptor(typeof(TInterceptor));
    }

    /// <summary>
    /// Adds multiple interceptors
    /// </summary>
    public IMiddlewareInterceptorOptionsBuilder AddInterceptors(params Type[] interceptorTypes)
    {
        foreach (Type interceptorType in interceptorTypes)
        {
            AddInterceptor(interceptorType);
        }

        return this;
    }

    /// <summary>
    /// Adds the logging interceptor
    /// </summary>
    public IMiddlewareInterceptorOptionsBuilder AddLogging()
    {
        return AddInterceptor(typeof(LoggingMiddlewareInterceptor<,>));
    }

    /// <summary>
    /// Gets the configured interceptor types
    /// </summary>
    internal ImmutableList<Type> GetInterceptors() => _interceptors;

    public IMiddlewareInterceptorConfiguration BuildConfiguration() =>
        new MiddlewareInterceptorConfiguration(middlewareType, this._interceptors);
}