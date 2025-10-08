using PipelineNet.InterceptorsFeatureSample.Middlewares.Decorators;

namespace PipelineNet.InterceptorsFeatureSample.Middlewares.Interceptors;

/// <summary>
///     A base class for a service that is used to intercept calls to the middlewares
///     before they hit the underlying storage.
/// </summary>
public abstract class InterceptorBase<TParameter, TReturn> : IInterceptorMiddlewareDecorator<TParameter, TReturn>
{
    /// <inheritdoc />
    public virtual Task BeforeRunAsync(TParameter parameter)
    {
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public virtual Task AfterRunAsync(TReturn @return) => Task.CompletedTask;
}