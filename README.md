# 💠 HELION

**Hyper-performant, zero-configuration Backend-as-a-Service (BaaS) and Hybrid Control Plane.**

Helion is a "Zero-Edge" stack designed for modern application foundations. It combines the orchestration power of .NET 10 with the high-concurrency data processing of Go 1.19, delivering a multi-binary system that handles everything from instant REST/GraphQL APIs to real-time SSE broadcasting and encrypted streaming backups.

---

## 📋 Table of Contents
- [Core Features](#-core-features)
- [Tech Stack](#-tech-stack)
- [Getting Started](#-getting-started)
- [Documentation & Governance](#-documentation--governance)
- [Chores & Maintenance](#-chores--maintenance)

---

## 🟦 Core Features

- **Zero-Edge Architecture:** Multi-binary system using C# .NET 10 Edge Gateway and Go 1.19 Data Engine.
- **High-Performance IPC:** Communication via Unix Domain Sockets (UDS) for near zero-copy memory transfers.
- **Stateless RBAC:** Bitwise flag validation embedded in JWT claims for instant, database-free permission checks.
- **Real-time SSE:** Tenant-isolated event broadcasting powered by PostgreSQL `LISTEN/NOTIFY`.
- **Encrypted Backups:** Zero-disk overhead streaming backups via `pg_dump | restic` with PGP-equivalent encryption.
- **Auto-Bootstrapping:** Automated infrastructure provisioning via Docker for "zero-config" local development.

---

## 🛠 Tech Stack

- **Edge Gateway:** C# .NET 10 (Single File, Trimmed)
- **Data Engine:** Go 1.19 (Optimized for UDS)
- **Telemetry:** Tripartite sink (Console, Flat File, Partitioned PostgreSQL)
- **Storage:** Managed SQLite (Edge) or PostgreSQL (Cloud/HA)
- **Security:** Stateless JWT, Ephemeral IPC Tokens, Postgres Advisory Locks

---

## 🚀 Getting Started

### Prerequisites
- Docker (for auto-bootstrapping system dependencies)
- .NET 10 SDK
- Go 1.19+

### Quick Start
1. Clone the repository.
2. Build the Go Data Engine:
   ```bash
   cd src/Engine && go build -o ../../.runtime/helion-engine main.go
   ```
3. Run the C# Edge Gateway:
   ```bash
   cd src/Host && dotnet run
   ```

---

## 📖 Documentation & Governance

Helion follows a strict **20-Pass Adversarial Review** process. All architectural decisions and lifecycle hardening constraints are documented in the [PDR (Product Requirements Document)](./CONTEXT-SOURCE/PDR.md).

- **Current Version:** `0.2.4` (Security & Lifecycle Hardening)
- **Classification:** Enterprise Implementation Blueprint

---

## 🟦 Chores & Maintenance

This repository adheres to the **Conventional Commits** standard and utilizes a dedicated **Rolling PDR** for structural evolution. 

- **Commit Format:** `type(scope): concise message`
- **Symbols:** 🟦 (Feature), 🟩 (Fix), 🟥 (Breaking), 💠 (Brand)

---

Built with 💠 by Garvey Snow
