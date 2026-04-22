namespace Helion.Host.Security;

using System;
using System.Threading.Tasks;
using Npgsql;
using Helion.Host.Telemetry;

public class CertLockManager
{
    private readonly string _systemDbUri;
    private readonly LogDispatcher _logger;
    private const long ACME_LOCK_ID = 42001; // Specific ID for cert provisioning

    public CertLockManager(string systemDbUri, LogDispatcher logger)
    {
        _systemDbUri = systemDbUri;
        _logger = logger;
    }

    public async Task<bool> AcquireProvisioningLockAsync()
    {
        // 20-Pass Fix: HA Cert Locks (Postgres Advisory Lock)
        try
        {
            await using var connection = new NpgsqlConnection(_systemDbUri);
            await connection.OpenAsync();

            // pg_try_advisory_lock returns true if lock was acquired
            await using var cmd = new NpgsqlCommand("SELECT pg_try_advisory_lock(@id)", connection);
            cmd.Parameters.AddWithValue("id", ACME_LOCK_ID);
            
            var result = await cmd.ExecuteScalarAsync();
            return result is bool locked && locked;
        }
        catch (Exception ex)
        {
            _logger.LogError("CertLock", ex);
            return false;
        }
    }

    public async Task ReleaseProvisioningLockAsync()
    {
        try
        {
            await using var connection = new NpgsqlConnection(_systemDbUri);
            await connection.OpenAsync();

            await using var cmd = new NpgsqlCommand("SELECT pg_advisory_unlock(@id)", connection);
            cmd.Parameters.AddWithValue("id", ACME_LOCK_ID);
            await cmd.ExecuteScalarAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError("CertLock", ex);
        }
    }
}
