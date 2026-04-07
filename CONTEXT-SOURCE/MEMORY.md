# HELION MEMORY

## Environment Patterns
- **Container**: `sgkens/phellams-automator:2.8.0` is the primary build environment.
- **C#**: .NET 10.0.103.
- **Go**: Version downgraded to 1.19 in `go.mod` to match container capabilities.
- **IPC**: Uses Unix Domain Sockets (UDS) by default for high performance.
- **Theming**: Uses `Spectre.Console` for advanced CLI styling and ASCII logo rendering.

## Fixes & Optimizations
- **LuaEngine**: `DoString` returns `bool` in KeraLua 1.4.1, not `LuaStatus`. Fixed check to `if (!result)`.
- **PowerShellManager**: `InvokeAsync` requires explicit type argument `<PSObject>` when inference fails.
- **SQLite**: Always use `_journal_mode=WAL&_sync=NORMAL` for concurrent performance.
- **UDS**: IPC requires `HELION_IPC_TOKEN` handshake.
- **Go Drivers**: Switched from `pgx/v5` to `pgx/v4` to maintain compatibility with Go 1.19.
- **UDS Cleanup**: C# host globs and deletes `helion_*.sock` in temp directory on boot to prevent orphan conflicts.
- **Telemetry**: PostgreSQL partitions are named `system_logs_yYYYYmMMdDD`.
- **Spectre.Console**: Always use `Markup.Escape()` for variable data to prevent parsing crashes.
- **ASCII Logo**: Implemented with explicit `[string]` casting for robust placeholder replacement.
- **Rule Alignment**: Use `Rule.Justification = Justify.Left` in Spectre.Console 0.55.0.
