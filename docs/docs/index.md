# Library Management System

Welcome to the Library Management System documentation. This is a microservices-based application built with modern .NET technologies, designed to manage library operations including parties (members), book cataloging, and lending workflows.

## Overview

The Library Management System is a distributed application composed of four microservices, each responsible for a specific domain:

- **Party.API** - Manages people and their roles (authors, customers)
- **Catalog.API** - Manages books and categories
- **Lending.API** - Orchestrates book borrowing and returns
- **Audit.API** - Event store for tracking all system activities

## Quick Links

| | |
|:---|:---|
| **[Getting Started](setup/index.md)** | Set up the project locally or with Docker |
| **[Architecture](architecture/index.md)** | Learn about the microservices architecture and design decisions |
| **[API Reference](api/index.md)** | Explore the REST API endpoints for each service |
| **[Assignment](assignment/index.md)** | View the original assignment requirements and implementation details |

## Technology Stack

| Component | Technology |
|-----------|------------|
| Backend | .NET 10 |
| Databases | PostgreSQL 16, MongoDB 7 |
| Message Broker | RabbitMQ 3 |
| Frontend | React 19 + Vite |
| ORM | Entity Framework Core |
| Validation | FluentValidation |
| Resilience | Polly |
| Testing | xUnit |

## Architecture at a Glance

```mermaid
flowchart TB
    subgraph "Docker Compose"
        FE[Frontend :4200]

        subgraph "Backend Services"
            PA[Party.API :5100]
            CA[Catalog.API :5200]
            LA[Lending.API :5300]
            AA[Audit.API :5400]
        end

        subgraph "Databases"
            PG1[(PostgreSQL party_db)]
            PG2[(PostgreSQL catalog_db)]
            PG3[(PostgreSQL lending_db)]
            MG[(MongoDB library_audit)]
        end

        RMQ[RabbitMQ :5672]
    end

    FE -->|HTTP| PA
    FE -->|HTTP| CA
    FE -->|HTTP| LA
    FE -->|HTTP| AA

    PA --> PG1
    CA --> PG2
    LA --> PG3
    AA --> MG

    PA -->|publishes| RMQ
    CA -->|publishes| RMQ
    LA -->|publishes| RMQ
    RMQ -->|consumes| AA
```

## Service Ports

| Service | Port | URL |
|---------|------|-----|
| Party.API | 5100 | http://localhost:5100/swagger |
| Catalog.API | 5200 | http://localhost:5200/swagger |
| Lending.API | 5300 | http://localhost:5300/swagger |
| Audit.API | 5400 | http://localhost:5400/swagger |
| Frontend | 4200 | http://localhost:4200 |
| Documentation | 8000 | http://localhost:8000 |
| RabbitMQ Management | 15672 | http://localhost:15672 |

## Getting Started

The fastest way to get started is using Docker Compose:

```bash
# Clone the repository
git clone <repository-url>
cd library-management

# Start all services (Linux/macOS)
./scripts/dev.sh up

# Or on any OS
docker compose up -d --build
```

For detailed setup instructions, see the [Setup Guide](setup/index.md).

## Development

### Running Tests

```bash
# Run all tests (Linux/macOS)
./scripts/dev.sh test

# Run specific service tests (any OS)
dotnet test tests/Party.API.Tests/
dotnet test tests/Catalog.API.Tests/
dotnet test tests/Lending.API.Tests/
dotnet test tests/Audit.API.Tests/
```

### Smoke & E2E Tests

```bash
# Quick health check (Linux/macOS)
./scripts/dev.sh smoke
```

## License

This is a Vimachem interview assignment.
