namespace Helion.Host.Backup;

using System;
using System.Diagnostics;
using System.Threading.Tasks;

public class SnapshotOrchestrator
{
    private readonly ResticManager _restic;

    public SnapshotOrchestrator(ResticManager restic)
    {
        _restic = restic;
    }

    public async Task ExecuteDatabaseSnapshotAsync(string databaseUri, string snapshotAlias)
    {
        await _restic.InitializeRepositoryAsync();

        // 20-Pass Fix: Restic Pipe Timeouts (30-minute limit)
        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(30));

        var dumpProcess = StartPgDump(databaseUri);
        var resticProcess = _restic.StartStdinBackup(snapshotAlias);

        try
        {
            // Stream pg_dump stdout directly into restic stdin
            await dumpProcess.StandardOutput.BaseStream.CopyToAsync(resticProcess.StandardInput.BaseStream, cts.Token);
            
            resticProcess.StandardInput.Close();

            await Task.WhenAll(dumpProcess.WaitForExitAsync(cts.Token), resticProcess.WaitForExitAsync(cts.Token));

            if (resticProcess.ExitCode != 0)
            {
                string error = await resticProcess.StandardError.ReadToEndAsync(cts.Token);
                throw new BackupException($"Restic backup failed: {error}");
            }
        }
        catch (OperationCanceledException)
        {
            dumpProcess.Kill();
            resticProcess.Kill();
            throw new BackupException("Database snapshot operation timed out after 30 minutes.");
        }
        finally
        {
            dumpProcess.Dispose();
            resticProcess.Dispose();
        }
    }

    private Process StartPgDump(string databaseUri)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "pg_dump", // Assumes pg_dump is available on the host path or orchestrated container
            Arguments = $"--clean --if-exists --format=custom {databaseUri}",
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        var process = new Process { StartInfo = psi };
        process.Start();
        return process;
    }
}
