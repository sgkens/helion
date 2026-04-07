# Helion Hosting & Deployment Guide

This guide covers deploying the Helion Zero-Edge Stack in production environments utilizing **Docker** and **Kubernetes**.

## 1. Containerization (Docker)

Helion operates as a multi-binary system enclosed within a single, lightweight Alpine container. 

### Building the Image
Use the provided `docker/Dockerfile` to construct the production image. The multi-stage build will automatically compile both the Go Data Engine and the C# Edge Gateway.

```bash
docker build -f docker/Dockerfile -t registry.gitlab.com/phellams/helion:latest .
```

### Running Locally
To test the built image, run it with port mappings and pass your configuration overrides via environment variables.

```bash
docker run -d \
  -p 5000:5000 \
  -e HELION_INFRA_CONNECTION_STRING="Host=host.docker.internal;Database=helion_system;Username=postgres;Password=your_password" \
  registry.gitlab.com/phellams/helion:latest
```

## 2. Orchestration (Kubernetes)

Helion's stateless edge architecture makes it highly scalable in Kubernetes. All persistent state is managed by your PostgreSQL cluster, and SQLite instances are dynamically loaded as tenant requests are routed to the engine.

### Deployment Manifests
The `kubernetes/` folder contains the baseline manifests for deploying Helion.

1. **Configure Secrets:**
   Create a secret for the system database connection string before applying the deployment.
   ```bash
   kubectl create secret generic helion-secrets --from-literal=system-db-uri="Host=your-pg-cluster;Database=helion_system;Username=postgres;Password=your_password"
   ```

2. **Apply Manifests:**
   Apply the deployment and the internal service.
   ```bash
   kubectl apply -f kubernetes/deployment.yaml
   kubectl apply -f kubernetes/service.yaml
   ```

3. **Ingress:**
   In production, expose the `helion-service` (Port 80) via your preferred Ingress Controller (e.g., NGINX, Traefik). Disable Helion's internal ACME/TLS if your Ingress is handling SSL termination by ensuring `HELION_SECURITY_ACME_ENABLED` is set to `"false"`.

## 3. High Availability (HA) Considerations

* **TLS Provisioning:** If relying on Helion's internal ACME engine in an HA configuration, ensure the PostgreSQL system database is accessible, as Helion utilizes `pg_advisory_lock` to prevent Let's Encrypt rate-limiting collisions during cluster scale-out.
* **Tenant Synchronization:** Tenant SQLite files are currently managed locally by the engine. For multi-node deployments requiring shared tenant data, ensure Helion instances are connected to a shared storage volume (e.g., NFS, EFS) mapped to the expected `.runtime/` tenant directory, or configure the system to prioritize Postgres engines for multi-node deployments.
