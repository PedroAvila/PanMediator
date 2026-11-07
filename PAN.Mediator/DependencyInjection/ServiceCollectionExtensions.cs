using System;
using Microsoft.Extensions.DependencyInjection;
using PAN.Mediator.Abstractions;
using PAN.Mediator.Core;
using PAN.Mediator.Notifications;
using PAN.Mediator.Registration;

namespace PAN.Mediator.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddPanMediator(this IServiceCollection services, Action<PanMediatorServiceConfiguration> configure)
        {
            if (services is null) throw new ArgumentNullException(nameof(services));
            if (configure is null) throw new ArgumentNullException(nameof(configure));

            var configuration = new PanMediatorServiceConfiguration();
            configure(configuration);

            foreach (var assembly in configuration.AssembliesToRegister)
            {
                HandlerRegistrar.RegisterHandlersFromAssembly(services, assembly, configuration.HandlerLifetime);
            }

            // One-time (container-build) discovery: prefer the source-generated dispatcher emitted into
            // a scanned assembly, falling back to the reflection-based implementation otherwise.
            var mediatorType = typeof(PanMediator);
            foreach (var assembly in configuration.AssembliesToRegister)
            {
                var generated = assembly.GetType("PAN.Mediator.Generated.GeneratedPanMediator", throwOnError: false);
                if (generated is not null && !generated.IsAbstract && typeof(IPanMediator).IsAssignableFrom(generated))
                {
                    mediatorType = generated;
                    break;
                }
            }

            services.Add(new ServiceDescriptor(typeof(IPanMediator), mediatorType, configuration.MediatorLifetime));

            // Registration order is preserved: the first AddOpenBehavior/AddBehavior call ends up outermost in the pipeline.
            foreach (var behaviorType in configuration.BehaviorsToRegister)
            {
                services.Add(new ServiceDescriptor(typeof(IPanPipelineBehavior<,>), behaviorType, configuration.BehaviorLifetime));
            }

            if (configuration.NotificationPublishStrategyType is not null)
            {
                services.Add(new ServiceDescriptor(
                    typeof(IPanNotificationPublishStrategy<>),
                    configuration.NotificationPublishStrategyType,
                    configuration.NotificationPublishStrategyLifetime));
            }

            return services;
        }
    }
}