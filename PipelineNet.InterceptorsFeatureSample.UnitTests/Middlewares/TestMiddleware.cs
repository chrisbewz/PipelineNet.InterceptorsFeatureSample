using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PipelineNet.InterceptorsFeatureSample.UnitTests.Interceptors;
using PipelineNet.Middleware;

namespace PipelineNet.InterceptorsFeatureSample.UnitTests.Middlewares;

public sealed class TestMiddleware(ILoggerFactory loggerFactory)
    : IAsyncMiddleware<IncomingInterceptionContext, OutcomingInterceptionContext>
{
    #region Fields

    private readonly ILogger<TestMiddleware> _logger = loggerFactory.CreateLogger<TestMiddleware>();

    #endregion

    public async Task<OutcomingInterceptionContext> Run(IncomingInterceptionContext parameter,
        Func<IncomingInterceptionContext, Task<OutcomingInterceptionContext>> next)
    {
        this._logger.LogDebug("{MiddlewareName} is running", this.GetType().Name);
        Task.Delay(TimeSpan.FromSeconds(1));
        this._logger.LogDebug("{MiddlewareName} finished running", this.GetType().Name);
        parameter.ExecutedMiddlewares.Add(this.GetType());
        _ = await next(parameter);
        return new OutcomingInterceptionContext() { SourceContext = parameter };
    }
}