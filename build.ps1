<#
.SYNOPSIS
Helion Master Build Script

.DESCRIPTION
Orchestrates the build process for the C# Edge Gateway and Go Data Engine.
Utilizes the mandated sgkens/phellams-automator:2.8.0 Docker container to ensure environment parity.

.EXAMPLE
.\build.ps1
#>

$ErrorActionPreference = "Stop"

$ContainerImage = "sgkens/phellams-automator:2.8.0"
$WorkspaceRoot = $PSScriptRoot

Write-Host "💠 Starting Helion Build Process..." -ForegroundColor Cyan

# Ensure runtime directory exists
$RuntimeDir = Join-Path $WorkspaceRoot ".runtime"
if (-not (Test-Path $RuntimeDir)) {
    New-Item -ItemType Directory -Path $RuntimeDir | Out-Null
}

Write-Host "`n[1/2] Building C# Edge Gateway..." -ForegroundColor Blue
$HostArgs = @(
    "run", "--rm", 
    "-v", "$WorkspaceRoot`:/app", 
    "-w", "/app/src/Host", 
    $ContainerImage, 
    "dotnet", "build", "Helion.Host.csproj", "-c", "Release"
)

$HostProcess = Start-Process docker -ArgumentList $HostArgs -NoNewWindow -Wait -PassThru
if ($HostProcess.ExitCode -ne 0) {
    Write-Host "🟥 C# Host build failed!" -ForegroundColor Red
    exit $HostProcess.ExitCode
}
Write-Host "🟩 C# Host build complete." -ForegroundColor Green


Write-Host "`n[2/2] Building Go Data Engine..." -ForegroundColor Blue
$EngineArgs = @(
    "run", "--rm", 
    "-v", "$WorkspaceRoot`:/app", 
    "-w", "/app/src/Engine", 
    "-e", "GO111MODULE=on",
    $ContainerImage, 
    "sh", "-c", "go mod tidy && go build -o ../../.runtime/helion-engine main.go"
)

$EngineProcess = Start-Process docker -ArgumentList $EngineArgs -NoNewWindow -Wait -PassThru
if ($EngineProcess.ExitCode -ne 0) {
    Write-Host "🟥 Go Engine build failed!" -ForegroundColor Red
    exit $EngineProcess.ExitCode
}
Write-Host "🟩 Go Engine build complete." -ForegroundColor Green

Write-Host "`n💠 Build Process Completed Successfully." -ForegroundColor Cyan
