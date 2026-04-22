# HELION: Master Product Requirements Document
**Version:** 0.3.0 (Beta Release - Core Expansion Complete)
**Date:** Current
**Classification:** Enterprise Implementation Blueprint & Autonomous Agent Directives

---

## 1. Executive Summary

**Project Name:** Helion
**Objective:** Develop a hyper-performant, zero-configuration Backend-as-a-Service (BaaS), hybrid infrastructure control plane, and commercial SaaS foundation.
**Target Deployment:** Bare metal, VPS, or localized developer environments (Linux, macOS, Windows).
**Core Philosophy:** The "Zero-Edge" stack. Helion operates as a multi-binary system delivering instant REST/GraphQL APIs, Server-Sent Events (SSE) real-time subscriptions, stateless JWT authentication, automated TLS, and dynamic container orchestration without the overhead of heavy runtimes.

---

## 2. AI Execution Directives (Gemini CLI)

This document is formatted strictly for automated parsing and implementation by the Gemini CLI agent. 

■ **Zero-Edge Mandate:** Rely exclusively on C# .NET 10 (Single File Deployment) and Go 1.19+ (calibrated for v4.1.0). 
■ **Foundation First:** Do not implement UI or higher-level features until the C# edge proxy, Go data engine, IPC over Unix Domain Sockets (UDS), and PostgreSQL state manager are successfully passing internal communication tests.
■ **Code Generation Rules:** Output complete file contents. Do not truncate classes. Ensure strict type declarations.
■ **The 20-Pass Constraints:** You MUST adhere to all constraints listed in Section 3 during implementation.

---

## 3. The 20-Pass Adversarial Constraints

### 3.1. Technical Logic Constraints
1.  **Compiler Constraint:** `Helion.Host` MUST use `<PublishSingleFile>true</PublishSingleFile>` and `<PublishTrimmed>true</PublishTrimmed>`.
2.  **UDS IPC Security:** C# and Go MUST communicate over Unix Domain Sockets (`.sock`) with an ephemeral IPC Token.
3.  **Boot Race Conditions:** C# `ProcessManager` MUST ping `/health` or wait for `ready` signal before Kestrel binds.
4.  **CORS & Body Limits:** Enforce strict `5MB` `MaxRequestBodySize` and implement permissive CORS.
5.  **Tenant Resolution:** Go engine MUST parse `X-Helion-Tenant-Id` for SQLite routing.
6.  **DDL Sanitization:** Strictly validate all identifiers using `^[a-zA-Z0-9_]+$`.
7.  **Migration Locks:** Return `503 Service Unavailable` with `Retry-After: 5` during SQLite -> Postgres upgrades.
8.  **SSE Threading:** Use single pub/sub demultiplexer goroutine for `LISTEN helion_events`.
9.  **Telemetry Pruning:** Background task MUST delete logs > 14 days and drop partitions > 30 days.
10. **Atomic Rate Limiting:** Use `Interlocked` operations for lock-free math in `TokenBucket`.
11. **Restic Pipe Timeouts:** Wrap `pg_dump | restic` in a 30-minute `CancellationToken`.
12. **Container State Recovery:** Issue `docker start` for registered tenant containers on boot.
13. **Sandboxed Automation:** Lua scripts restricted to 10,000 instructions via `lua_sethook`.
14. **HA Cert Locks:** Use Postgres `pg_advisory_lock` for ACME TLS provisioning.
15. **SPA Routing:** Implement `app.MapFallbackToFile("index.html")`.
16. **Config Interpolation:** Prioritize OS Environment Variables over `helion.yml`.
17. **Orphan Socket Cleanup:** Glob and delete stale `helion_*.sock` files on boot.
18. **Strict UTC:** All timestamps MUST utilize `DateTime.UtcNow` or Unix epoch.
19. **Auto-Bootstrap:** Orchestrate local Postgres if `helion_system_db` is unreachable.
20. **LRU Connection Caching:** Limit active SQLite connections to 500 most recent tenants.

### 3.2. Environmental & Stylistic Constraints (v4.1.0)
21. **Go Version Parity:** Go code MUST be compatible with **Go 1.19** (Container Limitation).
22. **Database Driver Parity:** Use **pgx/v4** for Go 1.19 compatibility.
23. **Theming:** Use a centralized `HelionPalette` object and a `UI` utility class for all CLI output.
24. **Markup Integrity:** Always use `Markup.Escape()` for variable data in Spectre.Console.
25. **ASCII Logo:** Implement robust ASCII logo rendering on boot.

---

## 4. System Architecture

■ **The Edge Gateway (C# .NET 10):** Kestrel Edge, Spectre.Console UI, UDS Reverse Proxy, PowerShell Hooks.
■ **The Data Engine (Go 1.19):** UDS Server, LRU Connection Pooler, REST-to-SQL, SSE Demultiplexer.
■ **The Backup Subsystem (Go - Restic):** Zero-disk streaming backups via `pg_dump | restic`.
■ **The Control Plane State (PostgreSQL):** handles RBAC, partitioned logs, and cluster locks.
