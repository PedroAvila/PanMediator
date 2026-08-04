using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PAN.Mediator.Notifications
{
    /// <summary>
    /// Governs how a notification's registered handlers are invoked during Publish (e.g. sequential vs
    /// concurrent fan-out). Registered as an open generic, mirroring
    /// <c>PAN.Mediator.Abstractions.IPanPipelineBehavior{TRequest,TResponse}</c>, and resolved per concrete
    /// notification type from the same <see cref="System.IServiceProvider"/> used to resolve handlers.
    /// </summary>
    public interface IPanNotificationPublishStrategy<TNotification>
        where TNotification : IPanNotification
    {
        Task Publish(
            TNotification notification,
            IEnumerable<IPanNotificationHandler<TNotification>> handlers,
            CancellationToken cancellationToken
        );
    }
}
