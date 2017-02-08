using CQRSlite.Bus;
using CQRSlite.Commands;
using CQRSlite.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace CQRSlite.Config
{
    public class BusRegistrar
    {
        private readonly IServiceProvider _serviceProvider;

        public BusRegistrar(IServiceProvider serviceProvider)
        {
            if (serviceProvider == null)
            {
                throw new ArgumentNullException(nameof(serviceProvider));
            }
            _serviceProvider = serviceProvider;
        }

        public void Register(params Type[] typesFromAssemblyContainingMessages)
        {
            var bus = _serviceProvider.GetService<IHandlerRegistrar>();

            foreach (var typesFromAssemblyContainingMessage in typesFromAssemblyContainingMessages)
            {
                var executorsAssembly = typesFromAssemblyContainingMessage.GetTypeInfo().Assembly;
                var executorTypes = executorsAssembly
                    .GetTypes()
                    .Select(t => new { Type = t, Interfaces = ResolveMessageHandlerInterface(t) })
                    .Where(e => e.Interfaces != null && e.Interfaces.Any());

                foreach (var executorType in executorTypes)
                {
                    foreach (var @interface in executorType.Interfaces)
                    {
                        InvokeHandler(@interface, bus, executorType.Type);
                    }
                }
            }
        }

        private void InvokeHandler(Type @interface, IHandlerRegistrar bus, Type executorType)
        {
            var commandType = @interface.GetGenericArguments()[0];

            var registerExecutorMethod = bus
                .GetType()
                .GetMethods(BindingFlags.Instance | BindingFlags.Public)
                .Where(mi => mi.Name == "RegisterHandler")
                .Where(mi => mi.IsGenericMethod)
                .Where(mi => mi.GetGenericArguments().Count() == 1)
                .Single(mi => mi.GetParameters().Count() == 1)
                .MakeGenericMethod(commandType);

            var del = new Action<dynamic>(x =>
            {
                dynamic handler = _serviceProvider.GetService(executorType);
                handler.Handle(x);
            });

            registerExecutorMethod.Invoke(bus, new object[] { del });
        }

        private static IEnumerable<Type> ResolveMessageHandlerInterface(Type type)
        {
            return type
                .GetInterfaces()
                .Where(i => i.GetTypeInfo().IsGenericType && ((i.GetGenericTypeDefinition() == typeof(ICommandHandler<>))
                                                || i.GetGenericTypeDefinition() == typeof(IEventHandler<>)));
        }
    }
}
