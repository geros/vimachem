# Local Development Setup

This guide covers setting up the Library Management System for local development, allowing you to run and debug services directly on your machine.

## Overview

For local development, you'll run:
- **Infrastructure services** (PostgreSQL, MongoDB, RabbitMQ) via Docker Compose
- **Backend APIs** locally with `dotnet run`
- **Frontend** with `npm run dev`

This setup provides fast iteration cycles and full debugging capabilities.

## Step 1: Start Infrastructure Services

First, start only the infrastructure services using Docker Compose:

```bash
# Using the helper script (Linux/macOS)
./scripts/dev.sh infra

# Or directly with Docker Compose (any OS)
docker compose up -d postgres mongo rabbitmq
```

This starts:
- PostgreSQL 16 (port 5432)
- MongoDB 7 (port 27017)
- RabbitMQ 3 with management UI (ports 5672, 15672)

Wait for services to be healthy:

```bash
docker compose ps
```

All services should show `healthy` status.

## Step 2: Initialize Databases

The PostgreSQL initialization script automatically creates the required databases on first startup:

- `party_db` - Party.API database
- `catalog_db` - Catalog.API database
- `lending_db` - Lending.API database

## Step 3: Run Backend Services

You can run all services from the solution root or individually.

### Option A: Run All Services

```bash
# From the repository root
dotnet run --project backend/Party.API/Party.API.csproj &
dotnet run --project backend/Catalog.API/Catalog.API.csproj &
dotnet run --project backend/Lending.API/Lending.API.csproj &
dotnet run --project backend/Audit.API/Audit.API.csproj &
```

### Option B: Run Individual Services

Open separate terminal windows for each service:

**Terminal 1 - Party.API:**
```bash
cd backend/Party.API
dotnet run
```

**Terminal 2 - Catalog.API:**
```bash
cd backend/Catalog.API
dotnet run
```

**Terminal 3 - Lending.API:**
```bash
cd backend/Lending.API
dotnet run
```

**Terminal 4 - Audit.API:**
```bash
cd backend/Audit.API
dotnet run
```

### Service URLs

Each service will be available at:

| Service | Local URL | Swagger URL |
|---------|-----------|-------------|
| Party.API | http://localhost:5100 | http://localhost:5100/swagger |
| Catalog.API | http://localhost:5200 | http://localhost:5200/swagger |
| Lending.API | http://localhost:5300 | http://localhost:5300/swagger |
| Audit.API | http://localhost:5400 | http://localhost:5400/swagger |

## Step 4: Run Frontend

The frontend is a React application built with Vite.

```bash
cd frontend

# Install dependencies (first time only)
npm install

# Start development server
npm run dev
```

The frontend will be available at http://localhost:5173 (or another port if 5173 is in use).

## Environment Configuration

### Backend Services

Each service uses `appsettings.Development.json` for local development. The default configuration connects to infrastructure services running in Docker.

Example for Party.API `appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=party_db;Username=postgres;Password=postgres"
  },
  "RabbitMQ": {
    "Host": "localhost",
    "Port": 5672,
    "Username": "guest",
    "Password": "guest"
  }
}
```

### Frontend

The frontend uses environment files for API configuration:

```bash
# frontend/.env.development
VITE_API_PARTY_URL=http://localhost:5100
VITE_API_CATALOG_URL=http://localhost:5200
VITE_API_LENDING_URL=http://localhost:5300
VITE_API_AUDIT_URL=http://localhost:5400
```

## Running Tests

### Unit Tests

```bash
# Run all tests (Linux/macOS)
./scripts/dev.sh test

# Or individually (any OS)
dotnet test tests/Party.API.Tests/
dotnet test tests/Catalog.API.Tests/
dotnet test tests/Lending.API.Tests/
dotnet test tests/Audit.API.Tests/
```

### Smoke Tests

```bash
./scripts/dev.sh smoke
```

## Debugging

### Visual Studio

1. Open `LibraryManagement.slnx` in Visual Studio
2. Set multiple startup projects:
   - Right-click solution → Properties
   - Select "Multiple startup projects"
   - Set Action to "Start" for all four APIs
3. Press F5 to debug

### Visual Studio Code

1. Open the repository root in VS Code
2. Use the Run and Debug panel (Ctrl+Shift+D)
3. Select "Launch All Services" configuration
4. Press F5 to start debugging

### JetBrains Rider

1. Open the solution
2. Create a Compound Run Configuration:
   - Run → Edit Configurations
   - Add new "Compound" configuration
   - Add all four API projects
3. Debug the compound configuration

## Development Workflow

### Making Changes

1. **Backend changes**: Services will hot-reload on file changes (if using `dotnet watch`)
2. **Frontend changes**: Vite provides HMR (Hot Module Replacement)

### Database Migrations

If you modify entity models:

```bash
cd backend/Party.API
dotnet ef migrations add MigrationName
dotnet ef database update
```

### Checking Logs

```bash
# Infrastructure logs
docker compose logs -f postgres
docker compose logs -f rabbitmq

# Or all infrastructure
docker compose logs -f
```

## Troubleshooting

### Port Conflicts

If ports are already in use:

```bash
# Find processes using specific ports
lsof -i :5100
lsof -i :5200
lsof -i :5300
lsof -i :5400
```

### Database Connection Issues

Ensure PostgreSQL is accepting connections:

```bash
docker compose ps postgres
# Should show "healthy"

# Test connection
psql -h localhost -U postgres -d party_db
```

### RabbitMQ Connection Issues

Check RabbitMQ status:

```bash
docker compose logs rabbitmq
curl -u guest:guest http://localhost:15672/api/overview
```

### Reset Everything

To start fresh:

```bash
# Stop everything and remove volumes (Linux/macOS)
./scripts/dev.sh clean

# Or manually (any OS)
docker compose down -v

# Restart infrastructure
./scripts/dev.sh infra
```

## Next Steps

- Review the [Architecture Overview](../architecture/index.md)
- Explore the [API Documentation](../api/index.md)
- Read the [Assignment Requirements](../assignment/index.md)
