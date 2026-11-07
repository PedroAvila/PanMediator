using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.DependencyInjection;
using PAN.Mediator.Abstractions;
using PAN.Mediator.Benchmarks.Fixtures;
using PAN.Mediator.DependencyInjection;
using PAN.Mediator.Notifications;

namespace PAN.Mediator.Benchmarks;

[MemoryDiagnoser]
public class PanSendBenchmarks
{
    private ServiceProvider _transientProvider = null!;
    private ServiceProvider _singletonProvider = null!;
    private IPanMediator _transientMediator = null!;
    private IPanMediator _singletonMediator = null!;
    private IPanRequestHandler<PanPing, string> _handler = null!;
    private readonly PanPing _request = PanPing.Instance;

    [GlobalSetup]
    public void Setup()
    {
        var transientServices = new ServiceCollection();
        transientServices.AddPanMediator(c => c.RegisterServicesFromAssembly(typeof(PanPing).Assembly));
        _transientProvider = transientServices.BuildServiceProvider();
        _transientMediator = _transientProvider.GetRequiredService<IPanMediator>();
        _handler = _transientProvider.GetRequiredService<IPanRequestHandler<PanPing, string>>();

        var singletonServices = new ServiceCollection();
        singletonServices.AddPanMediator(c =>
        {
            c.RegisterServicesFromAssembly(typeof(PanPing).Assembly);
            c.MediatorLifetime = ServiceLifetime.Singleton;
            c.HandlerLifetime = ServiceLifetime.Singleton;
            c.BehaviorLifetime = ServiceLifetime.Singleton;
        });
        _singletonProvider = singletonServices.BuildServiceProvider();
        _singletonMediator = _singletonProvider.GetRequiredService<IPanMediator>();
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _transientProvider.Dispose();
        _singletonProvider.Dispose();
    }

    [Benchmark(Baseline = true)]
    public ValueTask<string> Direct_Handler() => _handler.Handle(_request, default);

    [Benchmark]
    public ValueTask<string> Send_Transient() => _transientMediator.Send(_request, default);

    [Benchmark]
    public ValueTask<string> Send_Singleton() => _singletonMediator.Send(_request, default);
}

[MemoryDiagnoser]
public class PanSendWithBehaviorBenchmarks
{
    private ServiceProvider _transientProvider = null!;
    private ServiceProvider _singletonProvider = null!;
    private IPanMediator _transientMediator = null!;
    private IPanMediator _singletonMediator = null!;
    private IPanRequestHandler<PanPing, string> _handler = null!;
    private readonly PanPing _request = PanPing.Instance;

    [GlobalSetup]
    public void Setup()
    {
        var transientServices = new ServiceCollection();
        transientServices.AddPanMediator(c =>
        {
            c.RegisterServicesFromAssembly(typeof(PanPing).Assembly);
            c.AddOpenBehavior(typeof(PanPassThroughBehavior<,>));
        });
        _transientProvider = transientServices.BuildServiceProvider();
        _transientMediator = _transientProvider.GetRequiredService<IPanMediator>();
        _handler = _transientProvider.GetRequiredService<IPanRequestHandler<PanPing, string>>();

        var singletonServices = new ServiceCollection();
        singletonServices.AddPanMediator(c =>
        {
            c.RegisterServicesFromAssembly(typeof(PanPing).Assembly);
            c.AddOpenBehavior(typeof(PanPassThroughBehavior<,>));
            c.MediatorLifetime = ServiceLifetime.Singleton;
            c.HandlerLifetime = ServiceLifetime.Singleton;
            c.BehaviorLifetime = ServiceLifetime.Singleton;
        });
        _singletonProvider = singletonServices.BuildServiceProvider();
        _singletonMediator = _singletonProvider.GetRequiredService<IPanMediator>();
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _transientProvider.Dispose();
        _singletonProvider.Dispose();
    }

    [Benchmark(Baseline = true)]
    public ValueTask<string> Direct_Handler() => _handler.Handle(_request, default);

    [Benchmark]
    public ValueTask<string> Send_Transient() => _transientMediator.Send(_request, default);

    [Benchmark]
    public ValueTask<string> Send_Singleton() => _singletonMediator.Send(_request, default);
}

[MemoryDiagnoser]
public class PanPublishBenchmarks
{
    private ServiceProvider _provider = null!;
    private IPanMediator _mediator = null!;
    private IPanNotificationHandler<PanSingleNotification> _singleHandler = null!;

    [GlobalSetup]
    public void Setup()
    {
        var services = new ServiceCollection();
        services.AddPanMediator(c =>
        {
            c.RegisterServicesFromAssembly(typeof(PanSingleNotification).Assembly);
            c.MediatorLifetime = ServiceLifetime.Singleton;
            c.HandlerLifetime = ServiceLifetime.Singleton;
        });
        _provider = services.BuildServiceProvider();
        _mediator = _provider.GetRequiredService<IPanMediator>();
        _singleHandler = _provider.GetRequiredService<IPanNotificationHandler<PanSingleNotification>>();
    }

    [GlobalCleanup]
    public void Cleanup() => _provider.Dispose();

    [Benchmark(Baseline = true)]
    public ValueTask Direct_Single() => _singleHandler.Handle(PanSingleNotification.Instance, default);

    [Benchmark]
    public ValueTask Publish_Single() => _mediator.Publish(PanSingleNotification.Instance, default);

    [Benchmark]
    public ValueTask Publish_Three() => _mediator.Publish(PanThreeNotifications.Instance, default);
}
