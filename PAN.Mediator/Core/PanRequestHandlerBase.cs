using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using PAN.Mediator.Abstractions;

namespace PAN.Mediator.Core
{
    internal abstract class PanRequestHandlerBase
    {
        public abstract Task<object?> Handle(object request, IServiceProvider provider, CancellationToken cancellationToken);
    }

    internal abstract class PanRequestHandlerWrapper<TResponse> : PanRequestHandlerBase
    {
        public abstract Task<TResponse> Handle(IPanRequest<TResponse> request, IServiceProvider provider, CancellationToken cancellationToken);
    }

    internal sealed class PanRequestHandlerWrapperImpl<TRequest, TResponse> : PanRequestHandlerWrapper<TResponse>
        where TRequest : IPanRequest<TResponse>
    {
        public override async Task<object?> Handle(object request, IServiceProvider provider, CancellationToken cancellationToken)
            => await Handle((IPanRequest<TResponse>)request, provider, cancellationToken).ConfigureAwait(false);

        public override Task<TResponse> Handle(IPanRequest<TResponse> request, IServiceProvider provider, CancellationToken cancellationToken)
        {
            var typedRequest = (TRequest)request;

            PanRequestHandlerDelegate<TResponse> handlerDelegate = () =>
            {
                var handler = (IPanRequestHandler<TRequest, TResponse>)provider.GetRequiredService(typeof(IPanRequestHandler<TRequest, TResponse>));
                return handler.Handle(typedRequest, cancellationToken);
            };

            var behaviors = (IEnumerable<IPanPipelineBehavior<TRequest, TResponse>>)
                provider.GetServices(typeof(IPanPipelineBehavior<TRequest, TResponse>));

            // First-registered behavior ends up outermost in the chain.
            foreach (var behavior in behaviors.Reverse())
            {
                var next = handlerDelegate;
                var currentBehavior = behavior;
                handlerDelegate = () => currentBehavior.Handle(typedRequest, next, cancellationToken);
            }

            return handlerDelegate();
        }
    }
}
