using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using CQRSCode.WriteModel.Handlers;
using CQRSlite.Commands;
using CQRSlite.Config;
using CQRSlite.Events;
using Microsoft.Extensions.DependencyInjection;
using Scrutor;

namespace CQRSCode
{
    public static class CQRSliteServiceCollectionExtensions
    {
        public static IServiceCollection AddCQRSlite(this IServiceCollection services)
        {
            services.Scan(scan => scan
                .FromAssemblies(typeof(InventoryCommandHandlers).GetTypeInfo().Assembly)
                    .AddClasses(classes => classes.Where(x => {
                        var allInterfaces = x.GetInterfaces();
                        return
                            allInterfaces.Any(y => y.GetTypeInfo().IsGenericType && y.GetTypeInfo().GetGenericTypeDefinition() == typeof(ICommandHandler<>)) ||
                            allInterfaces.Any(y => y.GetTypeInfo().IsGenericType && y.GetTypeInfo().GetGenericTypeDefinition() == typeof(IEventHandler<>));
                    }))
                    .AsSelf()
                    .WithTransientLifetime()
            );

            var serviceProvider = services.BuildServiceProvider();
            var registrar = new BusRegistrar(serviceProvider);

            registrar.Register(typeof(InventoryCommandHandlers));


            return services;
        }
    }
}
