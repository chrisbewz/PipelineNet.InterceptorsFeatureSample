using PipelineNet.Middleware;

namespace PipelineNet.InterceptorsFeatureSample.Middlewares;

/// <summary>
/// Wrapper midleware to execute a specified action on a chain of responsibility
/// </summary>
/// <param name="action"></param>
/// <typeparam name="TContext"></typeparam>
public sealed class ActionMiddleware<TContext>(Action<TContext> action) : IMiddleware<TContext>
{
    #region Fields

    private readonly Action<TContext> _action = action;

    #endregion

    /// <inheritdoc />
    public void Run(TContext parameter, Action<TContext> next)
    {
        _action.Invoke(parameter);
        next.Invoke(parameter);
    }

    public static IMiddleware<TContext> Create(Action<TContext> action) => new ActionMiddleware<TContext>(action);
}

/// <summary>
/// Wrapper midleware to execute a specified action on a chain of responsibility
/// </summary>
/// <param name="action"></param>
/// <typeparam name="TContext"></typeparam>
/// <typeparam name="TReturn"></typeparam>
public sealed class ActionMiddleware<TContext, TReturn>(Func<TContext, TReturn> action) : IMiddleware<TContext, TReturn>
{
    #region Fields

    private readonly Func<TContext, TReturn> _action = action;

    #endregion

    public TReturn Run(TContext parameter, Func<TContext, TReturn> next)
    {
        _action.Invoke(parameter);
        return next.Invoke(parameter);
    }

    public static IMiddleware<TContext, TReturn> Create(Func<TContext, TReturn> action) =>
        new ActionMiddleware<TContext, TReturn>(action);
}