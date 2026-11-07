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

        public override Task Handle(
            object notification,
            IServiceProvider provider,
            CancellationToken cancellationToken
        )
        {
            try
            {
                var typedNotification = (TNotification)notification;

                var handlers = provider.GetServices<IPanNotificationHandler<TNotification>>();

                var strategy =
                    provider.GetService<IPanNotificationPublishStrategy<TNotification>>()
                    ?? DefaultStrategy;

                return strategy.Publish(typedNotification, handlers, cancellationToken);
            }
            catch (Exception ex)
            {
                // Preserve the previous async wrapper's behavior: synchronous failures surface as a
                // faulted Task instead of throwing from Publish.
                return Task.FromException(ex);
            }
        }
    }
}
