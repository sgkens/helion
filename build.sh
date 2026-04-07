#!/usr/bin/env bash

# HELION MASTER BUILD SCRIPT
# Orchestrates the build process for the C# Edge Gateway and Go Data Engine.
# Utilizes the mandated sgkens/phellams-automator:2.8.0 Docker container to ensure environment parity.

set -e

CONTAINER_IMAGE="sgkens/phellams-automator:2.8.0"
WORKSPACE_ROOT=$(pwd)

echo -e "\e[36m💠 Starting Helion Build Process...\e[0m"

# Ensure runtime directory exists
mkdir -p "$WORKSPACE_ROOT/.runtime"

echo -e "\n\e[34m[1/2] Building C# Edge Gateway...\e[0m"
if docker run --rm -v "$WORKSPACE_ROOT:/app" -w /app/src/Host $CONTAINER_IMAGE dotnet build Helion.Host.csproj -c Release; then
    echo -e "\e[32m🟩 C# Host build complete.\e[0m"
else
    echo -e "\e[31m🟥 C# Host build failed!\e[0m"
    exit 1
fi

echo -e "\n\e[34m[2/2] Building Go Data Engine...\e[0m"
if docker run --rm -v "$WORKSPACE_ROOT:/app" -w /app/src/Engine -e GO111MODULE=on $CONTAINER_IMAGE sh -c "go mod tidy && go build -o ../../.runtime/helion-engine main.go"; then
    echo -e "\e[32m🟩 Go Engine build complete.\e[0m"
else
    echo -e "\e[31m🟥 Go Engine build failed!\e[0m"
    exit 1
fi

echo -e "\n\e[36m💠 Build Process Completed Successfully.\e[0m"
