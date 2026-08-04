using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PAN.Mediator.Abstractions;
using PAN.Mediator.Notifications;

namespace PAN.Mediator.Tests
{
    public sealed class PingRequest : IPanRequest<string>
    {
        public string Message { get; init; } = string.Empty;
    }

    public sealed class PingRequestHandler : IPanRequestHandler<PingRequest, string>
    {
        public Task<string> Handle(PingRequest request, CancellationToken cancellationToken)
            => Task.FromResult($"Pong: {request.Message}");
    }

    public sealed class VoidRequest : IPanRequest
    {
    }

    public sealed class VoidRequestHandler : IPanRequestHandler<VoidRequest>
    {
        public Task<Unit> Handle(VoidRequest request, CancellationToken cancellationToken)
            => Unit.Task;
    }

    public sealed class PingNotification : IPanNotification
    {
        public string Message { get; init; } = string.Empty;
    }

    public sealed class PingNotificationHandlerOne : IPanNotificationHandler<PingNotification>
    {
        public Task Handle(PingNotification notification, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    public sealed class PingNotificationHandlerTwo : IPanNotificationHandler<PingNotification>
    {
        public Task Handle(PingNotification notification, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    public sealed class CallRecorder
    {
        public List<string> Calls { get; } = new();
    }

    public sealed class RecordingBehaviorA<TRequest, TResponse> : IPanPipelineBehavior<TRequest, TResponse>
        where TRequest : IPanRequest<TResponse>
    {
        private readonly CallRecorder _recorder;

        public RecordingBehaviorA(CallRecorder recorder) => _recorder = recorder;

        public async Task<TResponse> Handle(TRequest request, PanRequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            _recorder.Calls.Add("A-before");
            var response = await next();
            _recorder.Calls.Add("A-after");
            return response;
        }
    }

    public sealed class RecordingBehaviorB<TRequest, TResponse> : IPanPipelineBehavior<TRequest, TResponse>
        where TRequest : IPanRequest<TResponse>
    {
        private readonly CallRecorder _recorder;

        public RecordingBehaviorB(CallRecorder recorder) => _recorder = recorder;

        public async Task<TResponse> Handle(TRequest request, PanRequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            _recorder.Calls.Add("B-before");
            var response = await next();
            _recorder.Calls.Add("B-after");
            return response;
        }
    }

    public sealed class ShortCircuitBehavior : IPanPipelineBehavior<PingRequest, string>
    {
        public Task<string> Handle(PingRequest request, PanRequestHandlerDelegate<string> next, CancellationToken cancellationToken)
            => Task.FromResult("short-circuited");
    }

    // Separate from PingNotification so these fixtures (which need constructor args HandlerRegistrar's
    // assembly scan can't supply) never get swept up by tests that scan the assembly and Publish(PingNotification).
    public sealed class ProbeNotification : IPanNotification
    {
    }

    public sealed class ThrowingNotificationHandler : IPanNotificationHandler<ProbeNotification>
    {
        private readonly Exception _exception;

        public ThrowingNotificationHandler(Exception exception) => _exception = exception;

        public Task Handle(ProbeNotification notification, CancellationToken cancellationToken) => throw _exception;
    }

    public sealed class ConcurrencyTracker
    {
        private int _active;
        private int _maxActive;

        public int MaxActive => _maxActive;

        public IDisposable Enter()
        {
            var current = Interlocked.Increment(ref _active);

            int observedMax;
            do
            {
                observedMax = _maxActive;
                if (current <= observedMax) break;
            } while (Interlocked.CompareExchange(ref _maxActive, current, observedMax) != observedMax);

            return new Exit(this);
        }

        private sealed class Exit : IDisposable
        {
            private readonly ConcurrencyTracker _tracker;

            public Exit(ConcurrencyTracker tracker) => _tracker = tracker;

            public void Dispose() => Interlocked.Decrement(ref _tracker._active);
        }
    }

    public sealed class ConcurrencyProbeNotificationHandler : IPanNotificationHandler<ProbeNotification>
    {
        private readonly ConcurrencyTracker _tracker;
        private readonly TimeSpan _delay;

        public ConcurrencyProbeNotificationHandler(ConcurrencyTracker tracker, TimeSpan delay)
        {
            _tracker = tracker;
            _delay = delay;
        }

        public async Task Handle(ProbeNotification notification, CancellationToken cancellationToken)
        {
            using (_tracker.Enter())
            {
                await Task.Delay(_delay, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    public sealed class RecordingNotificationPublishStrategy<TNotification> : IPanNotificationPublishStrategy<TNotification>
        where TNotification : IPanNotification
    {
        private readonly CallRecorder _recorder;

        public RecordingNotificationPublishStrategy(CallRecorder recorder) => _recorder = recorder;

        public async Task Publish(TNotification notification, IEnumerable<IPanNotificationHandler<TNotification>> handlers, CancellationToken cancellationToken)
        {
            _recorder.Calls.Add("strategy-invoked");
            foreach (var handler in handlers)
            {
                await handler.Handle(notification, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
