# Implementation Details

This page maps the assignment requirements to the actual implementation.

## Section 1 - Requirements Checklist

### Parties & Role Management

| Requirement | Implementation |
|-------------|----------------|
| CRUD parties | `Party.API` — full REST endpoints at `/api/parties` |
| CRUD roles (Author, Customer) | Role assignment/removal via `/api/parties/{id}/roles` |
| A party can belong to both roles | `Party` aggregate supports multiple roles with duplicate validation |

### Book & Category Management

| Requirement | Implementation |
|-------------|----------------|
| CRUD books | `Catalog.API` — REST endpoints at `/api/catalog/books` |
| CRUD categories (Fiction, Mystery) | Categories seeded and managed via `/api/catalog/categories` |
| Track availability by ID | `GET /api/catalog/books/{id}/availability` |
| Track availability by Title | `GET /api/catalog/books/search?title={title}` |
| Borrowing and returning | `Lending.API` — `POST /api/lending/borrow` and `POST /api/lending/{bookId}/return` |
| One copy per customer at a time | Duplicate active borrowing check in `BorrowingService` |

### Borrowing Visibility

| Requirement | Implementation |
|-------------|----------------|
| List book titles with current borrowers | `GET /api/lending/summary` — returns active borrowings with denormalized book titles and customer names |

### Technical Requirements

| Requirement | Implementation |
|-------------|----------------|
| Microservice architecture | 4 services: Party.API, Catalog.API, Lending.API, Audit.API |
| Relational database | PostgreSQL 16 (party_db, catalog_db, lending_db) via EF Core |
| Initial data | `DataSeeder` in each service — parties, roles, categories, books |
| Clear domain ownership | Each service owns its bounded context and database |
| Unit tests | xUnit tests in `tests/` for all 4 services |
| Containerized | Dockerfile per service with multi-stage builds |
| docker-compose setup | Single `docker-compose.yml` starts all services and infrastructure |

## Section 2 - Requirements Checklist

### Event Publishing

| Requirement | Implementation |
|-------------|----------------|
| All actions published as events | `RabbitMqEventPublisher` in Party.API, Catalog.API, Lending.API |
| Entity identifiers in events | `IntegrationEvent.EntityId` and `RelatedEntityIds` dictionary |
| Action type in events | `IntegrationEvent.Action` (e.g., "Created", "Borrowed") |
| Timestamp in events | `IntegrationEvent.Timestamp` (UTC) |

### Event History

| Requirement | Implementation |
|-------------|----------------|
| Persist events | `Audit.API` consumes via `RabbitMqEventConsumer` (BackgroundService), stores in MongoDB |
| User-related events endpoint | `GET /api/events/parties/{partyId}` |
| Book-related events endpoint | `GET /api/events/books/{bookId}` |
| Paginated responses | `page` and `pageSize` query parameters with defaults and max limits |

### Data Retention

| Requirement | Implementation |
|-------------|----------------|
| Delete events older than 1 year | `EventRetentionJob` (BackgroundService) runs daily cleanup |
| Cleanup isolated from request handling | Runs as a separate BackgroundService, not in the request pipeline |

### Technical Requirements

| Requirement | Implementation |
|-------------|----------------|
| RabbitMQ | Topic exchange `library.events`, queue `audit.events` with DLX |
| Non-relational database | MongoDB 7 (`library_audit` database) |
| Retry and error-handling | Up to 3 retries with exponential backoff; dead letter queue for poison messages |

## Design Decisions

### 1. Lending.API as Orchestrator

**Decision:** Lending.API makes synchronous HTTP calls to Party.API and Catalog.API for the borrow/return flow.

**Rationale:** The borrow flow requires immediate validation across three domains. Direct orchestration provides correctness and simplicity.

**Trade-off:** Temporal coupling — if upstream services are down, borrowing fails. Mitigated with Polly retry (3 attempts, exponential backoff) and circuit breaker (5 failures, 30s recovery).

### 2. Denormalized Data

**Decision:** Store `AuthorName` in Book, `BookTitle` and `CustomerName` in Borrowing.

**Rationale:** The borrowing summary endpoint needs book titles and customer names. Without denormalization, every query requires cross-service HTTP calls. Copying names at write time makes summaries a simple local query.

**Trade-off:** Data can become stale if names change. Acceptable — names rarely change, and the audit trail captures the name at the time of action.

### 3. Stored AvailableCopies

**Decision:** Catalog.API stores `AvailableCopies` and exposes reserve/release endpoints.

**Rationale:** With borrowings in a separate database, computing availability requires a cross-service call on every read. A stored counter makes availability checks fast.

**Trade-off:** Risk of inconsistency if reservation succeeds but the borrowing save fails. Mitigated with compensating logic — Lending.API calls release if save fails.

### 4. MongoDB for Events

**Decision:** Use MongoDB for the audit event store.

**Rationale:** Schema flexibility for different event types, built-in TTL indexes for retention, and a write-heavy workload that fits MongoDB's strengths.

### 5. Dual Retention Strategy

**Decision:** Both MongoDB TTL index and an explicit BackgroundService for event cleanup.

**Rationale:** TTL index provides automatic deletion but checks only every ~60 seconds. The background job runs daily as a safety net for guaranteed coverage.
