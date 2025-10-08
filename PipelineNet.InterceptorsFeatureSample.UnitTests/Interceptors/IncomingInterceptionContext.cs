using System;
using System.Collections.Generic;

namespace PipelineNet.InterceptorsFeatureSample.UnitTests.Interceptors;

public class IncomingInterceptionContext
{
    public List<Type> ExecutedInterceptors { get; } = [];

    public List<Type> ExecutedMiddlewares { get; } = [];
}