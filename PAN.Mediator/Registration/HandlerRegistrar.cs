using System;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using PAN.Mediator.Abstractions;
using PAN.Mediator.Notifications;

namespace PAN.Mediator.Registration
{
    internal static class HandlerRegistrar
    {
        private static readonly Type[] OpenHandlerInterfaces =
        {
            typeof(IPanRequestHandler<,>),
            typeof(IPanNotificationHandler<>),
        };

        public static void RegisterHandlersFromAssembly(IServiceCollection services, Assembly assembly, ServiceLifetime lifetime)
        {
            foreach (var type in GetLoadableTypes(assembly))
            {
                if (type.IsAbstract || type.IsInterface) continue;

                foreach (var implementedInterface in type.GetInterfaces())
                {
                    if (!implementedInterface.IsGenericType) continue;

                    var openGeneric = implementedInterface.GetGenericTypeDefinition();
                    if (Array.IndexOf(OpenHandlerInterfaces, openGeneric) < 0) continue;

                    services.Add(new ServiceDescriptor(implementedInterface, type, lifetime));
                }
            }
        }

        private static Type[] GetLoadableTypes(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                return Array.FindAll(ex.Types, t => t is not null)!;
            }
        }
    }
}
