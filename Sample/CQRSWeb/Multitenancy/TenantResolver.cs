using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CQRSCode.Multitenancy;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using SaasKit.Multitenancy;

namespace CQRSWeb.Multitenancy
{
    public class TenantResolver : MemoryCacheTenantResolver<Tenant>
    {
        public TenantResolver(IMemoryCache cache, ILoggerFactory loggerFactory) : base(cache, loggerFactory)
        {
        }

        public TenantResolver(IMemoryCache cache, ILoggerFactory loggerFactory, MemoryCacheTenantResolverOptions options) : base(cache, loggerFactory, options)
        {
        }

        protected override string GetContextIdentifier(HttpContext context)
        {
            return context.Request.Query.Keys.FirstOrDefault() ?? "Default";
        }

        protected override IEnumerable<string> GetTenantIdentifiers(TenantContext<Tenant> context)
        {
            return new List<string>() { context.Tenant.Name };
        }

        protected override Task<TenantContext<Tenant>> ResolveAsync(HttpContext context)
        {
            var tenantName = context.Request.Query.Keys.FirstOrDefault() ?? "Default";
            var tenantContext = new TenantContext<Tenant>(new Tenant() { Name = tenantName });
            return Task.FromResult(tenantContext);
        }
    }
}
