using PAN.Mediator.Abstractions;
using PAN.Mediator.Notifications;

namespace PAN.Mediator.Benchmarks.Fixtures;

public sealed class PanPing : IPanRequest<string>
{
    public static readonly PanPing Instance = new();
}

public sealed class PanPingHandler : IPanRequestHandler<PanPing, string>
{
    public Task<string> Handle(PanPing request, CancellationToken cancellationToken) => Task.FromResult("pong");
}

public sealed class PanPassThroughBehavior<TRequest, TResponse> : IPanPipelineBehavior<TRequest, TResponse>
    where TRequest : IPanRequest<TResponse>
{
    public Task<TResponse> Handle(
        TRequest request,
        PanRequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken
    ) => next();
}

public sealed class PanSingleNotification : IPanNotification
{
    public static readonly PanSingleNotification Instance = new();
}

public sealed class PanSingleNotificationHandler : IPanNotificationHandler<PanSingleNotification>
{
    public Task Handle(PanSingleNotification notification, CancellationToken cancellationToken) => Task.CompletedTask;
}

public sealed class PanThreeNotifications : IPanNotification
{
    public static readonly PanThreeNotifications Instance = new();
}

public sealed class PanThreeNotificationHandler1 : IPanNotificationHandler<PanThreeNotifications>
{
    public Task Handle(PanThreeNotifications notification, CancellationToken cancellationToken) => Task.CompletedTask;
}

public sealed class PanThreeNotificationHandler2 : IPanNotificationHandler<PanThreeNotifications>
{
    public Task Handle(PanThreeNotifications notification, CancellationToken cancellationToken) => Task.CompletedTask;
}

public sealed class PanThreeNotificationHandler3 : IPanNotificationHandler<PanThreeNotifications>
{
    public Task Handle(PanThreeNotifications notification, CancellationToken cancellationToken) => Task.CompletedTask;
}
