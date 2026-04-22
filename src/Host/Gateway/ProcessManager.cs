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

    public async Task StartDataEngineAsync(string systemDbUri)
    {
        UdsPath = Path.Combine(Path.GetTempPath(), $"helion_{Guid.NewGuid():N}.sock");
        IpcToken = GenerateSecureToken();

        CleanupOrphanedSockets();

        _ = Task.Run(() => MonitorProcess(systemDbUri, _cts.Token));

        // 20-Pass Fix: Boot Race Conditions (Wait for Go Engine)
        await WaitForEngineReadyAsync(_cts.Token);
    }

    private async Task WaitForEngineReadyAsync(CancellationToken token)
    {
        using var handler = new SocketsHttpHandler
        {
            ConnectCallback = async (context, cancellationToken) =>
            {
                var socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
                await socket.ConnectAsync(new UnixDomainSocketEndPoint(UdsPath!), cancellationToken);
                return new NetworkStream(socket, ownsSocket: true);
            }
        };

        using var client = new HttpClient(handler);
        int attempts = 0;
        while (!token.IsCancellationRequested && attempts < 30)
        {
            try
            {
                var response = await client.GetAsync("http://localhost/health", token);
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInfo("ProcessManager", "Data Engine is ready.");
                    return;
                }
            }
            catch
            {
                // Engine not ready yet
            }
            attempts++;
            await Task.Delay(500, token);
        }

        throw new Exception("Data Engine failed to reach ready state within 15 seconds.");
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
