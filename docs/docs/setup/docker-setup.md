# Docker Setup

This guide covers running the entire Library Management System using Docker Compose, which is the simplest way to get started.

## Overview

The Docker Compose configuration includes:

- **Infrastructure**: PostgreSQL, MongoDB, RabbitMQ
- **Backend Services**: Party.API, Catalog.API, Lending.API, Audit.API
- **Frontend**: React SPA served via Nginx

## Quick Start

```bash
# Start all services (Linux/macOS)
./scripts/dev.sh up

# Or directly (any OS)
docker compose up -d
```

This builds all service images and starts the complete stack.

## Service Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                     Docker Network                          │
│                                                             │
│  ┌──────────────┐                                          │
│  │   Frontend   │  http://localhost:4200                   │
│  │   (Nginx)    │                                          │
│  └──────┬───────┘                                          │
│         │ HTTP                                               │
│         ▼                                                    │
│  ┌────────────┐  ┌─────────────┐  ┌─────────────┐           │
│  │ Party.API  │  │ Catalog.API │  │ Lending.API │           │
│  │   :8080    │  │   :8080     │  │   :8080     │           │
│  └─────┬──────┘  └──────┬──────┘  └──────┬──────┘           │
│        │                │                │                  │
│        └────────────────┴────────────────┘                  │
│                         │                                   │
│                         ▼                                   │
│  ┌────────────┐  ┌─────────────┐  ┌──────────────────┐     │
│  │ PostgreSQL │  │ PostgreSQL  │  │    PostgreSQL    │     │
│  │  party_db  │  │ catalog_db  │  │   lending_db     │     │
│  └────────────┘  └─────────────┘  └──────────────────┘     │
│                                                             │
│  ┌────────────┐  ┌─────────────────────────────────────┐   │
│  │  MongoDB   │  │              RabbitMQ               │   │
│  │library_audit│  │            :5672 / :15672           │   │
│  └────────────┘  └─────────────────────────────────────┘   │
│                                     ▲                       │
│  ┌────────────┐                     │                       │
│  │ Audit.API  │─────────────────────┘                       │
│  │   :8080    │  (consumes events)                          │
│  └────────────┘                                             │
└─────────────────────────────────────────────────────────────┘
```

## Docker Compose Services

### Infrastructure Services

#### PostgreSQL

```yaml
postgres:
  image: postgres:16-alpine
  environment:
    POSTGRES_USER: postgres
    POSTGRES_PASSWORD: postgres
  ports:
    - "5432:5432"
  volumes:
    - postgres_data:/var/lib/postgresql/data
    - ./scripts/init-databases.sql:/docker-entrypoint-initdb.d/init-databases.sql
```

- Three databases created automatically: `party_db`, `catalog_db`, `lending_db`
- Data persisted in named volume `postgres_data`
- Health check ensures readiness before dependent services start

#### MongoDB

```yaml
mongo:
  image: mongo:7
  ports:
    - "27017:27017"
  volumes:
    - mongo_data:/data/db
```

- Used by Audit.API for event storage
- Database name: `library_audit`

#### RabbitMQ

```yaml
rabbitmq:
  image: rabbitmq:3-management-alpine
  ports:
    - "5672:5672"    # AMQP protocol
    - "15672:15672"  # Management UI
  environment:
    RABBITMQ_DEFAULT_USER: guest
    RABBITMQ_DEFAULT_PASS: guest
```

- Management UI: http://localhost:15672 (guest/guest)
- Exchange: `library.events`

### Backend Services

Each backend service follows a similar pattern:

```yaml
party-api:
  build:
    context: .
    dockerfile: backend/Party.API/Dockerfile
  ports:
    - "5100:8080"
  environment:
    - ASPNETCORE_ENVIRONMENT=Development
    - ConnectionStrings__DefaultConnection=Host=postgres;Database=party_db;...
    - RabbitMQ__Host=rabbitmq
  depends_on:
    postgres:
      condition: service_healthy
    rabbitmq:
      condition: service_healthy
```

Service startup order:
1. **Tier 1**: Party.API, Audit.API (no API dependencies)
2. **Tier 2**: Catalog.API (depends on Party.API)
3. **Tier 3**: Lending.API (depends on Party.API and Catalog.API)

### Frontend

```yaml
frontend:
  build:
    context: ./frontend
    dockerfile: Dockerfile
  ports:
    - "4200:80"
```

- Built as a static SPA and served by Nginx
- Communicates with backend APIs via HTTP

## Useful Commands

### Start Services

> **Note:** `./scripts/dev.sh` commands require Linux or macOS. On Windows, use the `docker compose` equivalents shown below.

```bash
# Start all services
./scripts/dev.sh up

# Start only infrastructure
./scripts/dev.sh infra

# Start specific service
docker compose up -d party-api
```

### View Logs

```bash
# All services
./scripts/dev.sh logs

# Specific service
./scripts/dev.sh logs party-api

# Last 100 lines
docker compose logs --tail=100
```

### Stop Services

```bash
# Stop all services (keep volumes)
./scripts/dev.sh down

# Stop and remove volumes (data loss!)
./scripts/dev.sh clean
```

### Rebuild Services

```bash
# Rebuild and restart specific service
./scripts/dev.sh rebuild catalog-api

# Rebuild all
./scripts/dev.sh rebuild
```

### Execute Commands

```bash
# Run database migrations
docker compose exec party-api dotnet ef database update

# Access PostgreSQL
docker compose exec postgres psql -U postgres -d party_db

# Access MongoDB
docker compose exec mongo mongosh library_audit

# Access RabbitMQ CLI
docker compose exec rabbitmq rabbitmqctl status
```

## Environment Variables

### Backend Services

| Variable | Description | Default |
|----------|-------------|---------|
| `ASPNETCORE_ENVIRONMENT` | Environment name | Development |
| `ConnectionStrings__DefaultConnection` | PostgreSQL connection string | - |
| `MongoDB__ConnectionString` | MongoDB connection string | - |
| `MongoDB__DatabaseName` | MongoDB database name | library_audit |
| `RabbitMQ__Host` | RabbitMQ hostname | rabbitmq |
| `RabbitMQ__Port` | RabbitMQ port | 5672 |
| `RabbitMQ__Username` | RabbitMQ username | guest |
| `RabbitMQ__Password` | RabbitMQ password | guest |
| `Services__PartyApi` | Party.API URL (for inter-service calls) | http://party-api:8080 |
| `Services__CatalogApi` | Catalog.API URL (for inter-service calls) | http://catalog-api:8080 |

### Frontend

| Variable | Description |
|----------|-------------|
| `VITE_API_PARTY_URL` | Party.API base URL |
| `VITE_API_CATALOG_URL` | Catalog.API base URL |
| `VITE_API_LENDING_URL` | Lending.API base URL |
| `VITE_API_AUDIT_URL` | Audit.API base URL |

## Data Persistence

### Named Volumes

| Volume | Service | Path |
|--------|---------|------|
| `postgres_data` | PostgreSQL | /var/lib/postgresql/data |
| `mongo_data` | MongoDB | /data/db |

Data persists across container restarts. To reset:

```bash
docker compose down -v
```

### Database Initialization

The `scripts/init-databases.sql` file is executed on first PostgreSQL startup:

```sql
CREATE DATABASE party_db;
CREATE DATABASE catalog_db;
CREATE DATABASE lending_db;
```

## Health Checks

All services include health checks:

- **PostgreSQL**: `pg_isready` command
- **MongoDB**: `db.adminCommand('ping')`
- **RabbitMQ**: `rabbitmq-diagnostics check_port_connectivity`

Services with `depends_on` conditions wait for healthy status before starting.

## Troubleshooting

### Services Won't Start

Check service logs:

```bash
docker compose logs --tail=50 service-name
```

Common issues:
- Port conflicts: Ensure ports 4200, 5100-5400, 5432, 5672, 15672, 27017 are available
- Resource limits: Docker Desktop may need more memory allocated

### Database Connection Failures

```bash
# Check PostgreSQL health
docker compose ps postgres

# Verify databases exist
docker compose exec postgres psql -U postgres -l
```

### RabbitMQ Connection Issues

```bash
# Check RabbitMQ status
docker compose logs rabbitmq
curl -u guest:guest http://localhost:15672/api/overview
```

### Rebuild After Code Changes

```bash
docker compose up -d --build service-name
```

## Production Considerations

For production deployment:

1. **Use secrets management** for database passwords and credentials
2. **Enable HTTPS** with proper certificates
3. **Configure resource limits** for containers
4. **Use external monitoring** (Prometheus, Grafana)
5. **Set up log aggregation** (ELK stack, Fluentd)
6. **Use a reverse proxy** (Traefik, Nginx) for load balancing

## Next Steps

- Learn about the [Architecture](../architecture/index.md)
- Explore the [API Documentation](../api/index.md)
