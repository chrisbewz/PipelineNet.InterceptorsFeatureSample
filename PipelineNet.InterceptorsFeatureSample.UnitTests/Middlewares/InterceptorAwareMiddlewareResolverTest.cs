using System;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using PipelineNet.InterceptorsFeatureSample.Middlewares;
using PipelineNet.InterceptorsFeatureSample.UnitTests.Helpers;
using PipelineNet.InterceptorsFeatureSample.UnitTests.Interceptors;
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
        collection.AddMiddlewaresFromAssemblies([typeof(CountingInterceptor).Assembly]);
        collection.AddInterceptorsFromAssemblies(false, [typeof(CountingInterceptor).Assembly]);
    });

    #endregion

    [Fact]
    public void ShouldInterceptBefore()
    {
        IMiddlewareResolver middlewareResolver = this._serviceProvider.GetRequiredService<IMiddlewareResolver>();
        TestMiddleware? testMiddleware =
            middlewareResolver.Resolve(typeof(TestMiddleware)).Middleware as TestMiddleware;

        Assert.NotNull(testMiddleware);

        OutcomingInterceptionContext outcomingInterceptionContext = testMiddleware
            .Run(new IncomingInterceptionContext(), _ => Task.FromResult(new OutcomingInterceptionContext()))
            .GetAwaiter().GetResult();

        outcomingInterceptionContext.SourceContext.ExecutedInterceptors.Count.ShouldBe(1);
        outcomingInterceptionContext.SourceContext.ExecutedMiddlewares.Count.ShouldBe(1);
    }
}