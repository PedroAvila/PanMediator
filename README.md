# PAN.Mediator

Immutable, lightweight, and native mediator for CQRS and Hexagonal Architecture in .NET.

PAN.Mediator is a from-scratch, self-owned replacement for MediatR. It targets API-surface parity with MediatR — `Send`/`Publish`, pipeline behaviors, `Unit`, and an `AddXMediator`-style DI registration extension — so that an app wrapping MediatR today can migrate by changing `using`s and the package reference, not by rewriting handlers.

Multi-target: `netstandard2.0`, `net6.0`, `net8.0`. The NuGet package also ships a Roslyn source generator that emits a compile-time dispatch chain for your concrete requests (falling back to reflection only for unknown request types).

Library usage sample: [PanMediatorPrueba](https://github.com/PedroAvila/PanMediatorPrueba).

## Performance

Benchmarked against MediatR 12.5.0 (Apache 2.0): PAN.Mediator is up to 1.8x faster on `Send` and 18x more memory-efficient on `Publish`, while remaining a drop-in replacement.

BenchmarkDotNet, .NET 8.0.31, `DefaultJob`. Lower is better.

| Benchmark | PAN.Mediator | MediatR 12.5.0 |
|---|---:|---:|
| `Send` (Transient handler) | 72.68 ns / 136 B | 111.11 ns / 312 B |
| `Send` (Singleton handler) | 70.55 ns / 112 B | 101.56 ns / 312 B |
| `Send` + 1 pipeline behavior | 86.48 ns / 256 B | 152.13 ns / 504 B |
| `Publish` (1 handler) | 58.79 ns / 0 B | 95.38 ns / 288 B |
| `Publish` (3 handlers) | 100.74 ns / 32 B | 154.97 ns / 592 B |

Optimized (source generator + `ValueTask`):

| Benchmark | PAN.Mediator | MediatR 12.5.0 |
|---|---:|---:|
| `Send` (Transient handler) | 53.11 ns / 64 B | 101.91 ns / 312 B |
| `Send` (Singleton handler) | 46.77 ns / 40 B | 102.42 ns / 312 B |
| `Send` + 1 pipeline behavior | 71.97 ns / 184 B | 151.99 ns / 504 B |
| `Publish` (1 handler) | 66.27 ns / 0 B | 90.80 ns / 288 B |
| `Publish` (3 handlers) | 103.04 ns / 32 B | 152.72 ns / 592 B |

Compatibility note: compared with MediatR, handler/behavior methods return `ValueTask`/`ValueTask<T>` (and use `Unit.CompletedValueTask`) instead of `Task`/`Task<T>`. The rest of the surface (`Send`, `Publish`, pipeline behaviors, `Unit`, `AddPanMediator`) keeps the same shape, so migration is normally a per-handler signature change.

Reproduce them with:

```bash
dotnet run -c Release --project benchmarks/PAN.Mediator.Benchmarks/PAN.Mediator.Benchmarks.csproj
```

## Installation

```bash
dotnet add package PAN.Mediator
```

## Requirements

- .NET SDK compatible with `netstandard2.0`, `net6.0`, or `net8.0`.
- `Microsoft.Extensions.DependencyInjection` (or any container compatible with `IServiceProvider`).

## Quick start

### 1. Register the mediator

```csharp
services.AddPanMediator(config =>
{
    config.RegisterServicesFromAssembly(typeof(Program).Assembly);
    config.AddOpenBehavior(typeof(LoggingBehavior<,>));
});
```

`RegisterServicesFromAssembly`/`RegisterServicesFromAssemblies` scan the assembly and automatically register any concrete `IPanRequestHandler<,>` and `IPanNotificationHandler<>` (including `internal` ones).

Configurable lifetimes (all `Transient` by default):

```csharp
config.MediatorLifetime = ServiceLifetime.Scoped;
config.HandlerLifetime = ServiceLifetime.Scoped;
config.BehaviorLifetime = ServiceLifetime.Scoped;
```

### 2. Define a request and its handler

```csharp
public sealed class PingRequest : IPanRequest<string>
{
    public string Message { get; init; } = string.Empty;
}

public sealed class PingRequestHandler : IPanRequestHandler<PingRequest, string>
{
    public ValueTask<string> Handle(PingRequest request, CancellationToken cancellationToken)
        => new ValueTask<string>($"Pong: {request.Message}");
}
```

For requests with no meaningful response, use the `Unit`-based convenience form (equivalent to async `void`):

```csharp
public sealed class DeleteUserCommand : IPanRequest { }

public sealed class DeleteUserHandler : IPanRequestHandler<DeleteUserCommand>
{
    public ValueTask<Unit> Handle(DeleteUserCommand request, CancellationToken cancellationToken)
    {
        // ...
        return Unit.CompletedValueTask;
    }
}
```

### 3. Send the request

```csharp
public sealed class MyService(IPanMediator mediator)
{
    public ValueTask<string> Ping(string message) =>
        mediator.Send(new PingRequest { Message = message });
}
```

### 4. Pipeline behaviors

```csharp
public sealed class LoggingBehavior<TRequest, TResponse> : IPanPipelineBehavior<TRequest, TResponse>
    where TRequest : IPanRequest<TResponse>
{
    public async ValueTask<TResponse> Handle(TRequest request, PanRequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        Console.WriteLine($"Handling {typeof(TRequest).Name}");
        var response = await next();
        Console.WriteLine($"Handled {typeof(TRequest).Name}");
        return response;
    }
}
```

Register with `config.AddOpenBehavior(typeof(LoggingBehavior<,>))` (open generic, applies to all requests) or `config.AddBehavior<MySpecificBehavior>()` (a concrete type).

**Registration order matters**: the first registered behavior ends up outermost in the chain. If you register `ValidationBehavior` and then `LoggingBehavior`, `LoggingBehavior` wraps `ValidationBehavior`, which wraps the handler.

### 5. Notifications (pub/sub)

```csharp
public sealed class UserCreatedNotification : IPanNotification
{
    public required string Email { get; init; }
}

public sealed class SendWelcomeEmailHandler : IPanNotificationHandler<UserCreatedNotification>
{
    public ValueTask Handle(UserCreatedNotification notification, CancellationToken cancellationToken)
    {
        // send email...
        return default;
    }
}
```

```csharp
await mediator.Publish(new UserCreatedNotification { Email = "user@example.com" });
```

Zero registered handlers is a no-op, not an error. By default handlers run sequentially in registration order (see the next section to make it configurable).

## New: configurable Publish strategies

Unlike classic MediatR, in PAN.Mediator the way `Publish` invokes a notification's handlers is **pluggable** via `IPanNotificationPublishStrategy<TNotification>`, without changing the `IPanMediator`, `IPanNotification`, or `IPanNotificationHandler<T>` contracts.

Two ready-to-use strategies ship with the library:

- **`SequentialNotificationPublishStrategy<T>`** (default, no configuration needed): invokes handlers one by one, in registration order. If one throws, the following ones do not run — this is the original behavior.
- **`ParallelWhenAllNotificationPublishStrategy<T>`**: starts all handlers at once and waits for all of them to finish with `Task.WhenAll`, even if one fails. Useful when handlers are independent (e.g. notifying several external systems) and you don't want one slow or broken handler to block the rest.

### How to enable it

```csharp
services.AddPanMediator(config =>
{
    config.RegisterServicesFromAssembly(typeof(Program).Assembly);
    config.UseNotificationPublishStrategy(typeof(ParallelWhenAllNotificationPublishStrategy<>));
});
```

It is registered as an open generic — just like pipeline behaviors — and applies to all notification types in the process. If `UseNotificationPublishStrategy` is never called, the behavior is identical to the usual one (sequential), so it does not break existing code.

### Custom strategy

You can also implement your own strategy (for example, to tolerate failures, apply per-handler timeouts, or log the fan-out):

```csharp
public sealed class MyCustomPublishStrategy<TNotification> : IPanNotificationPublishStrategy<TNotification>
    where TNotification : IPanNotification
{
    public async ValueTask Publish(
        TNotification notification,
        IEnumerable<IPanNotificationHandler<TNotification>> handlers,
        CancellationToken cancellationToken)
    {
        // your fan-out logic
    }
}
```

```csharp
config.UseNotificationPublishStrategy(typeof(MyCustomPublishStrategy<>));
```

## Out of scope (v1)

Streaming requests (`IStreamRequest`), pre/post processors, and exception-handling middleware are not implemented — only `Send`, `Publish`, and pipeline behaviors. There are also no non-generic `Send(object)`/`Publish(object)` overloads.

## Development

```bash
# Build all three target frameworks
dotnet build PAN.Mediator.slnx

# Run the full test suite
dotnet test tests/PAN.Mediator.Tests/PAN.Mediator.Tests.csproj

# Run a single test
dotnet test tests/PAN.Mediator.Tests/PAN.Mediator.Tests.csproj --filter "DisplayName~Send_returns_the_handlers_response"
```

There is no consuming application in this repo — it is developed and tested independently. The library usage sample is at [PanMediatorPrueba](https://github.com/PedroAvila/PanMediatorPrueba).
