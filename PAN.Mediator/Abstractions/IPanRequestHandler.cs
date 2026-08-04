using System.Threading;
using System.Threading.Tasks;

namespace PAN.Mediator.Abstractions
{
    public interface IPanRequestHandler<in TRequest, TResponse>
        where TRequest : IPanRequest<TResponse>
    {
        Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken);
    }

    /// <summary>Convenience form for requests with no meaningful response (Unit).</summary>
    public interface IPanRequestHandler<in TRequest> : IPanRequestHandler<TRequest, Unit>
        where TRequest : IPanRequest<Unit>
    {
    }
}
