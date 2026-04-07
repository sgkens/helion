namespace Helion.Host.Telemetry;

using Microsoft.Extensions.Hosting;
using System;
using System.Threading;
using System.Threading.Tasks;

public class LogPartitionWorker : BackgroundService
{
    private readonly LogDispatcher _logDispatcher;

    public LogPartitionWorker(LogDispatcher logDispatcher)
    {
        _logDispatcher = logDispatcher;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logDispatcher.LogInfo("Telemetry", "Log Partition Worker started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            if (_logDispatcher.PostgresSink != null)
            {
                _logDispatcher.LogInfo("Telemetry", "Executing log partition rotation...");
                await _logDispatcher.PostgresSink.RotatePartitionsAsync();
            }
            
            // Run every 24 hours
            await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
        }
    }
}
