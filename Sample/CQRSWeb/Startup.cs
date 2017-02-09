using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using CQRSlite.Bus;
using CQRSlite.Commands;
using CQRSlite.Events;
using CQRSlite.Domain;
using CQRSCode.WriteModel;
using CQRSlite.Cache;
using Microsoft.Extensions.Caching.Memory;
using CQRSCode.ReadModel;
using CQRSlite.Config;
using CQRSCode.WriteModel.Handlers;
using Scrutor;
using System.Reflection;
using System.Linq;
using CQRSCode.Multitenancy;
using Microsoft.AspNetCore.Http;
using ISession = CQRSlite.Domain.ISession;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CQRSWeb
{
    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddScoped<Tenant>(TenantFactory);
            //services.AddScoped<Tenant>(provider => new Tenant() { Name = "Test"});


            services.AddMemoryCache();

            //Add Cqrs services
            services.AddSingleton<InProcessBus>(new InProcessBus());
            services.AddSingleton<ICommandSender>(y => y.GetService<InProcessBus>());
            services.AddSingleton<IEventPublisher>(y => y.GetService<InProcessBus>());
            services.AddSingleton<IHandlerRegistrar>(y => y.GetService<InProcessBus>());
            services.AddSingleton<BusRegistrar>();
            services.AddScoped<ISession, Session>();
            services.AddScoped<IEventStore, InMemoryEventStore>();
            services.AddScoped<ICache, CQRSlite.Cache.MemoryCache>();
            services.AddScoped<IRepository>(y => new CacheRepository(new Repository(y.GetService<IEventStore>()), y.GetService<IEventStore>(), y.GetService<ICache>()));

            services.AddTransient<IReadModelFacade, ReadModelFacade>();

            //Scan for commandhandlers and eventhandlers
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


            // Register command handlers
            services.BuildServiceProvider().GetService<BusRegistrar>().Register(typeof(InventoryCommandHandlers));

            // Add framework services.
            services.AddMvc();
        }

        //private BusRegistrar BusRegistrarFactory(IServiceProvider serviceProvider)
        //{
        //    var busRegistrar = new BusRegistrar(serviceProvider);
        //    return busRegistrar;
        //}

        private Tenant TenantFactory(IServiceProvider serviceProvider)
        {
            var context = serviceProvider.GetService<IHttpContextAccessor>();
            var tenantNameFromQUeryString = context?.HttpContext?.Request.Host.Host ?? "Default";
            return new Tenant() { Name = tenantNameFromQUeryString };
            //return new Tenant() { Name = "From factory"};
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole(LogLevel.Debug);

            app.UseDeveloperExceptionPage();
            app.UseStaticFiles();

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
