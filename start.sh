#!/usr/bin/env bash

# HELION MASTER START SCRIPT
# Starts the Helion Edge Gateway (C#) which automatically spawns the Data Engine (Go).

set -e

WORKSPACE_ROOT=$(pwd)
HOST_DIR="$WORKSPACE_ROOT/src/Host"

echo -e "\e[36m💠 Starting Helion Server...\e[0m"

if [ ! -f "$WORKSPACE_ROOT/.runtime/helion-engine" ]; then
    echo -e "\e[31m🟥 Go Engine binary not found in .runtime/. Please run ./build.sh first.\e[0m"
    exit 1
fi

cd "$HOST_DIR"
echo -e "\n\e[34m🚀 Launching C# Edge Gateway (which will spawn the Go Data Engine)...\e[0m"

# We use dotnet run for local development testing, assuming .NET 10 SDK is installed locally.
# Alternatively, to run isolated via Docker (matching build environment):
# docker run -it --rm -v "$WORKSPACE_ROOT:/app" -w /app/src/Host -p 5000:5000 sgkens/phellams-automator:2.8.0 dotnet run --urls "http://0.0.0.0:5000"

dotnet run
