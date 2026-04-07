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

        var dumpProcess = StartPgDump(databaseUri);
        var resticProcess = _restic.StartStdinBackup(snapshotAlias);

        try
        {
            // Stream pg_dump stdout directly into restic stdin
            // This prevents RAM saturation and avoids writing large .sql files to the host disk
            await dumpProcess.StandardOutput.BaseStream.CopyToAsync(resticProcess.StandardInput.BaseStream);
            
            // Close stdin to signal to restic that the stream is complete
            resticProcess.StandardInput.Close();

            await Task.WhenAll(dumpProcess.WaitForExitAsync(), resticProcess.WaitForExitAsync());

            if (resticProcess.ExitCode != 0)
            {
                string error = await resticProcess.StandardError.ReadToEndAsync();
                throw new BackupException($"Restic backup failed: {error}");
            }
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
