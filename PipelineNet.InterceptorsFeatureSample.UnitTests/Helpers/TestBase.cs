using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace PipelineNet.InterceptorsFeatureSample.UnitTests.Helpers;

public abstract class TestBase
{
    protected static IServiceProvider BuildServiceProvider(Action<IServiceCollection> configure)
    {
        IServiceCollection services = new ServiceCollection();

        services.AddLogging(builder =>
        {
            builder.SetMinimumLevel(LogLevel.Trace);
            builder.AddConsole();
        });

        configure.Invoke(services);

        return services.BuildServiceProvider();
    }
}