namespace Helion.Host.Gateway;

using System;
using System.IO;

public static class RuntimeLocator
{
    public static string GetRuntimeDirectory()
    {
        return Path.Combine(AppContext.BaseDirectory, ".runtime");
    }

    public static string GetGoExecutablePath()
    {
        return Path.Combine(GetRuntimeDirectory(), "helion-engine");
    }

    public static string GetResticExecutablePath()
    {
        return Path.Combine(GetRuntimeDirectory(), "restic");
    }
}
