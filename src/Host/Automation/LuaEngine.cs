namespace Helion.Host.Automation;

using KeraLua;
using System;
using System.IO;

public class LuaEngine : IDisposable
{
    private readonly Lua _state;
    private int _instructionCount = 0;
    private const int MaxInstructions = 10000;

    public LuaEngine()
    {
        _state = new Lua();
        _state.OpenLibs();
        
        // 20-Pass Fix: Sandboxed Automation (10,000 instructions limit)
        _state.SetHook((IntPtr L, IntPtr ar) => {
            _instructionCount++;
            if (_instructionCount > MaxInstructions)
            {
                throw new Exception("Lua execution exceeded instruction limit.");
            }
        }, LuaHookMask.Count, 1);
    }

    public void ExecuteScript(string script)
    {
        _instructionCount = 0;
        var result = _state.DoString(script);
        if (!result)
        {
            throw new Exception($"Lua Error: {_state.ToString(-1)}");
        }
    }

    public void Dispose()
    {
        _state.Dispose();
    }
}
