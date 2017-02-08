using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CQRSCode.Multitenancy;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace CQRSWeb.Multitenancy
{
    public class TenantMiddlewareLogger
    {
        RequestDelegate next;
        private readonly ILogger log;

        public TenantMiddlewareLogger(RequestDelegate next, ILoggerFactory loggerFactory)
        {
            this.next = next;
            this.log = loggerFactory.CreateLogger<TenantMiddlewareLogger>();
        }

        public async Task Invoke(HttpContext context)
        {
            var tenantContext = context.GetTenantContext<Tenant>();

            if (tenantContext != null)
            {
                var timestamp = ((DateTime)tenantContext.Properties["Created"]);

                await context.Response.WriteAsync(
                    $"Tenant \"{tenantContext.Tenant.Name}\" created at {timestamp.Ticks}");
            }
            else
            {
                await context.Response.WriteAsync("No matching tenant found.");
            }
        }
    }
}
