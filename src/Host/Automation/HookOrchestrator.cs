namespace Helion.Host.Automation;

using System;
using System.Threading.Tasks;
using Helion.Host.Telemetry;

public class HookOrchestrator
{
    private readonly LuaEngine _lua;
    private readonly PowerShellManager _ps;
    private readonly LogDispatcher _logger;

    public HookOrchestrator(LuaEngine lua, PowerShellManager ps, LogDispatcher logger)
    {
        _lua = lua;
        _ps = ps;
        _logger = logger;
    }

    public void RunPreRequestHook(string script)
    {
        try 
        {
            _lua.ExecuteScript(script);
        }
        catch (Exception ex)
        {
            _logger.LogError("LuaHook", ex);
            throw;
        }
    }

    public async Task RunPostRequestHookAsync(string script)
    {
        try 
        {
            await _ps.ExecuteScriptAsync(script);
        }
        catch (Exception ex)
        {
            _logger.LogError("PowerShellHook", ex);
        }
    }
}
