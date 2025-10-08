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
    public override Task BeforeRunAsync(IncomingInterceptionContext parameter)
    {
        interceptorCounter.BeforeRunCalled++;
        return base.BeforeRunAsync(parameter);
    }

    public override Task AfterRunAsync(OutcomingInterceptionContext parameter)
    {
        parameter.SourceContext.ExecutedInterceptors.Add(this.GetType());
        interceptorCounter.AfterRunCalled++;
        return base.AfterRunAsync(parameter);
    }
}