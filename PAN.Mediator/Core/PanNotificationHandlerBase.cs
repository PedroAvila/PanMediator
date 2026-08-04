using Microsoft.Extensions.DependencyInjection;
using PAN.Mediator.Notifications;

namespace PAN.Mediator.Core
{
    internal abstract class PanNotificationHandlerBase
    {
        public abstract Task Handle(
            object notification,
            IServiceProvider provider,
            CancellationToken cancellationToken
        );
    }

    internal sealed class PanNotificationHandlerWrapperImpl<TNotification>
        : PanNotificationHandlerBase
        where TNotification : IPanNotification
    {
        private static readonly IPanNotificationPublishStrategy<TNotification> DefaultStrategy =
            new SequentialNotificationPublishStrategy<TNotification>();

        public override async Task Handle(
            object notification,
            IServiceProvider provider,
            CancellationToken cancellationToken
        )
        {
            var typedNotification = (TNotification)notification;

            var handlers =
                (IEnumerable<IPanNotificationHandler<TNotification>>)
                    provider.GetServices(typeof(IPanNotificationHandler<TNotification>));

            var strategy =
                (IPanNotificationPublishStrategy<TNotification>?)
                    provider.GetService(typeof(IPanNotificationPublishStrategy<TNotification>))
                ?? DefaultStrategy;

            await strategy.Publish(typedNotification, handlers, cancellationToken).ConfigureAwait(false);
        }
    }
}
