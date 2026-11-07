using BenchmarkDotNet.Attributes;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using PAN.Mediator.Benchmarks.Fixtures;

namespace PAN.Mediator.Benchmarks;

[MemoryDiagnoser]
public class MediatRSendBenchmarks
{
    private ServiceProvider _transientProvider = null!;
    private ServiceProvider _singletonProvider = null!;
    private IMediator _transientMediator = null!;
    private IMediator _singletonMediator = null!;
    private IRequestHandler<MediatRPing, string> _handler = null!;
    private readonly MediatRPing _request = MediatRPing.Instance;

    [GlobalSetup]
    public void Setup()
    {
        var transientServices = new ServiceCollection();
        transientServices.AddMediatR(c => c.RegisterServicesFromAssembly(typeof(MediatRPing).Assembly));
        _transientProvider = transientServices.BuildServiceProvider();
        _transientMediator = _transientProvider.GetRequiredService<IMediator>();
        _handler = _transientProvider.GetRequiredService<IRequestHandler<MediatRPing, string>>();

        var singletonServices = new ServiceCollection();
        singletonServices.AddMediatR(c =>
        {
            c.RegisterServicesFromAssembly(typeof(MediatRPing).Assembly);
            c.Lifetime = ServiceLifetime.Singleton;
        });
        _singletonProvider = singletonServices.BuildServiceProvider();
        _singletonMediator = _singletonProvider.GetRequiredService<IMediator>();
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _transientProvider.Dispose();
        _singletonProvider.Dispose();
    }

    [Benchmark(Baseline = true)]
    public Task<string> Direct_Handler() => _handler.Handle(_request, default);

    [Benchmark]
    public Task<string> Send_Transient() => _transientMediator.Send(_request, default);

    [Benchmark]
    public Task<string> Send_Singleton() => _singletonMediator.Send(_request, default);
}

[MemoryDiagnoser]
public class MediatRSendWithBehaviorBenchmarks
{
    private ServiceProvider _transientProvider = null!;
    private ServiceProvider _singletonProvider = null!;
    private IMediator _transientMediator = null!;
    private IMediator _singletonMediator = null!;
    private IRequestHandler<MediatRPing, string> _handler = null!;
    private readonly MediatRPing _request = MediatRPing.Instance;

    [GlobalSetup]
    public void Setup()
    {
        var transientServices = new ServiceCollection();
        transientServices.AddMediatR(c =>
        {
            c.RegisterServicesFromAssembly(typeof(MediatRPing).Assembly);
            c.AddOpenBehavior(typeof(MediatRPassThroughBehavior<,>));
        });
        _transientProvider = transientServices.BuildServiceProvider();
        _transientMediator = _transientProvider.GetRequiredService<IMediator>();
        _handler = _transientProvider.GetRequiredService<IRequestHandler<MediatRPing, string>>();

        var singletonServices = new ServiceCollection();
        singletonServices.AddMediatR(c =>
        {
            c.RegisterServicesFromAssembly(typeof(MediatRPing).Assembly);
            c.AddOpenBehavior(typeof(MediatRPassThroughBehavior<,>));
            c.Lifetime = ServiceLifetime.Singleton;
        });
        _singletonProvider = singletonServices.BuildServiceProvider();
        _singletonMediator = _singletonProvider.GetRequiredService<IMediator>();
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _transientProvider.Dispose();
        _singletonProvider.Dispose();
    }

    [Benchmark(Baseline = true)]
    public Task<string> Direct_Handler() => _handler.Handle(_request, default);

    [Benchmark]
    public Task<string> Send_Transient() => _transientMediator.Send(_request, default);

    [Benchmark]
    public Task<string> Send_Singleton() => _singletonMediator.Send(_request, default);
}

[MemoryDiagnoser]
public class MediatRPublishBenchmarks
{
    private ServiceProvider _provider = null!;
    private IMediator _mediator = null!;
    private INotificationHandler<MediatRSingleNotification> _singleHandler = null!;

    [GlobalSetup]
    public void Setup()
    {
        var services = new ServiceCollection();
        services.AddMediatR(c =>
        {
            c.RegisterServicesFromAssembly(typeof(MediatRSingleNotification).Assembly);
            c.Lifetime = ServiceLifetime.Singleton;
        });
        _provider = services.BuildServiceProvider();
        _mediator = _provider.GetRequiredService<IMediator>();
        _singleHandler = _provider.GetRequiredService<INotificationHandler<MediatRSingleNotification>>();
    }

    [GlobalCleanup]
    public void Cleanup() => _provider.Dispose();

    [Benchmark(Baseline = true)]
    public Task Direct_Single() => _singleHandler.Handle(MediatRSingleNotification.Instance, default);

    [Benchmark]
    public Task Publish_Single() => _mediator.Publish(MediatRSingleNotification.Instance, default);

    [Benchmark]
    public Task Publish_Three() => _mediator.Publish(MediatRThreeNotifications.Instance, default);
}
