using System;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using PipelineNet.InterceptorsFeatureSample.Middlewares;
using PipelineNet.InterceptorsFeatureSample.Middlewares.Decorators;
using PipelineNet.InterceptorsFeatureSample.UnitTests.Helpers;
using PipelineNet.InterceptorsFeatureSample.UnitTests.Interceptors;
using PipelineNet.Middleware;
using PipelineNet.MiddlewareResolver;
using Shouldly;

namespace PipelineNet.InterceptorsFeatureSample.UnitTests.Middlewares;

[TestSubject(typeof(InterceptorAwareMiddlewareResolver))]
public class InterceptorAwareMiddlewareResolverTest : TestBase
{
    #region Fields

    private readonly IServiceProvider _serviceProvider = BuildServiceProvider(collection =>
    {
        collection.AddSingleton<IMiddlewareResolver, InterceptorAwareMiddlewareResolver>();
        collection.AddSingleton<IInterceptorRegistry, InterceptorRegistry>();
        collection.AddSingleton<InterceptorCounter>();
        collection.AddMiddlewaresFromAssemblies([typeof(CountingInterceptor).Assembly]);
        collection.AddInterceptorsFromAssemblies(false, [typeof(CountingInterceptor).Assembly]);
    });

    #endregion

    [Fact]
    public void ShouldInterceptBefore()
    {
        IAsyncMiddleware<IncomingInterceptionContext, OutcomingInterceptionContext> testMiddleware = ResolveDecorated<IncomingInterceptionContext, OutcomingInterceptionContext>(typeof(TestMiddleware));

        testMiddleware.ShouldNotBeNull();
        testMiddleware.GetType().GetGenericTypeDefinition().ShouldBe(typeof(InterceptorMiddlewareDecorator<,>));
        
        OutcomingInterceptionContext outcomingInterceptionContext = testMiddleware
            .Run(new IncomingInterceptionContext(), _ => Task.FromResult(new OutcomingInterceptionContext()))
            .GetAwaiter().GetResult();

        outcomingInterceptionContext.SourceContext.ExecutedInterceptors.Count.ShouldBe(1);
        outcomingInterceptionContext.SourceContext.ExecutedMiddlewares.Count.ShouldBe(1);
        
        InterceptorCounter counter = this._serviceProvider.GetRequiredService<InterceptorCounter>();
        counter.BeforeRunCalled.ShouldBe(1);
    }
    
    [Fact]
    public void ShouldInterceptAfter()
    {
        IAsyncMiddleware<IncomingInterceptionContext, OutcomingInterceptionContext> testMiddleware = ResolveDecorated<IncomingInterceptionContext, OutcomingInterceptionContext>(typeof(TestMiddleware));

        testMiddleware.ShouldNotBeNull();
        testMiddleware.GetType().GetGenericTypeDefinition().ShouldBe(typeof(InterceptorMiddlewareDecorator<,>));
        
        OutcomingInterceptionContext outcomingInterceptionContext = testMiddleware
            .Run(new IncomingInterceptionContext(), _ => Task.FromResult(new OutcomingInterceptionContext()))
            .GetAwaiter().GetResult();

        outcomingInterceptionContext.SourceContext.ExecutedInterceptors.Count.ShouldBe(1);
        outcomingInterceptionContext.SourceContext.ExecutedMiddlewares.Count.ShouldBe(1);
        
        InterceptorCounter counter = this._serviceProvider.GetRequiredService<InterceptorCounter>();
        counter.BeforeRunCalled.ShouldBe(1);
    }
    
    [Fact]
    public void ShouldResolveFromGenericTypeDefinition()
    {
        IAsyncMiddleware<IncomingInterceptionContext, OutcomingInterceptionContext> testMiddleware = 
            ResolveDecorated<IncomingInterceptionContext, OutcomingInterceptionContext>(typeof(IAsyncMiddleware<,>).MakeGenericType(typeof(IncomingInterceptionContext), typeof(OutcomingInterceptionContext)));

        testMiddleware.ShouldNotBeNull();
        testMiddleware.GetType().GetGenericTypeDefinition().ShouldBe(typeof(InterceptorMiddlewareDecorator<,>));
    }

    private IAsyncMiddleware<TIn, TOut> ResolveDecorated<TIn, TOut>(Type t)
    {
        IMiddlewareResolver middlewareResolver = this._serviceProvider.GetRequiredService<IMiddlewareResolver>();

        // Maybe a generic resolve method on middleware resolver would be better to avoid the explicit cast?
        IAsyncMiddleware<TIn, TOut> testMiddleware =
            (IAsyncMiddleware<TIn, TOut>)middlewareResolver.Resolve(t).Middleware;
        return testMiddleware;
    }
}