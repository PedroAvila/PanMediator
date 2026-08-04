using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace PAN.Mediator.Registration
{
    public sealed class PanMediatorServiceConfiguration
    {
        internal List<Assembly> AssembliesToRegister { get; } = new();

        internal List<Type> BehaviorsToRegister { get; } = new();

        internal Type? NotificationPublishStrategyType { get; private set; }

        public ServiceLifetime MediatorLifetime { get; set; } = ServiceLifetime.Transient;

        public ServiceLifetime HandlerLifetime { get; set; } = ServiceLifetime.Transient;

        public ServiceLifetime BehaviorLifetime { get; set; } = ServiceLifetime.Transient;

        public ServiceLifetime NotificationPublishStrategyLifetime { get; set; } = ServiceLifetime.Transient;

        public PanMediatorServiceConfiguration RegisterServicesFromAssembly(Assembly assembly)
        {
            if (assembly is null)
                throw new ArgumentNullException(nameof(assembly));

            AssembliesToRegister.Add(assembly);
            return this;
        }

        public PanMediatorServiceConfiguration RegisterServicesFromAssemblies(
            params Assembly[] assemblies
        )
        {
            if (assemblies is null)
                throw new ArgumentNullException(nameof(assemblies));

            AssembliesToRegister.AddRange(assemblies);
            return this;
        }

        public PanMediatorServiceConfiguration AddOpenBehavior(Type openBehaviorType)
        {
            if (openBehaviorType is null)
                throw new ArgumentNullException(nameof(openBehaviorType));

            if (!openBehaviorType.IsGenericTypeDefinition)
                throw new ArgumentException(
                    $"'{openBehaviorType.Name}' must be an open generic type definition (e.g. typeof(LoggingBehavior<,>)).",
                    nameof(openBehaviorType)
                );

            BehaviorsToRegister.Add(openBehaviorType);
            return this;
        }

        public PanMediatorServiceConfiguration AddBehavior<TBehavior>()
            where TBehavior : class
        {
            BehaviorsToRegister.Add(typeof(TBehavior));
            return this;
        }

        /// <summary>
        /// Registers an open-generic notification publish strategy (e.g.
        /// <c>typeof(ParallelWhenAllNotificationPublishStrategy&lt;&gt;)</c>) used for every <c>Publish</c> call.
        /// Calling this more than once replaces the previously configured strategy (last call wins) — unlike
        /// <see cref="AddOpenBehavior"/>/<see cref="AddBehavior{TBehavior}"/>, which accumulate into a chain,
        /// a publish strategy is a single active choice, not a chain. If never called, Publish uses the
        /// built-in sequential (foreach-await) strategy — PAN.Mediator's default behavior.
        /// </summary>
        public PanMediatorServiceConfiguration UseNotificationPublishStrategy(Type openStrategyType)
        {
            if (openStrategyType is null)
                throw new ArgumentNullException(nameof(openStrategyType));

            if (!openStrategyType.IsGenericTypeDefinition)
                throw new ArgumentException(
                    $"'{openStrategyType.Name}' must be an open generic type definition (e.g. typeof(ParallelWhenAllNotificationPublishStrategy<>)).",
                    nameof(openStrategyType)
                );

            NotificationPublishStrategyType = openStrategyType;
            return this;
        }
    }
}
