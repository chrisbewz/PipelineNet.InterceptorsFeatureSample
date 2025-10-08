# Feature : Middleware/Pipeline Interceptors Support 

This repository demonstrates a proposed feature for [PipelineNet](https://github.com/ipvalverde/PipelineNet): first-class interceptor support for middlewares. 

The goal is to enable pre/post middlewares execution behaviors around the existing traits, this way cross-cutting concerns can be added as needed on pipelines.

## Concept

1. Middlewares are decorated only when associated interceptors exist on DI container.
2. All Interceptors are registered on DI container setup
3. Interceptors can execute arbitrary code before/after middlewares.
4. Interceptors can be configured globally or per-middleware.
5. Interceptors can fail without affecting the pipeline execution. (missing implementation)
6. The decoration logic is concentrated on custom middleware resolver implementation (`InterceptorAwareMiddlewareResolver`), avoiding any changes to existing middleware design choices.
7. interceptors configuration associations are stored and accessible through `IInterceptorRegistry`.
8. All interceptors are registered as scoped services to ensure that they are primarily accessible via IMiddlewareResolver only

## Repo contents

- Interceptor-aware resolver
  - `InterceptorAwareMiddlewareResolver` decorates resolved middlewares with configured interceptors when any is found.
  - `IInterceptorRegistry` store middleware-interceptor associations to further resolution from DI container
  - `IMiddlewareInterceptorConfiguration` associates a middleware with a set of interceptors.

- Custom Registration extensions
  - `AddMiddleware<TMiddleware>(...)` with optional per-middleware interceptor configuration.
  - `AddInterceptor<TInterceptor>()` for global interceptors; `AddLogging()` convenience method.
  - Extra assembly scan helpers to register middlewares and interceptors.

- Decorator & contracts
  - `InterceptorMiddlewareDecorator<TIn, TOut>` that wraps `IAsyncMiddleware<TIn, TOut>`.
  - `IInterceptor` and `IInterceptorMiddlewareDecorator<TIn, TOut>` contracts for building custom interceptors.
  - `IInterceptorRegistry` and `IMiddlewareInterceptorConfiguration` to map middlewares to interceptors.

!!! 
    Note: the current prototype focuses on `IAsyncMiddleware<TIn, TOut>`; other traits can be added incrementally.

## Next steps
- Extend interception support to pipelines and chains of responsibility traits (might need some refactoring on base interfaces)
- Accept other middleware traits beyond `IAsyncMiddleware<,>` (related to previous point)
- Provide built-in interceptors (logging, metrics, caching, retries) through optional packages.
