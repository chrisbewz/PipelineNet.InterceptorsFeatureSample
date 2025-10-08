using Microsoft.Extensions.DependencyInjection;
using PipelineNet.InterceptorsFeatureSample.Middlewares.Decorators;
using PipelineNet.Middleware;

namespace PipelineNet.InterceptorsFeatureSample.Middlewares;

/// <summary>
/// Factory for creating middleware instances with interceptors
/// </summary>
public interface IMiddlewareFactory
{
    TMiddleware Create<TMiddleware>() where TMiddleware : class;
    object Create(Type middlewareType);
}

public sealed class MiddlewareFactory(IServiceProvider serviceProvider) : IMiddlewareFactory
{
    public TMiddleware Create<TMiddleware>() where TMiddleware : class
    {
        return (TMiddleware)Create(typeof(TMiddleware));
    }

    public object Create(Type middlewareType)
    {
        // Get the base middleware instance
        object middleware = serviceProvider.GetRequiredService(middlewareType);

        // Check if we need to wrap with interceptors
        IEnumerable<IMiddlewareInterceptorConfiguration> configurations =
            serviceProvider.GetServices<IMiddlewareInterceptorConfiguration>()
                .Where(c => c.MiddlewareType == middlewareType);

        List<Type> allInterceptorTypes = new();

        // Add middleware-specific interceptors
        foreach (IMiddlewareInterceptorConfiguration config in configurations)
        {
            allInterceptorTypes.AddRange(config.InterceptorTypes);
        }

        if (allInterceptorTypes.Count == 0)
        {
            return middleware;
        }

        // Wrap with interceptors
        return WrapWithInterceptors(middleware, middlewareType, allInterceptorTypes);
    }

    private object WrapWithInterceptors(object middleware, Type middlewareType, List<Type> interceptorTypes)
    {
        // Find the IAsyncMiddleware<,> interface
        Type? asyncMiddlewareInterface = middlewareType.GetInterfaces()
            .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IAsyncMiddleware<,>));

        if (asyncMiddlewareInterface == null)
        {
            return middleware;
        }

        Type[] genericArgs = asyncMiddlewareInterface.GetGenericArguments();
        Type parameterType = genericArgs[0];
        Type returnType = genericArgs[1];

        // Create interceptor instances
        List<object> interceptors = new();

        foreach (Type interceptorType in interceptorTypes)
        {
            // Handle generic interceptor types (like LoggingMiddlewareInterceptor<,>)
            Type concreteInterceptorType = interceptorType.IsGenericTypeDefinition
                ? interceptorType.MakeGenericType(parameterType, returnType)
                : interceptorType;

            // Resolve the interceptor from DI
            Type interceptorInterfaceType = typeof(IInterceptorMiddlewareDecorator<,>)
                .MakeGenericType(parameterType, returnType);

            object? interceptor = serviceProvider.GetService(interceptorInterfaceType);

            if (interceptor != null)
            {
                interceptors.Add(interceptor);
            }
        }

        if (interceptors.Count == 0)
        {
            return middleware;
        }

        // Create the decorator
        Type decoratorType = typeof(InterceptorMiddlewareDecorator<,>)
            .MakeGenericType(parameterType, returnType);

        object decorator = Activator.CreateInstance(
            decoratorType,
            middleware,
            interceptors.ToArray())!;

        return decorator;
    }
}