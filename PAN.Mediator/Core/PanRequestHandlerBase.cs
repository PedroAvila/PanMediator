using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using PAN.Mediator.Abstractions;

namespace PAN.Mediator.Core
{
    internal abstract class PanRequestHandlerBase
    {
        public abstract ValueTask<object?> Handle(object request, IServiceProvider provider, CancellationToken cancellationToken);
    }

    internal abstract class PanRequestHandlerWrapper<TResponse> : PanRequestHandlerBase
    {
        public abstract ValueTask<TResponse> Handle(IPanRequest<TResponse> request, IServiceProvider provider, CancellationToken cancellationToken);
    }

    internal sealed class PanRequestHandlerWrapperImpl<TRequest, TResponse> : PanRequestHandlerWrapper<TResponse>
        where TRequest : IPanRequest<TResponse>
    {
        public override async ValueTask<object?> Handle(object request, IServiceProvider provider, CancellationToken cancellationToken)
            => await Handle((IPanRequest<TResponse>)request, provider, cancellationToken).ConfigureAwait(false);

        public override ValueTask<TResponse> Handle(IPanRequest<TResponse> request, IServiceProvider provider, CancellationToken cancellationToken)
        {
            var typedRequest = (TRequest)request;

            var behaviors = provider.GetServices<IPanPipelineBehavior<TRequest, TResponse>>();

            IReadOnlyList<IPanPipelineBehavior<TRequest, TResponse>> behaviorList;
            if (behaviors is IReadOnlyList<IPanPipelineBehavior<TRequest, TResponse>> list)
            {
                behaviorList = list;
            }
            else
            {
                behaviorList = new List<IPanPipelineBehavior<TRequest, TResponse>>(behaviors);
            }

            var count = behaviorList.Count;

            // No behaviors: invoke the handler directly (no delegate or closure allocated).
            if (count == 0)
            {
                return provider
                    .GetRequiredService<IPanRequestHandler<TRequest, TResponse>>()
                    .Handle(typedRequest, cancellationToken);
            }

            // Resolve the handler lazily so a short-circuiting behavior never forces resolution.
            PanRequestHandlerDelegate<TResponse> next = () =>
                provider
                    .GetRequiredService<IPanRequestHandler<TRequest, TResponse>>()
                    .Handle(typedRequest, cancellationToken);

            // First-registered behavior ends up outermost: fold from the last behavior down to
            // index 1, then invoke behavior[0] directly. This avoids LINQ Reverse(), its buffer,
            // and one extra closure/delegate invocation per call.
            for (var i = count - 1; i >= 1; i--)
            {
                var current = next;
                var behavior = behaviorList[i];
                next = () => behavior.Handle(typedRequest, current, cancellationToken);
            }

            return behaviorList[0].Handle(typedRequest, next, cancellationToken);
        }
    }
}
