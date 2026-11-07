using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PAN.Mediator.Notifications
{
    /// <summary>
    /// Starts every handler without awaiting in between, then awaits all of them via
    /// <see cref="Task.WhenAll(IEnumerable{Task})"/>. Unlike <see cref="SequentialNotificationPublishStrategy{TNotification}"/>,
    /// every handler runs to completion even if one or more throw. Awaiting the returned task rethrows only the
    /// first exception encountered (standard Task.WhenAll unwrapping); the full set of exceptions remains
    /// available as an <see cref="System.AggregateException"/> via the task's <see cref="Task.Exception"/>
    /// property for callers who inspect it directly instead of awaiting.
    /// </summary>
    public sealed class ParallelWhenAllNotificationPublishStrategy<TNotification> : IPanNotificationPublishStrategy<TNotification>
        where TNotification : IPanNotification
    {
        public ValueTask Publish(
            TNotification notification,
            IEnumerable<IPanNotificationHandler<TNotification>> handlers,
            CancellationToken cancellationToken
        )
        {
            var tasks = new List<Task>();
            foreach (var handler in handlers)
            {
                Task task;
                try
                {
                    task = handler.Handle(notification, cancellationToken).AsTask();
                }
                catch (Exception ex)
                {
                    // A handler that throws synchronously (before its first await) must not stop the
                    // remaining handlers from starting — capture it as a faulted task instead.
                    task = Task.FromException(ex);
                }

                tasks.Add(task);
            }

            return tasks.Count == 0 ? default : new ValueTask(Task.WhenAll(tasks));
        }
    }
}
