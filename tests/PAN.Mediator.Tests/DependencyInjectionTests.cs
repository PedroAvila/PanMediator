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
