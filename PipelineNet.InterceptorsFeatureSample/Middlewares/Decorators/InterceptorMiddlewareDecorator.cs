using Ardalis.GuardClauses;
using AutomaticInterface;
using PipelineNet.Middleware;

namespace PipelineNet.InterceptorsFeatureSample.Middlewares.Decorators;

public class InterceptorMiddlewareDecorator<TParameter>(
    IAsyncMiddleware<TParameter, TParameter> inner,
    params IInterceptorMiddlewareDecorator<TParameter, TParameter>[] interceptors) :
    InterceptorMiddlewareDecorator<TParameter, TParameter>(inner, interceptors);

/// <summary>
/// A decorator to control middlewares execution interception
/// </summary>
/// <typeparam name="TParameter"></typeparam>
/// <typeparam name="TReturn"></typeparam>
public class InterceptorMiddlewareDecorator<TParameter, TReturn> :
    IAsyncMiddleware<TParameter, TReturn>
{
    #region Fields

    private readonly IAsyncMiddleware<TParameter, TReturn> _inner;

    private readonly IEnumerable<IInterceptorMiddlewareDecorator<TParameter, TReturn>> _interceptors;

    #endregion

    /// <summary>
    /// A decorator to control middlewares execution interception
    /// </summary>
    /// <param name="inner"></param>
    /// <param name="interceptors"></param>
    /// <typeparam name="TParameter"></typeparam>
    /// <typeparam name="TReturn"></typeparam>
    public InterceptorMiddlewareDecorator(
        IAsyncMiddleware<TParameter, TReturn> inner,
        params IInterceptorMiddlewareDecorator<TParameter, TReturn>[] interceptors)
    {
        _ = Guard.Against.Null(inner, nameof(inner));
        _ = Guard.Against.Null(interceptors, nameof(interceptors));

        this._inner = inner;
        this._interceptors = interceptors;
    }

    /// <inheritdoc />
    [IgnoreAutomaticInterface]
    public async Task<TReturn> Run(TParameter parameter, Func<TParameter, Task<TReturn>> next)
    {
        foreach (IInterceptorMiddlewareDecorator<TParameter, TReturn> interceptor in this._interceptors)
            await interceptor.BeforeRunAsync(parameter);

        TReturn @return = await this._inner.Run(parameter, next);

        foreach (IInterceptorMiddlewareDecorator<TParameter, TReturn> interceptor in this._interceptors)
            await interceptor.AfterRunAsync(@return);

        return @return;
    }
}