using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using PAN.Mediator.Abstractions;
using PAN.Mediator.Core;
using PAN.Mediator.Notifications;
using Xunit;

namespace PAN.Mediator.Tests
{
    public class NotificationTests
    {
        [Fact]
        public async Task Publish_invokes_every_registered_handler_in_registration_order()
        {
            var handlerOne = Substitute.For<IPanNotificationHandler<PingNotification>>();
            var handlerTwo = Substitute.For<IPanNotificationHandler<PingNotification>>();
            var handlerThree = Substitute.For<IPanNotificationHandler<PingNotification>>();

            var services = new ServiceCollection();
            services.AddSingleton(handlerOne);
            services.AddSingleton(handlerTwo);
            services.AddSingleton(handlerThree);
            services.AddSingleton<IPanMediator, PanMediator>();

            var provider = services.BuildServiceProvider();
            var mediator = provider.GetRequiredService<IPanMediator>();

            var notification = new PingNotification { Message = "hi" };
            await mediator.Publish(notification);

            await handlerOne.Received(1).Handle(notification, Arg.Any<CancellationToken>());
            await handlerTwo.Received(1).Handle(notification, Arg.Any<CancellationToken>());
            await handlerThree.Received(1).Handle(notification, Arg.Any<CancellationToken>());

            Received.InOrder(async () =>
            {
                await handlerOne.Handle(notification, Arg.Any<CancellationToken>());
                await handlerTwo.Handle(notification, Arg.Any<CancellationToken>());
                await handlerThree.Handle(notification, Arg.Any<CancellationToken>());
            });
        }

        [Fact]
        public async Task Publish_with_no_registered_handlers_does_not_throw()
        {
            var services = new ServiceCollection();
            services.AddSingleton<IPanMediator, PanMediator>();

            var provider = services.BuildServiceProvider();
            var mediator = provider.GetRequiredService<IPanMediator>();

            await mediator.Publish(new PingNotification { Message = "hi" });
        }

        [Fact]
        public async Task Publish_uses_sequential_strategy_by_default_and_stops_on_first_exception()
        {
            var expectedException = new InvalidOperationException("boom");
            var throwingHandler = new ThrowingNotificationHandler(expectedException);
            var secondHandler = Substitute.For<IPanNotificationHandler<ProbeNotification>>();

            var services = new ServiceCollection();
            services.AddSingleton<IPanNotificationHandler<ProbeNotification>>(throwingHandler);
            services.AddSingleton(secondHandler);
            services.AddSingleton<IPanMediator, PanMediator>();

            var provider = services.BuildServiceProvider();
            var mediator = provider.GetRequiredService<IPanMediator>();

            var notification = new ProbeNotification();
            var actualException = await Assert.ThrowsAsync<InvalidOperationException>(() => mediator.Publish(notification));

            Assert.Same(expectedException, actualException);
            await secondHandler.DidNotReceive().Handle(Arg.Any<ProbeNotification>(), Arg.Any<CancellationToken>());
        }
    }
}
