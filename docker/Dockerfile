# Stage 1: Build Go Engine
FROM golang:1.19-alpine AS build-go

WORKDIR /app
COPY src/Engine/go.mod src/Engine/go.sum ./
RUN go mod download

COPY src/Engine/ ./
RUN CGO_ENABLED=0 GOOS=linux go build -ldflags="-w -s" -o helion-engine main.go

# Stage 2: Build C# Host
FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build-dotnet

WORKDIR /app
# Copy the entire workspace to ensure frontend assets and relative paths are preserved
COPY . .
WORKDIR /app/src/Host

# Restore & Publish Trimmed Single File
RUN dotnet publish Helion.Host.csproj -c Release -r linux-musl-x64 --self-contained true /p:PublishSingleFile=true /p:PublishTrimmed=true -o /app/out

# Stage 3: Final Minimal Image
FROM alpine:3.18

# Install dependencies needed by .NET self-contained and our app
RUN apk add --no-cache \
    icu-libs \
    tzdata \
    postgresql-client \
    restic \
    lua5.4

WORKDIR /opt/helion

# Copy Go Engine into .runtime
RUN mkdir -p .runtime
COPY --from=build-go /app/helion-engine .runtime/helion-engine

# Copy C# Host
COPY --from=build-dotnet /app/out/Helion.Host .

# Create symlink for restic in .runtime if needed by ProcessManager logic
RUN ln -s /usr/bin/restic .runtime/restic

ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false
ENV ASPNETCORE_URLS=http://0.0.0.0:5000
EXPOSE 5000

# Execute the C# Host
CMD ["./Helion.Host"]
