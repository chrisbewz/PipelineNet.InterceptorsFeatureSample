using System.Collections.Immutable;
using Ardalis.GuardClauses;
using Microsoft.Extensions.DependencyInjection;
using PipelineNet.InterceptorsFeatureSample.Middlewares.Decorators;
using PipelineNet.Middleware;
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

    // TODO: current decorator logic was made focusing on IAsyncMiddleware and its related traits that have arity of 2
    //       it might be worth to return here and extend the support to decorate other middleware traits
    private object? Decorate(IServiceProvider sp, Type middlewareType)
    {
        Predicate<Type> decorationConditions = t =>
        {
            if (middlewareType.IsInterface || middlewareType.IsGenericType || middlewareType.IsGenericTypeDefinition)
                return t.GetGenericTypeDefinition() == typeof(IAsyncMiddleware<,>);
            return MiddlewareHelpers.ImplementsMiddlewareInterface(t) && t.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IAsyncMiddleware<,>));
        };
        
        _ = Guard.Against.InvalidInput(middlewareType,
            nameof(middlewareType),
            t => decorationConditions(t),
            "Expected a middleware type that implements IAsyncMiddleware<,>. Other middleware traits are not supported yet.");
        
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
        // At this point we can guarantee that the middleware implements IAsyncMiddleware<,> so its safe to attempt extracting its arguments from runtime type
        var genericArgs = TryGetGenericArguments(middlewareType);

        // Another guard to ensure we have exactly 2 generic arguments since we are only supporting IAsyncMiddleware<,> for now
        _ = Guard.Against.InvalidInput(genericArgs.Length, nameof(genericArgs),c  => c == 2);
        
        Type inputType = genericArgs[0];
        Type returnType = genericArgs[1];

        // Resolve all interceptors
        object[] resolvedInterceptors = ResolveInterceptors(sp, interceptorTypes, inputType, returnType);

        Type decoratorType = typeof(InterceptorMiddlewareDecorator<,>).MakeGenericType(inputType, returnType);
        
        Array typedInterceptorArray = Array.CreateInstance(typeof(IInterceptorMiddlewareDecorator<,>).MakeGenericType(inputType, returnType), resolvedInterceptors.Length);
        Array.Copy(resolvedInterceptors, typedInterceptorArray, resolvedInterceptors.Length);
        
        object? decorator = Activator.CreateInstance(decoratorType, middleware, typedInterceptorArray);

        return decorator;
    }

    private static Type[] TryGetGenericArguments(Type middlewareType)
    {
        Type inner = middlewareType;

        return inner.IsInterface ? inner.GetGenericArguments() : inner.GetInterfaces().First(it => it.IsGenericType && it.GetGenericTypeDefinition() == typeof(IAsyncMiddleware<,>)).GetGenericArguments();
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
            object? interceptor = null;
            // Handle open generic types
            if (interceptorType.IsGenericTypeDefinition)
            {
                concreteType = interceptorType.MakeGenericType(inputType, returnType);
                
                // This should work for generic types as long 
                // since scrutor which is used when calling AddInterceptor methods from MiddlewareServiceCollectionExtensions
                // is expected to register InterceptorBase<TParameter, TReturn> implementations with its implemented interfaces too,
                // which does include IInterceptorMiddlewareDecorator<,>
                Type interceptorInterfaceType =
                    typeof(IInterceptorMiddlewareDecorator<,>).MakeGenericType(inputType, returnType);
                interceptor = sp.GetService(interceptorInterfaceType);
            }
            else
                // Fallback to concrete type
                interceptor = sp.GetService(concreteType);

            if (interceptor != null)
                interceptors.Add(interceptor);
            // As last option try to create instance using ActivatorUtilities
            else
            {
                try
                {
                    object instance = ActivatorUtilities.CreateInstance(sp, concreteType);
                    interceptors.Add(instance);
                }
                catch
                {
                    // Ignored on this sample but might be worth to handle errors here somehow
                    continue;
                }
            }
        }

        return interceptors.ToArray();
    }
}