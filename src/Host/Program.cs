using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Features;
using Helion.Host.Configuration;
using Helion.Host.Gateway;
using Helion.Host.Middleware;
using Helion.Host.Telemetry;
using Helion.Host.Automation;
using Helion.Host.Orchestration;

var builder = WebApplication.CreateSlimBuilder(args);

// 1. Config & Core Services
var config = HelionConfig.Load();
builder.Services.AddSingleton(config);
builder.Services.AddSingleton<LogDispatcher>();
builder.Services.AddSingleton<ProcessManager>();
builder.Services.AddSingleton<DockerProvider>();
builder.Services.AddSingleton<InfrastructureManager>();
builder.Services.AddHostedService<LogPartitionWorker>();

// 2. Automation
builder.Services.AddSingleton<LuaEngine>();
builder.Services.AddSingleton<PowerShellManager>();
builder.Services.AddSingleton<HookOrchestrator>();

// 3. Middleware Services
builder.Services.AddSingleton<GoReverseProxy>();
builder.Services.AddCors();

// 4. Kestrel Limits & Kestrel Config
builder.WebHost.ConfigureKestrel(options =>
{
    // 20-Pass Fix: 5MB Max Body Size to prevent OOM
    options.Limits.MaxRequestBodySize = 5242880; 
});

var app = builder.Build();

// 5. 20-Pass Fix: CORS Policy
app.UseCors(policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());

// 6. Boot Sequence
UI.PrintLogo("4.0.0");
UI.PrintHeader("System Initialization");

var logger = app.Services.GetRequiredService<LogDispatcher>();
var processManager = app.Services.GetRequiredService<ProcessManager>();
var infraManager = app.Services.GetRequiredService<InfrastructureManager>();

logger.LogInfo("System", "Helion Edge Gateway Booting...");

// 20-Pass Fix: Auto-Bootstrap System DB
await infraManager.EnsureSystemDatabaseAsync(config.Infrastructure.ConnectionString);

// 20-Pass Fix: Boot Race Conditions (ProcessManager handles internal wait)
processManager.StartDataEngine(config.Infrastructure.ConnectionString); 

// 7. Middleware Pipeline
app.UseMiddleware<GlobalExceptionMiddleware>();
app.UseMiddleware<RateLimiterMiddleware>(); // Interlocked TokenBucket

// 8. UDS Reverse Proxy
app.Map("/api/{**catch-all}", async (HttpContext context, GoReverseProxy proxy) =>
{
    await proxy.InvokeAsync(context);
});

// 9. 20-Pass Fix: SPA Fallback Routing
app.MapFallbackToFile("index.html");

app.Run();
