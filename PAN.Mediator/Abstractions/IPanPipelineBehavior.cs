using System.Threading;
using System.Threading.Tasks;

namespace PAN.Mediator.Abstractions
{
    /// <summary>Continuation delegate representing the rest of the pipeline (next behavior, or the terminal handler).</summary>
    public delegate Task<TResponse> PanRequestHandlerDelegate<TResponse>();

    public interface IPanPipelineBehavior<in TRequest, TResponse>
        where TRequest : IPanRequest<TResponse>
    {
        Task<TResponse> Handle(TRequest request, PanRequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken);
    }
}
