using System.Collections.Immutable;
using Ardalis.GuardClauses;
using Microsoft.Extensions.DependencyInjection;
using PipelineNet.InterceptorsFeatureSample.Middlewares.Decorators;
using PipelineNet.MiddlewareResolver;

namespace PipelineNet.InterceptorsFeatureSample.Middlewares;

/// <summary>
/// A middleware resolver that supports interceptor decoration
/// </summary>
public sealed class InterceptorAwareMiddlewareResolver(IServiceProvider serviceProvider) : IMiddlewareResolver
{
    #region Fields

    private readonly IServiceProvider _serviceProvider = Guard.Against.Null(serviceProvider, nameof(serviceProvider));

    #endregion

    /// <inheritdoc />
    public MiddlewareResolverResult Resolve(Type type)
    {
        using IServiceScope scope = _serviceProvider.CreateScope();
        IServiceProvider sp = scope.ServiceProvider;

        MiddlewareResolverResult result = Guard.Against.Null(
            TryResolveDecorated(type, sp),
            nameof(type),
            "Failed attempt to resolve decorated middleware from service provider");

        return result;
    }

    private MiddlewareResolverResult? TryResolveDecorated(Type type, IServiceProvider sp)
    {
        bool resolved = this.TryResolveInternal(type, sp, out MiddlewareResolverResult? result);
        return result;
    }

    private bool TryResolveInternal(Type type, IServiceProvider sp, out MiddlewareResolverResult result)
    {
        result = null;
        if (type is { IsGenericType: true })
            type = type.GetGenericTypeDefinition();

        if (!MiddlewareHelpers.IsMiddlewareType(type) && !MiddlewareHelpers.ImplementsMiddlewareInterface(type))
            return false;

        // at this point we can guarantee that the specified type is a middleware so we can attempt to decorate it
        object? middleware = this.Decorate(sp, type);

        if (middleware == null)
        {
            throw new InvalidOperationException(
                $"Failed to resolve middleware of type {type.FullName}. " +
                "Ensure it is registered in the service collection.");
        }


        result = new MiddlewareResolverResult
        {
            Middleware = middleware,
            Dispose = false
        };

        return true;
    }

    private object? Decorate(IServiceProvider sp, Type middlewareType)
    {
        // Resolve the actual middleware instance
        object middleware = this.TryResolveMiddlewareFromProvider(sp, middlewareType);

        // Considering that we resolved the middleware from service container, its also necessary
        // to lookup on interceptor registry for any registered interceptor configuration
        // associated with current resolved middleware instance type
        IInterceptorRegistry registry = sp.GetService<IInterceptorRegistry>()!;

        if (!registry.HasInterceptors(middlewareType))
            return middleware;

        // Get interceptor types for this middleware
        ImmutableList<Type>? interceptorTypes = registry.GetInterceptorsForMiddleware(middlewareType);

        // Extract generic arguments from the interface
        Type[] genericArgs = middlewareType.GetGenericArguments();

        if (genericArgs.Length != 2)
            // TODO: current decorator logic was made focusing on IAsyncMiddleware and its related traits that have arity of 2
            //       it might be worth to return here and extend the support to decorate other middleware traits
            return middleware; // Can't decorate, return as-is

        Type inputType = genericArgs[0];
        Type returnType = genericArgs[1];

        // Resolve all interceptors
        object[] resolvedInterceptors = ResolveInterceptors(sp, interceptorTypes, inputType, returnType);

        Type decoratorType = typeof(InterceptorMiddlewareDecorator<,>).MakeGenericType(inputType, returnType);
        object? decorator = Activator.CreateInstance(decoratorType, middleware, resolvedInterceptors);

        return decorator;
    }

    private object TryResolveMiddlewareFromProvider(IServiceProvider sp, Type middlewareType)
    {
        _ = Guard.Against.InvalidInput(
            middlewareType,
            nameof(middlewareType),
            t => sp.GetService(t) != null,
            "The requested middleware is not present on current service provider");

        return sp.GetService(middlewareType)!;
    }

    private object[] ResolveInterceptors(
        IServiceProvider sp,
        ImmutableList<Type> interceptorTypes,
        Type inputType,
        Type returnType)
    {
        List<object> interceptors = new List<object>();

        foreach (Type interceptorType in interceptorTypes)
        {
            Type concreteType = interceptorType;

            // Handle open generic types
            if (interceptorType.IsGenericTypeDefinition)
            {
                concreteType = interceptorType.MakeGenericType(inputType, returnType);
            }

            // Try to resolve from DI
            Type interceptorInterfaceType =
                typeof(IInterceptorMiddlewareDecorator<,>).MakeGenericType(inputType, returnType);
            object? interceptor = sp.GetService(interceptorInterfaceType);

            if (interceptor != null)
            {
                interceptors.Add(interceptor);
            }
            else
            {
                // Fallback: create instance using ActivatorUtilities
                try
                {
                    object instance = ActivatorUtilities.CreateInstance(sp, concreteType);
                    interceptors.Add(instance);
                }
                catch
                {
                    // Skip interceptors that can't be created
                    continue;
                }
            }
        }

        return interceptors.ToArray();
    }
}