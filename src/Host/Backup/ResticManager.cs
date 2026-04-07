namespace Helion.Host.Backup;

using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

public class BackupException : Exception
{
    public BackupException(string message) : base(message) { }
}

public class CommandResult
{
    public bool IsSuccess { get; set; }
    public string Error { get; set; } = "";
    public string Output { get; set; } = "";
}

public class ResticManager
{
    private readonly string _resticPath;
    private readonly string _repoUri;
    private readonly string _repoPassword;

    public ResticManager(string repoUri, string repoPassword)
    {
        _resticPath = Path.Combine(AppContext.BaseDirectory, ".runtime", "restic");
        _repoUri = repoUri;
        _repoPassword = repoPassword;
    }

    public async Task InitializeRepositoryAsync()
    {
        // Executes: restic init
        var result = await ExecuteResticCommandAsync("init");
        // Ignore "repository master key and config already initialized" errors
        if (!result.IsSuccess && !result.Error.Contains("already initialized"))
        {
            throw new BackupException($"Failed to initialize restic repository: {result.Error}");
        }
    }

    public Process StartStdinBackup(string snapshotName)
    {
        var psi = new ProcessStartInfo
        {
            FileName = _resticPath,
            Arguments = $"backup --stdin --stdin-filename {snapshotName}.sql",
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        psi.EnvironmentVariables["RESTIC_REPOSITORY"] = _repoUri;
        psi.EnvironmentVariables["RESTIC_PASSWORD"] = _repoPassword;

        var process = new Process { StartInfo = psi };
        process.Start();
        return process;
    }

    public Process StartStdoutDump(string snapshotId)
    {
        var psi = new ProcessStartInfo
        {
            FileName = _resticPath,
            Arguments = $"dump {snapshotId} {snapshotId}.sql",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        psi.EnvironmentVariables["RESTIC_REPOSITORY"] = _repoUri;
        psi.EnvironmentVariables["RESTIC_PASSWORD"] = _repoPassword;

        var process = new Process { StartInfo = psi };
        process.Start();
        return process;
    }

    public async Task<CommandResult> ExecuteResticCommandAsync(string arguments)
    {
        var psi = new ProcessStartInfo
        {
            FileName = _resticPath,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        psi.EnvironmentVariables["RESTIC_REPOSITORY"] = _repoUri;
        psi.EnvironmentVariables["RESTIC_PASSWORD"] = _repoPassword;

        using var process = new Process { StartInfo = psi };
        process.Start();

        string output = await process.StandardOutput.ReadToEndAsync();
        string error = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        return new CommandResult
        {
            IsSuccess = process.ExitCode == 0,
            Output = output,
            Error = error
        };
    }
}
