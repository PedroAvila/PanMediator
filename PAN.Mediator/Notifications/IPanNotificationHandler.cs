using System.Threading;
using System.Threading.Tasks;

namespace PAN.Mediator.Notifications
{
    public interface IPanNotificationHandler<in TNotification>
        where TNotification : IPanNotification
    {
        Task Handle(TNotification notification, CancellationToken cancellationToken);
    }
}
