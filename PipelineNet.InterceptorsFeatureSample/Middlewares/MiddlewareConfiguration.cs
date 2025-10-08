using System.Collections.Immutable;
using AutomaticInterface;

namespace PipelineNet.InterceptorsFeatureSample.Middlewares;

[GenerateAutomaticInterface]
internal sealed class MiddlewareInterceptorConfiguration(Type middlewareType, ImmutableList<Type> interceptorTypes)
    : IMiddlewareInterceptorConfiguration
{
    public Type MiddlewareType { get; } = middlewareType;
    public ImmutableList<Type> InterceptorTypes { get; } = interceptorTypes ?? ImmutableList<Type>.Empty;
}