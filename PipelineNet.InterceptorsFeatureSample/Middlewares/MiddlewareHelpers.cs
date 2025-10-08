using Ardalis.GuardClauses;
using PipelineNet.Finally;
using PipelineNet.InterceptorsFeatureSample.Middlewares.Decorators;
using PipelineNet.Middleware;

namespace PipelineNet.InterceptorsFeatureSample.Middlewares;

internal static class MiddlewareHelpers
{
    private static readonly Type[] MiddlewareTypes =
    [
        typeof(IMiddleware<>),
        typeof(IAsyncMiddleware<>),
        typeof(ICancellableAsyncMiddleware<>),
        typeof(IMiddleware<,>),
        typeof(IAsyncMiddleware<,>),
        typeof(ICancellableAsyncMiddleware<,>),
        typeof(IFinally<,>),
        typeof(IAsyncFinally<,>),
        typeof(ICancellableAsyncFinally<,>)
    ];

    private static readonly Type[] InterceptorTypes =
    [
        typeof(IInterceptor<,>)
    ];

    /// <summary>
    /// Checks if the given type is a middleware interface type
    /// </summary>
    public static bool IsMiddlewareType(Type type)
    {
        Guard.Against.Null(type, nameof(type));

        if (!type.IsInterface || !type.IsGenericType)
            return false;

        Type genericTypeDefinition = type.GetGenericTypeDefinition();
        return MiddlewareTypes.Contains(genericTypeDefinition);
    }

    /// <summary>
    /// Checks if the given type is an interceptor interface type
    /// </summary>
    public static bool IsInterceptorType(Type type)
    {
        Guard.Against.Null(type, nameof(type));

        if (!type.IsInterface || !type.IsGenericType)
            return false;

        Type genericTypeDefinition = type.GetGenericTypeDefinition();
        return InterceptorTypes.Contains(genericTypeDefinition);
    }

    /// <summary>
    /// Checks if at least one interface of the given type is a middleware
    /// </summary>
    public static bool ImplementsMiddlewareInterface(Type type)
    {
        Guard.Against.Null(type, nameof(type));

        return type.GetInterfaces().Any(IsMiddlewareType);
    }

    /// <summary>
    /// Checks if at least one interface of the given type is an interceptor
    /// </summary>
    public static bool ImplementsInterceptor(Type type)
    {
        Guard.Against.Null(type, nameof(type));

        return type.GetInterfaces().Any(IsInterceptorType);
    }
}