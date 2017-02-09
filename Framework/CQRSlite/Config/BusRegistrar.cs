using CQRSlite.Bus;
using CQRSlite.Commands;
using CQRSlite.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace CQRSlite.Config
{
    public class BusRegistrar
    {
        private readonly IServiceProvider _services;
        private readonly IHandlerRegistrar _handlerRegistrar;

        public BusRegistrar(IServiceProvider services, IHandlerRegistrar handlerRegistrar)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            _services = services;
            _handlerRegistrar = handlerRegistrar;
        }

        public void Register(params Type[] typesFromAssemblyContainingMessages)
        {
            

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
                        RegisterHandler(@interface, _handlerRegistrar, executorType.Type);
                    }
                }
            }
        }

        private void RegisterHandler(Type @interface, IHandlerRegistrar bus, Type executorType)
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
                dynamic handler = _services.GetService(executorType);
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
