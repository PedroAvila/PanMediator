using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using PAN.Mediator.Abstractions;
using PAN.Mediator.Core;
using Xunit;

namespace PAN.Mediator.Tests
{
    public class PipelineBehaviorTests
    {
        [Fact]
        public async Task Behaviors_run_in_registration_order_with_the_first_one_outermost()
        {
            var services = new ServiceCollection();
            var recorder = new CallRecorder();

            services.AddSingleton(recorder);
            services.AddSingleton<IPanRequestHandler<PingRequest, string>, PingRequestHandler>();
            services.AddSingleton(typeof(IPanPipelineBehavior<,>), typeof(RecordingBehaviorA<,>));
            services.AddSingleton(typeof(IPanPipelineBehavior<,>), typeof(RecordingBehaviorB<,>));
            services.AddSingleton<IPanMediator, PanMediator>();

            var provider = services.BuildServiceProvider();
            var mediator = provider.GetRequiredService<IPanMediator>();

            await mediator.Send(new PingRequest { Message = "hi" });

            Assert.Equal(new[] { "A-before", "B-before", "B-after", "A-after" }, recorder.Calls);
        }
    }
}
