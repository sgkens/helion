namespace Helion.Host.Telemetry;

using System;
using System.IO;
using Helion.Host.Configuration;
using Helion.Host.Gateway;
using Spectre.Console;

public class LogDispatcher
{
    private readonly string _logDirectory;
    public PostgresLogSink? PostgresSink { get; }
    private static readonly object _fileLock = new();

    public LogDispatcher(HelionConfig config)
    {
        _logDirectory = Path.Combine(AppContext.BaseDirectory, "logs");
        if (!Directory.Exists(_logDirectory)) Directory.CreateDirectory(_logDirectory);

        if (!string.IsNullOrEmpty(config.Infrastructure.ConnectionString))
        {
            try 
            {
                PostgresSink = new PostgresLogSink(config.Infrastructure.ConnectionString);
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[{HelionPalette.Error.ToMarkup()}] [LogDispatcher] Failed to initialize PostgresLogSink: {Markup.Escape(ex.Message)}[/]");
            }
        }
    }

    public void LogError(string context, Exception ex, string? correlationId = null)
    {
        string timestamp = DateTime.UtcNow.ToString("O");
        string message = ex.Message;
        string? stackTrace = ex.StackTrace;

        // 1. Console Sink (Spectre)
        AnsiConsole.MarkupLine($"[{HelionPalette.Error.ToMarkup()}][ERROR] {timestamp} | {Markup.Escape(context)} | {Markup.Escape(message)}[/]");

        // 2. Postgres Sink
        PostgresSink?.EnqueueLog("ERROR", context, message, correlationId);

        // 3. Flat File Sink
        WriteToFile($"[ERROR] {timestamp} | {correlationId} | {context} | {message}\n{stackTrace}");
    }

    public void LogInfo(string context, string message, string? correlationId = null)
    {
        string timestamp = DateTime.UtcNow.ToString("O");

        // 1. Console Sink (Spectre)
        AnsiConsole.MarkupLine($"[{HelionPalette.Accent.ToMarkup()}][INFO]  {timestamp} | {Markup.Escape(context)} | {Markup.Escape(message)}[/]");

        // 2. Postgres Sink
        PostgresSink?.EnqueueLog("INFO", context, message, correlationId);

        // 3. Flat File Sink
        WriteToFile($"[INFO]  {timestamp} | {correlationId} | {context} | {message}");
    }

    private void WriteToFile(string logEntry)
    {
        string currentFile = Path.Combine(_logDirectory, $"helion_{DateTime.UtcNow:yyyyMMdd}.log");
        lock (_fileLock)
        {
            File.AppendAllText(currentFile, logEntry + Environment.NewLine);
        }
    }
}
