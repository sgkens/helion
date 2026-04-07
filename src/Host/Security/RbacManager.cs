namespace Helion.Host.Security;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;

[Flags]
public enum HelionPermission
{
    None = 0,
    Read = 1 << 0,
    Write = 1 << 1,
    Delete = 1 << 2,
    SchemaManage = 1 << 3,
    BackupRestore = 1 << 4,
    Admin = Read | Write | Delete | SchemaManage | BackupRestore
}

public class RbacManager
{
    public bool HasPermission(ClaimsPrincipal user, HelionPermission requiredPermission)
    {
        // 20-Pass Fix: Stateless RBAC (Bitwise flag validation)
        var permissionClaim = user.Claims.FirstOrDefault(c => c.Type == "helion_perms")?.Value;
        if (string.IsNullOrEmpty(permissionClaim)) return false;

        if (int.TryParse(permissionClaim, out var userPermsValue))
        {
            var userPerms = (HelionPermission)userPermsValue;
            return (userPerms & requiredPermission) == requiredPermission;
        }

        return false;
    }

    public string GeneratePermissionString(params HelionPermission[] permissions)
    {
        HelionPermission combined = HelionPermission.None;
        foreach (var p in permissions) combined |= p;
        return ((int)combined).ToString();
    }
}
