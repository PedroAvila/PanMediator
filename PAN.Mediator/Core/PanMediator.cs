using System.Collections.Concurrent;
using System.Threading;
using PAN.Mediator.Abstractions;
using PAN.Mediator.Notifications;

namespace PAN.Mediator.Core
{
    public sealed class PanMediator : IPanMediator
    {
        private static readonly ConcurrentDictionary<Type, PanRequestHandlerBase> RequestHandlers = new();
        private static readonly ConcurrentDictionary<Type, PanNotificationHandlerBase> NotificationHandlers = new();

        private readonly IServiceProvider _provider;

        public PanMediator(IServiceProvider provider)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        }

        public Task<TResponse> Send<TResponse>(IPanRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            if (request is null) throw new ArgumentNullException(nameof(request));

            var requestType = request.GetType();
            var wrapper = (PanRequestHandlerWrapper<TResponse>)RequestHandlers.GetOrAdd(requestType, type =>
            {
                var wrapperType = typeof(PanRequestHandlerWrapperImpl<,>).MakeGenericType(type, typeof(TResponse));
                return (PanRequestHandlerBase)Activator.CreateInstance(wrapperType)!;
            });

            return wrapper.Handle(request, _provider, cancellationToken);
        }

        public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
            where TNotification : IPanNotification
        {
            if (notification is null) throw new ArgumentNullException(nameof(notification));

            var notificationType = notification.GetType();
            var wrapper = NotificationHandlers.GetOrAdd(notificationType, static type =>
            {
                var wrapperType = typeof(PanNotificationHandlerWrapperImpl<>).MakeGenericType(type);
                return (PanNotificationHandlerBase)Activator.CreateInstance(wrapperType)!;
            });

            return wrapper.Handle(notification, _provider, cancellationToken);
        }
    }
}
