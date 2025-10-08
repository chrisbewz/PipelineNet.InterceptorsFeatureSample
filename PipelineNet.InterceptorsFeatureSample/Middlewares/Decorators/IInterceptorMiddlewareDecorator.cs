namespace PipelineNet.InterceptorsFeatureSample.Middlewares.Decorators;

public partial interface IInterceptorMiddlewareDecorator<TParameter, TReturn> : IInterceptor<TParameter, TReturn>
{
    public Task BeforeRunAsync(TParameter parameter);
    public Task AfterRunAsync(TReturn parameter);
}