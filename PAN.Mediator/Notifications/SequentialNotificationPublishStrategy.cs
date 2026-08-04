using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PAN.Mediator.Notifications
{
    /// <summary>
    /// Invokes handlers one at a time, in enumeration order, awaiting each before starting the next.
    /// If a handler throws, remaining handlers are never invoked and the exception propagates immediately —
    /// this is PAN.Mediator's default, unconfigured Publish behavior.
    /// </summary>
    public sealed class SequentialNotificationPublishStrategy<TNotification> : IPanNotificationPublishStrategy<TNotification>
        where TNotification : IPanNotification
    {
        public async Task Publish(
            TNotification notification,
            IEnumerable<IPanNotificationHandler<TNotification>> handlers,
            CancellationToken cancellationToken
        )
        {
            foreach (var handler in handlers)
            {
                await handler.Handle(notification, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
