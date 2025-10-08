using PipelineNet.Middleware;

namespace PipelineNet.InterceptorsFeatureSample.Middlewares.Interceptors;

/// <summary>
/// Decorator that executes additional middlewares after the inner middleware completes
/// </summary>
/// <typeparam name="TParameter">The parameter type passed through the middleware</typeparam>
public class PostExecutionMiddlewareInterceptor<TParameter>(
    IAsyncMiddleware<TParameter> inner,
    params IAsyncMiddleware<TParameter, TParameter>[] postMiddlewares)
    : PostExecutionMiddlewareInterceptor<TParameter, TParameter>(inner, postMiddlewares);

public class PostExecutionMiddlewareInterceptor<TParameter, TReturn>(
    IAsyncMiddleware<TParameter> inner,
    params IAsyncMiddleware<TParameter, TParameter>[] postMiddlewares)
    : InterceptorBase<TParameter, TParameter>
{
    public override async Task AfterRunAsync(TParameter @return)
    {
        foreach (var postMiddleware in postMiddlewares)
            await postMiddleware.Run(@return, null);

        await base.AfterRunAsync(@return);
    }
}