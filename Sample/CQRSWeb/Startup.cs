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
using CQRSCode;
using CQRSCode.Multitenancy;
using CQRSWeb.Multitenancy;

namespace CQRSWeb
{
    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMultitenancy<Tenant, TenantResolver>();

            services.AddMemoryCache();

            //Add Cqrs services
            services.AddSingleton<InProcessBus>(new InProcessBus());
            services.AddSingleton<ICommandSender>(y => y.GetService<InProcessBus>());
            services.AddSingleton<IEventPublisher>(y => y.GetService<InProcessBus>());
            services.AddSingleton<IHandlerRegistrar>(y => y.GetService<InProcessBus>());
            services.AddScoped<ISession, Session>();
            services.AddScoped<IEventStore, InMemoryEventStore>();
            services.AddScoped<ICache, CQRSlite.Cache.MemoryCache>();
            services.AddScoped<IRepository>(y => new CacheRepository(new Repository(y.GetService<IEventStore>()), y.GetService<IEventStore>(), y.GetService<ICache>()));
            //services.AddSingleton<BusRegistrar>(BusRegistrarFactory);
            services.AddTransient<IReadModelFacade, ReadModelFacade>();

            //Scan for commandhandlers and eventhandlers
            services.AddCQRSlite();


            // Add framework services.
            services.AddMvc();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole(LogLevel.Debug);

            app.UseMultitenancy<Tenant>();

            app.UseDeveloperExceptionPage();
            app.UseStaticFiles();

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });

            app.UseMiddleware<TenantMiddlewareLogger>();
        }
    }
}
