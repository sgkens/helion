namespace Helion.Host.Middleware;

using Microsoft.AspNetCore.Http;
using System.Collections.Concurrent;
using System.Threading.Tasks;

public class MaintenanceMiddleware
{
    private readonly RequestDelegate _next;
    // Thread-safe set of tenant IDs currently undergoing migration
    private static readonly ConcurrentDictionary<string, bool> _lockedTenants = new();

    public MaintenanceMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public static void LockTenant(string tenantId) => _lockedTenants.TryAdd(tenantId, true);
    public static void UnlockTenant(string tenantId) => _lockedTenants.TryRemove(tenantId, out _);

    public async Task InvokeAsync(HttpContext context)
    {
        var tenantId = context.Request.Headers["X-Helion-Tenant-Id"].ToString();

        // 20-Pass Fix: Migration Locks
        if (!string.IsNullOrEmpty(tenantId) && _lockedTenants.ContainsKey(tenantId))
        {
            context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            context.Response.Headers.Append("Retry-After", "5");
            await context.Response.WriteAsync("Tenant is undergoing migration. Please retry in 5 seconds.");
            return;
        }

        await _next(context);
    }
}
