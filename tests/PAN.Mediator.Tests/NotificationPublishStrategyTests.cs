using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using PAN.Mediator.Abstractions;
using PAN.Mediator.Core;
using PAN.Mediator.DependencyInjection;
using PAN.Mediator.Notifications;
using PAN.Mediator.Registration;
using Xunit;

namespace PAN.Mediator.Tests
{
    public class NotificationPublishStrategyTests
    {
        [Fact]
        public async Task Sequential_strategy_never_overlaps_handlers()
        {
            var tracker = new ConcurrencyTracker();
            var delay = TimeSpan.FromMilliseconds(50);

            var services = new ServiceCollection();
            services.AddSingleton<IPanNotificationHandler<ProbeNotification>>(new ConcurrencyProbeNotificationHandler(tracker, delay));
            services.AddSingleton<IPanNotificationHandler<ProbeNotification>>(new ConcurrencyProbeNotificationHandler(tracker, delay));
            services.AddSingleton<IPanNotificationHandler<ProbeNotification>>(new ConcurrencyProbeNotificationHandler(tracker, delay));
            services.AddSingleton<IPanMediator, PanMediator>();

            var provider = services.BuildServiceProvider();
            var mediator = provider.GetRequiredService<IPanMediator>();

            await mediator.Publish(new ProbeNotification());

            Assert.Equal(1, tracker.MaxActive);
        }

        [Fact]
        public async Task ParallelWhenAll_strategy_runs_handlers_concurrently()
        {
            var tracker = new ConcurrencyTracker();
            var delay = TimeSpan.FromMilliseconds(50);

            var services = new ServiceCollection();
            services.AddSingleton<IPanNotificationHandler<ProbeNotification>>(new ConcurrencyProbeNotificationHandler(tracker, delay));
            services.AddSingleton<IPanNotificationHandler<ProbeNotification>>(new ConcurrencyProbeNotificationHandler(tracker, delay));
            services.AddSingleton<IPanNotificationHandler<ProbeNotification>>(new ConcurrencyProbeNotificationHandler(tracker, delay));
            services.AddSingleton(typeof(IPanNotificationPublishStrategy<>), typeof(ParallelWhenAllNotificationPublishStrategy<>));
            services.AddSingleton<IPanMediator, PanMediator>();

            var provider = services.BuildServiceProvider();
            var mediator = provider.GetRequiredService<IPanMediator>();

            await mediator.Publish(new ProbeNotification());

            Assert.Equal(3, tracker.MaxActive);
        }

        [Fact]
        public async Task ParallelWhenAll_strategy_runs_every_handler_even_when_some_throw()
        {
            var firstHandler = new ThrowingNotificationHandler(new InvalidOperationException("first"));
            var middleHandler = Substitute.For<IPanNotificationHandler<ProbeNotification>>();
            var thirdHandler = new ThrowingNotificationHandler(new InvalidOperationException("second"));

            var services = new ServiceCollection();
            services.AddSingleton<IPanNotificationHandler<ProbeNotification>>(firstHandler);
            services.AddSingleton(middleHandler);
            services.AddSingleton<IPanNotificationHandler<ProbeNotification>>(thirdHandler);
            services.AddSingleton(typeof(IPanNotificationPublishStrategy<>), typeof(ParallelWhenAllNotificationPublishStrategy<>));
            services.AddSingleton<IPanMediator, PanMediator>();

            var provider = services.BuildServiceProvider();
            var mediator = provider.GetRequiredService<IPanMediator>();

            var notification = new ProbeNotification();

            // Sequential would stop after the first throwing handler; ParallelWhenAll starts every
            // handler up front, so the handler registered after the first throw still runs.
            await Assert.ThrowsAnyAsync<Exception>(() => mediator.Publish(notification));
            await middleHandler.Received(1).Handle(notification, Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task AddPanMediator_wires_custom_open_generic_notification_publish_strategy_end_to_end()
        {
            var recorder = new CallRecorder();

            var services = new ServiceCollection();
            services.AddSingleton(recorder);
            services.AddPanMediator(configuration =>
            {
                configuration.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
                configuration.UseNotificationPublishStrategy(typeof(RecordingNotificationPublishStrategy<>));
            });

            var provider = services.BuildServiceProvider();
            var mediator = provider.GetRequiredService<IPanMediator>();

            // Uses PingNotification (not ProbeNotification) because this test relies on the assembly scan
            // to register handlers — PingNotificationHandlerOne/Two have parameterless constructors, unlike
            // the ProbeNotification fixtures used elsewhere in this file, which need constructor args the
            // scan can't supply.
            await mediator.Publish(new PingNotification { Message = "hi" });

            Assert.Contains("strategy-invoked", recorder.Calls);
        }

        [Fact]
        public void UseNotificationPublishStrategy_throws_when_type_is_not_an_open_generic_definition()
        {
            var configuration = new PanMediatorServiceConfiguration();

            Assert.Throws<ArgumentException>(() =>
                configuration.UseNotificationPublishStrategy(typeof(SequentialNotificationPublishStrategy<PingNotification>)));
        }
    }
}
