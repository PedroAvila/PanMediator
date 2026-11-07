using MediatR;

namespace PAN.Mediator.Benchmarks.Fixtures;

public sealed class MediatRPing : IRequest<string>
{
    public static readonly MediatRPing Instance = new();
}

public sealed class MediatRPingHandler : IRequestHandler<MediatRPing, string>
{
    public Task<string> Handle(MediatRPing request, CancellationToken cancellationToken) => Task.FromResult("pong");
}

public sealed class MediatRPassThroughBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken
    ) => next();
}

public sealed class MediatRSingleNotification : INotification
{
    public static readonly MediatRSingleNotification Instance = new();
}

public sealed class MediatRSingleNotificationHandler : INotificationHandler<MediatRSingleNotification>
{
    public Task Handle(MediatRSingleNotification notification, CancellationToken cancellationToken) => Task.CompletedTask;
}

public sealed class MediatRThreeNotifications : INotification
{
    public static readonly MediatRThreeNotifications Instance = new();
}

public sealed class MediatRThreeNotificationHandler1 : INotificationHandler<MediatRThreeNotifications>
{
    public Task Handle(MediatRThreeNotifications notification, CancellationToken cancellationToken) => Task.CompletedTask;
}

public sealed class MediatRThreeNotificationHandler2 : INotificationHandler<MediatRThreeNotifications>
{
    public Task Handle(MediatRThreeNotifications notification, CancellationToken cancellationToken) => Task.CompletedTask;
}

public sealed class MediatRThreeNotificationHandler3 : INotificationHandler<MediatRThreeNotifications>
{
    public Task Handle(MediatRThreeNotifications notification, CancellationToken cancellationToken) => Task.CompletedTask;
}
