# Setup Guide

This guide will help you set up the Library Management System for local development or production deployment.

## Overview

There are two primary ways to run the application:

1. **Docker Compose (Recommended)** - Easiest way to get started. All services run in containers with proper networking.
2. **Local Development** - Run services locally with .NET SDK for active development and debugging.

## Prerequisites

Before you begin, ensure you have the following installed:

- [Docker](https://docs.docker.com/get-docker/) and [Docker Compose](https://docs.docker.com/compose/install/)
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) (for local development)
- [Node.js 20+](https://nodejs.org/) (for frontend development)
- [Git](https://git-scm.com/downloads)

## Quick Start with Docker

The fastest way to get the system running:

```bash
# Start all services
make up

# Or using the script
./scripts/dev.sh up
```

This will start:
- PostgreSQL databases (3 instances)
- MongoDB (Audit event store)
- RabbitMQ (message broker)
- All four backend APIs
- React frontend

## Service URLs

Once running, access the services at:

| Service | URL |
|---------|-----|
| Party.API Swagger | http://localhost:5100/swagger |
| Catalog.API Swagger | http://localhost:5200/swagger |
| Lending.API Swagger | http://localhost:5300/swagger |
| Audit.API Swagger | http://localhost:5400/swagger |
| RabbitMQ Management | http://localhost:15672 (guest/guest) |
| Frontend | http://localhost:4200 |

## Next Steps

- [Prerequisites](prerequisites.md) - Detailed requirements
- [Local Development](local-development.md) - Running services locally
- [Docker Setup](docker-setup.md) - Docker Compose configuration
