namespace Helion.Host.Backup;

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Helion.Host.Telemetry;

public class RestoreOrchestrator
{
    private readonly ResticManager _restic;
    private readonly LogDispatcher _logger;

    public RestoreOrchestrator(ResticManager restic, LogDispatcher logger)
    {
        _restic = restic;
        _logger = logger;
    }

    public async Task ExecuteDatabaseRestoreAsync(string databaseUri, string snapshotId)
    {
        _logger.LogInfo("Backup", $"Initiating database restore from snapshot: {snapshotId}");

        var resticProcess = StartResticDump(snapshotId);
        var psqlProcess = StartPgRestore(databaseUri);

        try
        {
            // Stream restic dump directly into psql
            await resticProcess.StandardOutput.BaseStream.CopyToAsync(psqlProcess.StandardInput.BaseStream);
            
            psqlProcess.StandardInput.Close();

            await Task.WhenAll(resticProcess.WaitForExitAsync(), psqlProcess.WaitForExitAsync());

            if (psqlProcess.ExitCode != 0)
            {
                string error = await psqlProcess.StandardError.ReadToEndAsync();
                throw new BackupException($"Postgres restore failed: {error}");
            }

            _logger.LogInfo("Backup", "Database restore completed successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError("Backup", ex);
            throw;
        }
        finally
        {
            resticProcess.Dispose();
            psqlProcess.Dispose();
        }
    }

    private Process StartResticDump(string snapshotId)
    {
        // Executes: restic dump <snapshotId> helion_backup.sql
        // Assumes the stdin-filename used during backup was consistent
        return _restic.StartStdoutDump(snapshotId);
    }

    private Process StartPgRestore(string databaseUri)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "psql",
            Arguments = databaseUri,
            RedirectStandardInput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        var process = new Process { StartInfo = psi };
        process.Start();
        return process;
    }
}
