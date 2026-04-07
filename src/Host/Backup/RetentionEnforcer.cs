namespace Helion.Host.Backup;

using System.Threading.Tasks;

public class RetentionEnforcer
{
    private readonly ResticManager _restic;

    public RetentionEnforcer(ResticManager restic)
    {
        _restic = restic;
    }

    public async Task EnforcePolicyAsync(RetentionPolicy policy)
    {
        string args = $"forget --keep-daily {policy.KeepDaily} " +
                      $"--keep-weekly {policy.KeepWeekly} " +
                      $"--keep-monthly {policy.KeepMonthly} " +
                      $"--prune";

        // This executes the deduplication cleanup
        await _restic.ExecuteResticCommandAsync(args);
    }
}

public class RetentionPolicy
{
    public int KeepDaily { get; set; } = 7;
    public int KeepWeekly { get; set; } = 4;
    public int KeepMonthly { get; set; } = 6;
}
