using System.Threading;
using System.Threading.Tasks;
using PAN.Mediator.Notifications;

namespace PAN.Mediator.Abstractions
{
    public interface IPanMediator
    {
        /// <summary>Dispatches a request to its single registered handler through the pipeline behavior chain.</summary>
        ValueTask<TResponse> Send<TResponse>(IPanRequest<TResponse> request, CancellationToken cancellationToken = default);

        /// <summary>Publishes a notification to every registered handler using the configured
        /// <see cref="IPanNotificationPublishStrategy{TNotification}"/>
        /// (sequential foreach-await by default, matching PAN.Mediator's original behavior).</summary>
        ValueTask Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
            where TNotification : IPanNotification;
    }
}
