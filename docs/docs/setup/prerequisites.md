# Prerequisites

This page details all the software requirements for developing and running the Library Management System.

## Required Software

### Docker and Docker Compose

Docker is required for running the infrastructure services (databases, message broker) and can also run the entire application stack.

**Installation:**
- [Docker Desktop](https://www.docker.com/products/docker-desktop) (Windows/Mac) - includes Docker Compose
- [Docker Engine](https://docs.docker.com/engine/install/) (Linux)

**Verify installation:**
```bash
docker --version
docker compose version
```

### .NET 10 SDK

Required for local development, building, and running tests.

**Installation:**
- Download from [.NET 10 Downloads](https://dotnet.microsoft.com/download/dotnet/10.0)
- Or use a package manager:
  ```bash
  # macOS with Homebrew
  brew install dotnet

  # Ubuntu/Debian
  wget https://packages.microsoft.com/config/ubuntu/$(lsb_release -rs)/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
  sudo dpkg -i packages-microsoft-prod.deb
  sudo apt-get update
  sudo apt-get install -y dotnet-sdk-10.0
  ```

**Verify installation:**
```bash
dotnet --version
```

### Node.js 20+

Required for frontend development.

**Installation:**
- Download from [nodejs.org](https://nodejs.org/)
- Or use a version manager:
  ```bash
  # Using nvm
  nvm install 20
  nvm use 20
  ```

**Verify installation:**
```bash
node --version
npm --version
```

### Git

Required for cloning the repository and version control.

**Installation:**
- [git-scm.com](https://git-scm.com/downloads)

**Verify installation:**
```bash
git --version
```

## Optional Tools

### IDE / Editor

Recommended development environments:

- **Visual Studio 2022+** (Windows) - Full-featured IDE
- **Visual Studio Code** (Cross-platform) - Lightweight editor with C# Dev Kit
- **JetBrains Rider** (Cross-platform) - Powerful .NET IDE

**VS Code Extensions:**
- C# Dev Kit
- Docker
- REST Client
- Markdown All in One

### API Testing Tools

- **Swagger UI** - Built into each service at `/swagger`
- **Postman** - For manual API testing
- **curl/httpie** - Command-line HTTP clients

## System Requirements

### Minimum Requirements

- **RAM**: 8 GB
- **Disk**: 10 GB free space
- **CPU**: 4 cores

### Recommended

- **RAM**: 16 GB (for running all services simultaneously)
- **Disk**: 20 GB free space
- **CPU**: 8 cores

## Network Requirements

The following ports must be available on your machine:

| Port | Service |
|------|---------|
| 4200 | Frontend |
| 5100 | Party.API |
| 5200 | Catalog.API |
| 5300 | Lending.API |
| 5400 | Audit.API |
| 5432 | PostgreSQL |
| 5672 | RabbitMQ (AMQP) |
| 15672 | RabbitMQ (Management UI) |
| 27017 | MongoDB |

## Verification

Run this checklist to verify your environment:

```bash
# Check Docker
docker run hello-world

# Check .NET
dotnet --version

# Check Node.js
node --version
npm --version

# Check Git
git --version

```

All commands should execute without errors and display version information.
