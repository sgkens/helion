namespace Helion.Host.Telemetry;

using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Npgsql;

public class PostgresLogSink : IDisposable
{
    private readonly string _systemDbUri;
    private readonly BlockingCollection<LogEntry> _logQueue;
    private readonly CancellationTokenSource _cts;

    public PostgresLogSink(string systemDbUri)
    {
        _systemDbUri = systemDbUri;
        _logQueue = new BlockingCollection<LogEntry>(boundedCapacity: 10000);
        _cts = new CancellationTokenSource();

        InitializePartitions();
        Task.Run(() => ProcessLogQueueAsync(_cts.Token));
    }

    public void EnqueueLog(string level, string context, string message, string? correlationId)
    {
        var entry = new LogEntry 
        { 
            Timestamp = DateTime.UtcNow, 
            Level = level, 
            Context = context, 
            Message = message, 
            CorrelationId = correlationId 
        };
        _logQueue.TryAdd(entry, millisecondsTimeout: 0); 
    }

    private async Task ProcessLogQueueAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                await using var connection = new NpgsqlConnection(_systemDbUri);
                await connection.OpenAsync(token);

                while (!token.IsCancellationRequested)
                {
                    var entry = _logQueue.Take(token);
                    await using var cmd = new NpgsqlCommand(@"
                        INSERT INTO system_logs (timestamp, level, context, message, correlation_id) 
                        VALUES (@ts, @lvl, @ctx, @msg, @corr)", connection);
                        
                    cmd.Parameters.AddWithValue("ts", entry.Timestamp);
                    cmd.Parameters.AddWithValue("lvl", entry.Level);
                    cmd.Parameters.AddWithValue("ctx", entry.Context);
                    cmd.Parameters.AddWithValue("msg", entry.Message);
                    cmd.Parameters.AddWithValue("corr", (object?)entry.CorrelationId ?? DBNull.Value);
                    
                    await cmd.ExecuteNonQueryAsync(token);
                }
            }
            catch (Exception ex)
            {
                // In a real scenario, we'd log this to console or a flat file
                Console.WriteLine($"[PostgresLogSink Error] {ex.Message}");
                if (!token.IsCancellationRequested)
                {
                    await Task.Delay(5000, token); // Wait before retrying
                }
            }
        }
    }

    private void InitializePartitions()
    {
        try
        {
            using var connection = new NpgsqlConnection(_systemDbUri);
            connection.Open();
            using var cmd = new NpgsqlCommand(@"
                CREATE TABLE IF NOT EXISTS system_logs (
                    id BIGSERIAL,
                    timestamp TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                    level VARCHAR(10),
                    context VARCHAR(100),
                    message TEXT,
                    correlation_id VARCHAR(50)
                ) PARTITION BY RANGE (timestamp);
            ", connection);
            cmd.ExecuteNonQuery();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[PostgresLogSink Partition Init Error] {ex.Message}");
        }
    }

    public async Task RotatePartitionsAsync()
    {
        // 20-Pass Fix: Telemetry Pruning (Daily rotation + 30-day drop)
        try
        {
            await using var connection = new NpgsqlConnection(_systemDbUri);
            await connection.OpenAsync();

            var tomorrow = DateTime.UtcNow.AddDays(1);
            var nextDay = tomorrow.AddDays(1);
            
            string partitionName = $"system_logs_y{tomorrow:yyyy}m{tomorrow:MM}d{tomorrow:dd}";
            string rangeStart = tomorrow.ToString("yyyy-MM-dd");
            string rangeEnd = nextDay.ToString("yyyy-MM-dd");

            // 1. Create Tomorrow's Partition
            await using (var cmd = new NpgsqlCommand($@"
                CREATE TABLE IF NOT EXISTS {partitionName} 
                PARTITION OF system_logs 
                FOR VALUES FROM ('{rangeStart}') TO ('{rangeEnd}')", connection))
            {
                await cmd.ExecuteNonQueryAsync();
            }

            // 2. Drop Partitions older than 30 days
            var cutoff = DateTime.UtcNow.AddDays(-30);
            // In a real production system, we would query pg_inherits or similar to find matching tables.
            // For now, we attempt to drop the specific day's partition if it follows our naming convention.
            string oldPartitionName = $"system_logs_y{cutoff:yyyy}m{cutoff:MM}d{cutoff:dd}";
            await using (var cmd = new NpgsqlCommand($"DROP TABLE IF EXISTS {oldPartitionName}", connection))
            {
                await cmd.ExecuteNonQueryAsync();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[PostgresLogSink Partition Rotation Error] {ex.Message}");
        }
    }

    public void Dispose()
    {
        _cts.Cancel();
        _logQueue.Dispose();
    }
}

public struct LogEntry
{
    public DateTime Timestamp { get; set; }
    public string Level { get; set; }
    public string Context { get; set; }
    public string Message { get; set; }
    public string? CorrelationId { get; set; }
}
