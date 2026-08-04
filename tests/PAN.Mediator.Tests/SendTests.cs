using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using PAN.Mediator.Abstractions;
using PAN.Mediator.Core;
using Xunit;

namespace PAN.Mediator.Tests
{
    public class SendTests
    {
        [Fact]
        public async Task Send_returns_the_handlers_response()
        {
            var services = new ServiceCollection();
            services.AddSingleton<IPanRequestHandler<PingRequest, string>, PingRequestHandler>();
            services.AddSingleton<IPanMediator, PanMediator>();

            var provider = services.BuildServiceProvider();
            var mediator = provider.GetRequiredService<IPanMediator>();

            var response = await mediator.Send(new PingRequest { Message = "hi" });

            Assert.Equal("Pong: hi", response);
        }

        [Fact]
        public async Task Send_for_a_Unit_request_completes_successfully()
        {
            var services = new ServiceCollection();
            services.AddSingleton<IPanRequestHandler<VoidRequest, Unit>, VoidRequestHandler>();
            services.AddSingleton<IPanMediator, PanMediator>();

            var provider = services.BuildServiceProvider();
            var mediator = provider.GetRequiredService<IPanMediator>();

            var response = await mediator.Send(new VoidRequest());

            Assert.Equal(Unit.Value, response);
        }

        [Fact]
        public async Task Send_without_a_registered_handler_throws()
        {
            var services = new ServiceCollection();
            services.AddSingleton<IPanMediator, PanMediator>();

            var provider = services.BuildServiceProvider();
            var mediator = provider.GetRequiredService<IPanMediator>();

            await Assert.ThrowsAsync<InvalidOperationException>(() => mediator.Send(new PingRequest { Message = "hi" }));
        }

        [Fact]
        public async Task Behavior_that_does_not_call_next_short_circuits_the_handler()
        {
            var handler = Substitute.For<IPanRequestHandler<PingRequest, string>>();
            handler.Handle(Arg.Any<PingRequest>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult("real"));

            var services = new ServiceCollection();
            services.AddSingleton(handler);
            services.AddSingleton<IPanPipelineBehavior<PingRequest, string>, ShortCircuitBehavior>();
            services.AddSingleton<IPanMediator, PanMediator>();

            var provider = services.BuildServiceProvider();
            var mediator = provider.GetRequiredService<IPanMediator>();

            var response = await mediator.Send(new PingRequest { Message = "hi" });

            Assert.Equal("short-circuited", response);
            await handler.DidNotReceive().Handle(Arg.Any<PingRequest>(), Arg.Any<CancellationToken>());
        }
    }
}
