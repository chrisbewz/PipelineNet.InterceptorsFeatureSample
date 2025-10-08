using System.Diagnostics;
using Microsoft.Extensions.Logging;
using PipelineNet.Middleware;

namespace PipelineNet.InterceptorsFeatureSample.Middlewares.Interceptors;

/// <summary>
/// Decorator that logs the execution of a middleware
/// </summary>
/// <typeparam name="TParameter">The parameter type passed through the middleware</typeparam>
public sealed class LoggingMiddlewareInterceptor<TParameter>(
    IAsyncMiddleware<TParameter, TParameter> inner,
    ILoggerFactory loggerFactory)
    : LoggingMiddlewareInterceptor<TParameter, TParameter>(inner, loggerFactory);

/// <summary>
/// Decorator that logs the execution of a middleware
/// </summary>
/// <typeparam name="TParameter">The parameter type passed through the middleware</typeparam>
/// <typeparam name="TReturn">The type returned from pipeline</typeparam>
public class LoggingMiddlewareInterceptor<TParameter, TReturn>(
    IAsyncMiddleware<TParameter, TReturn> inner,
    ILoggerFactory loggerFactory,
    string? middlewareName = null) : InterceptorBase<TParameter, TReturn>
{
    #region Fields

    private readonly string _middlewareName = middlewareName ?? inner.GetType().Name;

    private Stopwatch _stopwatch;

    private readonly ILogger<LoggingMiddlewareInterceptor<TParameter, TReturn>> _logger =
        loggerFactory.CreateLogger<LoggingMiddlewareInterceptor<TParameter, TReturn>>();

    #endregion

    /// <inheritdoc />
    public override Task BeforeRunAsync(TParameter parameter)
    {
        this._logger.LogInformation("Starting execution of middleware: {MiddlewareName}", this._middlewareName);

        this._stopwatch = Stopwatch.StartNew();
        return base.BeforeRunAsync(parameter);
    }

    /// <inheritdoc />
    public override Task AfterRunAsync(TReturn @return)
    {
        this._stopwatch.Stop();
        this._logger.LogInformation(
            "Completed execution of middleware: {MiddlewareName} in {ElapsedMs}ms",
            this._middlewareName,
            this._stopwatch.ElapsedMilliseconds);
        return base.AfterRunAsync(@return);
    }
}