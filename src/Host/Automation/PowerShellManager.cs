namespace Helion.Host.Automation;

using System;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Threading;
using System.Threading.Tasks;

public class PowerShellManager
{
    public async Task ExecuteScriptAsync(string script, CancellationToken cancellationToken = default)
    {
        using var runspace = RunspaceFactory.CreateRunspace();
        runspace.Open();

        using var ps = PowerShell.Create();
        ps.Runspace = runspace;
        ps.AddScript(script);

        // 20-Pass Fix: PowerShell execution wrapped in a strict CancellationToken (default 5s)
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(TimeSpan.FromSeconds(5));

        try
        {
            var results = await ps.InvokeAsync<PSObject>(null, new PSInvocationSettings { Host = null }, null, cts.Token);
            foreach (var result in results)
            {
                // Process results
            }
        }
        catch (OperationCanceledException)
        {
            throw new Exception("PowerShell script execution timed out.");
        }
        finally
        {
            runspace.Close();
        }
    }
}
