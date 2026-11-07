using System.Threading;
using System.Threading.Tasks;

namespace PAN.Mediator.Notifications
{
    public interface IPanNotificationHandler<in TNotification>
        where TNotification : IPanNotification
    {
        ValueTask Handle(TNotification notification, CancellationToken cancellationToken);
    }
}
