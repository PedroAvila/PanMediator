namespace PAN.Mediator.Abstractions
{
    /// <summary>Marker for a request that returns a response of type <typeparamref name="TResponse"/>.</summary>
    public interface IPanRequest<out TResponse> { }

    /// <summary>Marker for a request with no meaningful return value (response is <see cref="Unit"/>).</summary>
    public interface IPanRequest : IPanRequest<Unit> { }
}
