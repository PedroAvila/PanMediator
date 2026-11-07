using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using PAN.Mediator.Abstractions;
using PAN.Mediator.DependencyInjection;
using Xunit;

namespace PAN.Mediator.Tests
{
    public class DependencyInjectionTests
    {
        [Fact]
        public async Task AddPanMediator_scans_the_assembly_and_resolves_handlers_without_manual_registration()
        {
            var services = new ServiceCollection();

            services.AddPanMediator(config =>
            {
                config.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
            });

            var provider = services.BuildServiceProvider();
            var mediator = provider.GetRequiredService<IPanMediator>();

            var response = await mediator.Send(new PingRequest { Message = "hi" });

            Assert.Equal("Pong: hi", response);
        }

        [Fact]
        public async Task AddPanMediator_prefers_the_source_generated_mediator_when_available()
        {
            var services = new ServiceCollection();

            services.AddPanMediator(config =>
            {
                config.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
            });

            var provider = services.BuildServiceProvider();
            var mediator = provider.GetRequiredService<IPanMediator>();

            Assert.Equal("PAN.Mediator.Generated.GeneratedPanMediator", mediator.GetType().FullName);

            Assert.Equal("Pong: generated", await mediator.Send(new PingRequest { Message = "generated" }));
            Assert.Equal(Unit.Value, await mediator.Send(new VoidRequest()));

            await mediator.Publish(new PingNotification { Message = "generated" });
        }

        [Fact]
        public async Task AddPanMediator_registers_open_behaviors_and_notification_handlers_from_scanning()
        {
            var services = new ServiceCollection();
            var recorder = new CallRecorder();
            services.AddSingleton(recorder);

            services.AddPanMediator(config =>
            {
                config.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
                config.AddOpenBehavior(typeof(RecordingBehaviorA<,>));
                config.AddOpenBehavior(typeof(RecordingBehaviorB<,>));
            });

            var provider = services.BuildServiceProvider();
            var mediator = provider.GetRequiredService<IPanMediator>();

            await mediator.Send(new PingRequest { Message = "hi" });
            Assert.Equal(new[] { "A-before", "B-before", "B-after", "A-after" }, recorder.Calls);

            // Notification handlers found by the same assembly scan.
            await mediator.Publish(new PingNotification { Message = "hi" });
        }
    }
}
