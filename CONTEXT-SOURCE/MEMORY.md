# HELION MEMORY

## Environment Patterns
- **Container**: `sgkens/phellams-automator:2.8.0` is the primary build environment.
- **C#**: .NET 10.0.103.
- **Go**: Version downgraded to 1.19 in `go.mod` to match container capabilities.
- **IPC**: Uses Unix Domain Sockets (UDS) by default for high performance.
- **Theming**: Uses `Spectre.Console` for advanced CLI styling and ASCII logo rendering.
- **Workflow**: Adheres to `develop` > `feature-branch` > `main` branching model.

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
- **Auth**: `JwtAuthMiddleware` injects `X-Helion-Tenant-Id` header for downstream routing.
- **Backup**: Recovery uses `restic dump` streamed directly into `psql` to avoid disk I/O bottlenecks.
- **Engine**: REST filters use dot notation (e.g., `?age=gt.18`).
- **Config**: Environment variables `HELION_INFRA_CONNECTION_STRING`, `HELION_SECURITY_JWT_TTL`, and `HELION_SECURITY_ACME_ENABLED` override YAML.
- **Locks**: HA ACME synchronization uses `pg_try_advisory_lock(42001)`.
- **Maintenance**: `MaintenanceMiddleware` returns `503` with `Retry-After: 5` for locked tenants.
- **RBAC**: Stateless bitwise flag validation via `helion_perms` claim.
- **SSE**: Tenant-isolated broadcasting via `SSEDemux` maps.
