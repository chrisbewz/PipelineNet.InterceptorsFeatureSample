using PipelineNet.Middleware;

namespace PipelineNet.InterceptorsFeatureSample.Middlewares.Interceptors;

/// <summary>
/// Decorator that executes additional middlewares before the inner middleware runs
/// </summary>
/// <typeparam name="TParameter">The parameter type passed through the middleware</typeparam>
public class PreExecutionMiddlewareInterceptor<TParameter>(
    IAsyncMiddleware<TParameter, TParameter> inner,
    params IAsyncMiddleware<TParameter, TParameter>[] preMiddlewares)
    : PreExecutionMiddlewareInterceptor<TParameter, TParameter>(inner, preMiddlewares);

public class PreExecutionMiddlewareInterceptor<TParameter, TReturn>(
    IAsyncMiddleware<TParameter, TReturn> inner,
    params IAsyncMiddleware<TParameter, TReturn>[] preMiddlewares)
    : InterceptorBase<TParameter, TReturn>
{
    public override async Task BeforeRunAsync(TParameter parameter)
    {
        foreach (var preMiddleware in preMiddlewares)
            await preMiddleware.Run(parameter, null);
        await base.BeforeRunAsync(parameter);
    }
}