using System.Threading.Tasks;
using PipelineNet.InterceptorsFeatureSample.Middlewares.Interceptors;

namespace PipelineNet.InterceptorsFeatureSample.UnitTests.Interceptors;

public record InterceptorCounter
{
    public int BeforeRunCalled { get; set; }

    public int AfterRunCalled { get; set; }
}

public sealed class CountingInterceptor(InterceptorCounter interceptorCounter)
    : InterceptorBase<IncomingInterceptionContext, OutcomingInterceptionContext>
{
    private readonly InterceptorCounter _interceptorCounter = interceptorCounter;

    public override Task BeforeRunAsync(IncomingInterceptionContext parameter)
    {
        this._interceptorCounter.BeforeRunCalled++;
        return base.BeforeRunAsync(parameter);
    }

    public override Task AfterRunAsync(OutcomingInterceptionContext parameter)
    {
        parameter.SourceContext.ExecutedInterceptors.Add(this.GetType());
        this._interceptorCounter.AfterRunCalled++;
        return base.AfterRunAsync(parameter);
    }
}