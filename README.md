# Library Management System

A microservices-based Library Management System built with .NET 10, PostgreSQL, MongoDB, and RabbitMQ.

## Architecture Overview

```
┌───────────────────────────────────────────────────────────────────────────────┐
│                              Docker Compose                                   │
│                                                                               │
│  ┌──────────────┐                                                             │
│  │  Frontend     │  (Angular - not implemented in this phase)                │
│  │  :4200        │                                                            │
│  └──────┬───────┘                                                             │
│         │ HTTP                                                                │
│         ▼                                                                     │
│  ┌────────────┐  ┌─────────────┐  ┌─────────────┐    ┌──────────────────┐     │
│  │ Party.API  │  │ Catalog.API │  │ Lending.API │    │    Audit.API     │     │
│  │ :5100      │  │ :5200       │  │ :5300       │    │    :5400         │     │
│  │ Parties &  │  │ Books &     │  │ Borrowings  │    │ Event Store/     │     │
│  │ Roles      │  │ Categories  │  │             │    │ Query            │     │
│  └─────┬──────┘  └──────┬──────┘  └──┬──────────┘    └────────┬─────────┘     │
│        │                │             │  HTTP calls ▲          │               │
│        │                │             │  to Party & │          │               │
│        │                │             │  Catalog    │          │               │
│        │                │             ├─────────────┘          │               │
│  ┌─────▼──────┐  ┌──────▼──────┐  ┌──▼──────────┐    ┌───────▼──────────┐    │
│  │ PostgreSQL  │  │ PostgreSQL  │  │ PostgreSQL  │    │    MongoDB       │    │
│  │ party_db   │  │ catalog_db  │  │ lending_db  │    │  library_audit   │    │
│  └────────────┘  └─────────────┘  └─────────────┘    └──────────────────┘    │
│                                                                               │
│                         ┌──────────┐                                          │
│    All 4 APIs ─────────▶│ RabbitMQ │─────────▶ Audit.API consumes             │
│    publish events       │ :5672    │                                          │
│                         └──────────┘                                          │
└───────────────────────────────────────────────────────────────────────────────┘
```

## Services

| Service | Port | Database | Description |
|---------|------|----------|-------------|
| **Party.API** | 5100 | PostgreSQL (party_db) | Manages parties (people) and their roles (Author, Customer) |
| **Catalog.API** | 5200 | PostgreSQL (catalog_db) | Manages books and categories; validates authors via Party.API |
| **Lending.API** | 5300 | PostgreSQL (lending_db) | Orchestrates borrow/return flows; calls Party.API and Catalog.API |
| **Audit.API** | 5400 | MongoDB (library_audit) | Event store and query service; consumes all events from RabbitMQ |

## Technology Stack

- **.NET 10** - Web APIs
- **PostgreSQL 16** - Relational databases (Party, Catalog, Lending)
- **MongoDB 7** - Event store (Audit)
- **RabbitMQ 3** - Message broker for event-driven communication
- **EF Core** - ORM with code-first migrations
- **FluentValidation** - Input validation
- **Polly** - Resilience patterns (retry, circuit breaker)
- **xUnit** - Testing framework

## Quick Start

### Prerequisites

- Docker and Docker Compose
- .NET 10 SDK (for local development)

### Run with Docker Compose

```bash
# Start all services
make up

# Or using the dev script
./scripts/dev.sh up
```

Services will be available at:
- Party.API: http://localhost:5100/swagger
- Catalog.API: http://localhost:5200/swagger
- Lending.API: http://localhost:5300/swagger
- Audit.API: http://localhost:5400/swagger
- RabbitMQ Management: http://localhost:15672 (guest/guest)

### Run Tests

```bash
# Run all unit tests
make test

# Or individually
dotnet test tests/Party.API.Tests/
dotnet test tests/Catalog.API.Tests/
dotnet test tests/Lending.API.Tests/
dotnet test tests/Audit.API.Tests/
```

### Smoke & E2E Tests

```bash
# Quick health check
make smoke

# Full end-to-end borrow/return flow
make e2e
```

## Development Commands

```bash
# Start infrastructure only (postgres, mongo, rabbitmq)
make infra

# View logs
make logs                    # All services
make logs SVC=party-api      # Specific service

# Rebuild a service
make rebuild SVC=lending-api

# Clean everything (including volumes)
make clean
```

## API Endpoints

### Party.API (Port 5100)

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | /api/parties | List all parties |
| GET | /api/parties/{id} | Get party by ID |
| POST | /api/parties | Create party |
| PUT | /api/parties/{id} | Update party |
| DELETE | /api/parties/{id} | Delete party |
| POST | /api/parties/{id}/roles | Assign role |
| DELETE | /api/parties/{id}/roles/{roleType} | Remove role |

### Catalog.API (Port 5200)

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | /api/catalog/books | List books |
| GET | /api/catalog/books/{id} | Get book |
| GET | /api/catalog/books/search?title={title} | Search by title |
| GET | /api/catalog/books/{id}/availability | Check availability |
| POST | /api/catalog/books | Create book |
| PUT | /api/catalog/books/{id} | Update book |
| PUT | /api/catalog/books/{id}/reserve | Reserve copy (internal) |
| PUT | /api/catalog/books/{id}/release | Release copy (internal) |
| GET | /api/catalog/categories | List categories |

### Lending.API (Port 5300)

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | /api/lending/borrow | Borrow a book |
| POST | /api/lending/{bookId}/return | Return a book |
| GET | /api/lending/summary | Borrowed books summary |
| GET | /api/lending/by-customer/{customerId} | Customer borrowings |
| GET | /api/lending/by-book/{bookId} | Book borrowings |

### Audit.API (Port 5400)

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | /api/events/parties/{partyId} | Events for party (paginated) |
| GET | /api/events/books/{bookId} | Events for book (paginated) |

## Key Design Decisions

### 1. Four Microservices with Database-per-Service

**Decision:** Split into Party, Catalog, Lending, and Audit — each with its own database.

**Rationale:** Clear bounded contexts with strict domain ownership. Each service can be deployed, scaled, and evolved independently.

**Trade-off:** More infrastructure complexity (3 PostgreSQL databases + MongoDB + RabbitMQ). More configuration and docker-compose services. But demonstrates genuine microservice architecture.

### 2. Lending.API as Orchestrator

**Decision:** Lending.API orchestrates the borrow/return flow by making synchronous HTTP calls to Party.API and Catalog.API.

**Rationale:** The borrow flow requires data from all three domains: validate customer role (Party), check availability (Catalog), create borrowing record (Lending). Direct orchestration provides correctness and simplicity for this assignment.

**Trade-off:** Temporal coupling — if Party.API or Catalog.API is down, borrowing fails. In production, you'd add circuit breakers (included via Polly), fallback caching, or a saga with compensating transactions.

### 3. Denormalized Data in Lending and Catalog

**Decision:** Store `BookTitle` and `CustomerName` in the Borrowing record; store `AuthorName` in the Book record.

**Rationale:** The "Borrowing Visibility" requirement needs book titles + customer names. Without denormalization, every query requires cross-service calls. By copying names at write time, the summary endpoint is a simple local query.

**Trade-off:** Data can become stale if a party's name changes. Acceptable for a library system — names rarely change, and the audit trail captures the name at the time of the action.

### 4. AvailableCopies as Stored Field

**Decision:** Catalog.API stores `AvailableCopies` and exposes reserve/release endpoints.

**Rationale:** In a monolith, we could compute `TotalCopies - ActiveBorrowings.Count`. With borrowings in a separate database, computing requires a cross-service call on every read. Storing it means book listings and availability checks are fast local queries.

**Trade-off:** Risk of inconsistency if reserve succeeds but Lending.API save fails. Documented as a known limitation. In production, would use the Saga pattern with compensating transactions.

### 5. Resilience with Polly

**Decision:** All inter-service HTTP calls use Polly retry (3 attempts, exponential backoff) + circuit breaker (trips after 5 failures, 30s recovery).

**Rationale:** Microservices fail independently. Polly prevents cascading failures and handles transient network issues.

### 6. MongoDB TTL Index + Background Job for Data Retention

**Decision:** Dual approach — MongoDB TTL index for auto-delete AND explicit BackgroundService cleanup.

**Rationale:** Belt-and-suspenders. TTL index checks every ~60 seconds. Background job runs daily for guaranteed coverage.

## Project Structure

```
src/
├── Party.API/
│   ├── Domain/              # Entities, Exceptions
│   ├── Application/         # DTOs, Services, Validators
│   ├── Infrastructure/      # DbContext, Messaging
│   └── Controllers/
├── Catalog.API/
│   ├── Domain/
│   ├── Application/
│   ├── Infrastructure/
│   ├── HttpClients/         # PartyServiceClient
│   └── Controllers/
├── Lending.API/
│   ├── Domain/
│   ├── Application/
│   ├── HttpClients/         # PartyServiceClient, CatalogServiceClient
│   ├── Infrastructure/
│   └── Controllers/
├── Audit.API/
│   ├── Domain/
│   ├── Application/
│   ├── Infrastructure/      # EventRepository, RabbitMqEventConsumer, EventRetentionJob
│   └── Controllers/
└── Shared/
    └── Events/              # IntegrationEvent, IEventPublisher

tests/
├── Party.API.Tests/
├── Catalog.API.Tests/
├── Lending.API.Tests/
└── Audit.API.Tests/
```

## Coding Conventions

See [CLAUDE.md](CLAUDE.md) for full coding standards:
- Same-line braces (K&R style)
- Tabs for indentation (width 4)
- File-scoped namespaces
- Conventional commits with scope

## Event Types

All services publish events to RabbitMQ (`library.events` exchange):

| Service | Event | Routing Key |
|---------|-------|-------------|
| Party.API | PartyCreated | party.created |
| Party.API | PartyUpdated | party.updated |
| Party.API | RoleAssigned | party.role_assigned |
| Catalog.API | BookCreated | book.created |
| Catalog.API | BookUpdated | book.updated |
| Lending.API | BookBorrowed | borrowing.borrowed |
| Lending.API | BookReturned | borrowing.returned |

Audit.API consumes all events with `#` wildcard binding.

## License

This is a Vimachem interview assignment.
