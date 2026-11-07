using PAN.Mediator.Abstractions;
using PAN.Mediator.Notifications;

namespace PAN.Mediator.Benchmarks.Fixtures;

public sealed class PanPing : IPanRequest<string>
{
    public static readonly PanPing Instance = new();
}

public sealed class PanPingHandler : IPanRequestHandler<PanPing, string>
{
    public ValueTask<string> Handle(PanPing request, CancellationToken cancellationToken) => new ValueTask<string>("pong");
}

public sealed class PanPassThroughBehavior<TRequest, TResponse> : IPanPipelineBehavior<TRequest, TResponse>
{
    public ValueTask<TResponse> Handle(
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
    public ValueTask Handle(PanSingleNotification notification, CancellationToken cancellationToken) => default;
}

public sealed class PanThreeNotifications : IPanNotification
{
    public static readonly PanThreeNotifications Instance = new();
}

public sealed class PanThreeNotificationHandler1 : IPanNotificationHandler<PanThreeNotifications>
{
    public ValueTask Handle(PanThreeNotifications notification, CancellationToken cancellationToken) => default;
}

public sealed class PanThreeNotificationHandler2 : IPanNotificationHandler<PanThreeNotifications>
{
    public ValueTask Handle(PanThreeNotifications notification, CancellationToken cancellationToken) => default;
}

public sealed class PanThreeNotificationHandler3 : IPanNotificationHandler<PanThreeNotifications>
{
    public ValueTask Handle(PanThreeNotifications notification, CancellationToken cancellationToken) => default;
}
