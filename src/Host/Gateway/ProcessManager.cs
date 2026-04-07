namespace Helion.Host.Gateway;

using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Helion.Host.Telemetry;

public class ProcessManager : IDisposable
{
    private Process? _goEngineProcess;
    private readonly CancellationTokenSource _cts = new();
    private readonly LogDispatcher _logger;
    public string? UdsPath { get; private set; }
    public string IpcToken { get; private set; } = "";

    public ProcessManager(LogDispatcher logger)
    {
        _logger = logger;
    }

    public void StartDataEngine(string systemDbUri)
    {
        UdsPath = Path.Combine(Path.GetTempPath(), $"helion_{Guid.NewGuid():N}.sock");
        IpcToken = GenerateSecureToken();

        CleanupOrphanedSockets();

        Task.Run(() => MonitorProcess(systemDbUri, _cts.Token));
    }

    private void CleanupOrphanedSockets()
    {
        // 20-Pass Fix: Orphan Socket Cleanup
        try 
        {
            var files = Directory.GetFiles(Path.GetTempPath(), "helion_*.sock");
            foreach (var file in files)
            {
                File.Delete(file);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("Cleanup", ex);
        }
    }

    private void MonitorProcess(string systemDbUri, CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            string enginePath = RuntimeLocator.GetGoExecutablePath();

            if (!File.Exists(enginePath))
            {
                _logger.LogError("ProcessManager", new Exception($"Data Engine not found at {enginePath}"));
                Thread.Sleep(5000);
                continue;
            }

            _goEngineProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = enginePath,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    EnvironmentVariables = 
                    {
                        { "HELION_UDS_PATH", UdsPath },
                        { "HELION_IPC_TOKEN", IpcToken },
                        { "HELION_SYSTEM_DB", systemDbUri }
                    }
                }
            };


            try 
            {
                _goEngineProcess.Start();
                _goEngineProcess.WaitForExit();
            }
            catch (Exception ex)
            {
                _logger.LogError("ProcessManager", ex);
            }

            if (!token.IsCancellationRequested)
            {
                _logger.LogError("ProcessManager", new Exception("Data Engine crashed. Respawning in 1000ms."));
                Thread.Sleep(1000);
            }
        }
    }

    private int GetAvailablePort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        int port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    private string GenerateSecureToken()
    {
        byte[] tokenBytes = new byte[32];
        RandomNumberGenerator.Fill(tokenBytes);
        return Convert.ToBase64String(tokenBytes);
    }

    public void Dispose()
    {
        _cts.Cancel();
        if (_goEngineProcess != null && !_goEngineProcess.HasExited)
        {
            _goEngineProcess.Kill();
            _goEngineProcess.Dispose();
        }
    }
}
