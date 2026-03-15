using FastEndpoints;

namespace ScoreCast.Ws.Application.V1.Interfaces;

public interface IQuery : ICommand;

public interface IQuery<out TResult> : ICommand<TResult>;

public interface IQueryHandler<in TQuery, TResult> : ICommandHandler<TQuery, TResult>
    where TQuery : ICommand<TResult>;
