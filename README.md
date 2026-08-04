# PAN.Mediator

Mediador inmutable, ligero y nativo para CQRS y Arquitectura Hexagonal en .NET.

PAN.Mediator es un reemplazo propio y desde cero de MediatR. Apunta a paridad de superficie de API con MediatR — `Send`/`Publish`, pipeline behaviors, `Unit`, y una extensión de registro DI estilo `AddXMediator` — para que una app que hoy envuelve MediatR pueda migrar cambiando `using`s y la referencia de paquete, no reescribiendo handlers.

Multi-target: `netstandard2.0`, `net6.0`, `net8.0`.

## Instalación

```bash
dotnet add package PAN.Mediator
```

## Requisitos

- .NET SDK compatible con `netstandard2.0`, `net6.0` o `net8.0`.
- `Microsoft.Extensions.DependencyInjection` (o cualquier contenedor compatible con `IServiceProvider`).

## Uso rápido

### 1. Registrar el mediador

```csharp
services.AddPanMediator(config =>
{
    config.RegisterServicesFromAssembly(typeof(Program).Assembly);
    config.AddOpenBehavior(typeof(LoggingBehavior<,>));
});
```

`RegisterServicesFromAssembly`/`RegisterServicesFromAssemblies` escanean el assembly y registran automáticamente cualquier `IPanRequestHandler<,>` e `IPanNotificationHandler<>` concreto (incluso `internal`).

Lifetimes configurables (todos `Transient` por defecto):

```csharp
config.MediatorLifetime = ServiceLifetime.Scoped;
config.HandlerLifetime = ServiceLifetime.Scoped;
config.BehaviorLifetime = ServiceLifetime.Scoped;
```

### 2. Definir un request y su handler

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

Para requests sin respuesta significativa, usar la forma de conveniencia basada en `Unit` (equivalente a `void` async):

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

### 3. Enviar el request

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

Registrar con `config.AddOpenBehavior(typeof(LoggingBehavior<,>))` (genérico abierto, aplica a todos los requests) o `config.AddBehavior<MySpecificBehavior>()` (un tipo concreto).

**El orden de registro importa**: el primer behavior registrado queda más "afuera" en la cadena. Si registrás `ValidationBehavior` y después `LoggingBehavior`, `LoggingBehavior` envuelve a `ValidationBehavior`, que envuelve al handler.

### 5. Notificaciones (pub/sub)

```csharp
public sealed class UserCreatedNotification : IPanNotification
{
    public required string Email { get; init; }
}

public sealed class SendWelcomeEmailHandler : IPanNotificationHandler<UserCreatedNotification>
{
    public Task Handle(UserCreatedNotification notification, CancellationToken cancellationToken)
    {
        // enviar email...
        return Task.CompletedTask;
    }
}
```

```csharp
await mediator.Publish(new UserCreatedNotification { Email = "user@example.com" });
```

Zero handlers registrados es un no-op, no un error. Por defecto los handlers se ejecutan secuencialmente en orden de registro (ver siguiente sección para hacerlo configurable).

## Novedad: estrategias de Publish configurables

A diferencia de MediatR clásico, en PAN.Mediator la forma en que `Publish` invoca a los handlers de una notificación es **pluggable** vía `IPanNotificationPublishStrategy<TNotification>`, sin cambiar el contrato de `IPanMediator`, `IPanNotification` ni `IPanNotificationHandler<T>`.

Vienen dos estrategias listas para usar:

- **`SequentialNotificationPublishStrategy<T>`** (default, sin configurar nada): invoca los handlers uno por uno, en orden de registro. Si uno tira una excepción, los siguientes no se ejecutan — es el comportamiento original.
- **`ParallelWhenAllNotificationPublishStrategy<T>`**: arranca todos los handlers a la vez y espera a que todos terminen con `Task.WhenAll`, aunque alguno falle. Útil cuando los handlers son independientes (ej. notificar a varios sistemas externos) y no querés que uno lento o roto bloquee al resto.

### Cómo activarla

```csharp
services.AddPanMediator(config =>
{
    config.RegisterServicesFromAssembly(typeof(Program).Assembly);
    config.UseNotificationPublishStrategy(typeof(ParallelWhenAllNotificationPublishStrategy<>));
});
```

Se registra como genérico abierto — igual que los pipeline behaviors — y aplica a todos los tipos de notificación del proceso. Si nunca se llama a `UseNotificationPublishStrategy`, el comportamiento es idéntico al de siempre (secuencial), así que no rompe código existente.

### Estrategia propia

También podés implementar tu propia estrategia (por ejemplo, para tolerar fallos, aplicar timeouts por handler, o loggear el fan-out):

```csharp
public sealed class MyCustomPublishStrategy<TNotification> : IPanNotificationPublishStrategy<TNotification>
    where TNotification : IPanNotification
{
    public async Task Publish(
        TNotification notification,
        IEnumerable<IPanNotificationHandler<TNotification>> handlers,
        CancellationToken cancellationToken)
    {
        // tu lógica de fan-out
    }
}
```

```csharp
config.UseNotificationPublishStrategy(typeof(MyCustomPublishStrategy<>));
```

## Fuera de alcance (v1)

Streaming requests (`IStreamRequest`), pre/post processors y exception-handling middleware no están implementados — solo `Send`, `Publish` y pipeline behaviors. Tampoco hay overloads no genéricos `Send(object)`/`Publish(object)`.

## Desarrollo

```bash
# Compilar los tres target frameworks
dotnet build PAN.Mediator.slnx

# Correr toda la suite de tests
dotnet test tests/PAN.Mediator.Tests/PAN.Mediator.Tests.csproj

# Correr un test puntual
dotnet test tests/PAN.Mediator.Tests/PAN.Mediator.Tests.csproj --filter "DisplayName~Send_returns_the_handlers_response"
```

No hay una aplicación consumidora en este repo — se desarrolla y testea de forma independiente.
