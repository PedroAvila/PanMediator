# PAN.Mediator

Immutable, lightweight, and native mediator for CQRS and Hexagonal Architecture in .NET.

PAN.Mediator is a from-scratch, self-owned replacement for MediatR. It targets API-surface parity with MediatR — `Send`/`Publish`, pipeline behaviors, `Unit`, and an `AddXMediator`-style DI registration extension — so that an app wrapping MediatR today can migrate by changing `using`s and the package reference, not by rewriting handlers.

Multi-target: `netstandard2.0`, `net6.0`, `net8.0`.

Library usage sample: [PanMediatorPrueba](https://github.com/PedroAvila/PanMediatorPrueba).

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
    public Task<string> Handle(PingRequest request, CancellationToken cancellationToken)
        => Task.FromResult($"Pong: {request.Message}");
}
```

For requests with no meaningful response, use the `Unit`-based convenience form (equivalent to async `void`):

```csharp
public sealed class DeleteUserCommand : IPanRequest { }

public sealed class DeleteUserHandler : IPanRequestHandler<DeleteUserCommand>
{
    public Task<Unit> Handle(DeleteUserCommand request, CancellationToken cancellationToken)
    {
        // ...
        return Unit.Task;
    }
}
```

### 3. Send the request

```csharp
public sealed class MyService(IPanMediator mediator)
{
    public Task<string> Ping(string message) =>
        mediator.Send(new PingRequest { Message = message });
}
```

### 4. Pipeline behaviors

```csharp
public sealed class LoggingBehavior<TRequest, TResponse> : IPanPipelineBehavior<TRequest, TResponse>
    where TRequest : IPanRequest<TResponse>
{
    public async Task<TResponse> Handle(TRequest request, PanRequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
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
    public Task Handle(UserCreatedNotification notification, CancellationToken cancellationToken)
    {
        // send email...
        return Task.CompletedTask;
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
    public async Task Publish(
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
