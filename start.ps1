<#
.SYNOPSIS
Helion Master Start Script

.DESCRIPTION
Starts the Helion Edge Gateway (C#) which automatically spawns the Data Engine (Go).
If the project hasn't been built yet, please run build.ps1 first.

.EXAMPLE
.\start.ps1
#>

$ErrorActionPreference = "Stop"

$WorkspaceRoot = $PSScriptRoot
$HostDir = Join-Path $WorkspaceRoot "src/Host"

Write-Host "💠 Starting Helion Server..." -ForegroundColor Cyan

if (-not (Test-Path (Join-Path $WorkspaceRoot ".runtime/helion-engine"))) {
    Write-Host "🟥 Go Engine binary not found in .runtime/. Please run build.ps1 first." -ForegroundColor Red
    exit 1
}

Set-Location $HostDir
Write-Host "🚀 Launching C# Edge Gateway (which will spawn the Go Data Engine)..." -ForegroundColor Blue

# We use dotnet run for local development testing, assuming .NET 10 SDK is installed locally.
# If strict container execution is needed for running, we can wrap this in docker similar to build.ps1
dotnet run
