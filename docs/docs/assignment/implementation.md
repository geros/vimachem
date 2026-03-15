# Implementation Details

This page describes how the assignment requirements were implemented.

## Architecture Implementation

### Service Boundaries

Four microservices were created with clear domain ownership:

| Service | Domain | Database | Dependencies |
|---------|--------|----------|--------------|
| Party.API | People and roles | PostgreSQL (party_db) | None |
| Catalog.API | Books and categories | PostgreSQL (catalog_db) | Party.API |
| Lending.API | Borrow/return workflows | PostgreSQL (lending_db) | Party.API, Catalog.API |
| Audit.API | Event store | MongoDB (library_audit) | None (event consumer) |

### Communication Patterns

**Synchronous (HTTP)**:
- Lending.API → Party.API: Validate customer
- Lending.API → Catalog.API: Check availability, reserve/release
- Catalog.API → Party.API: Validate author

**Asynchronous (Events)**:
- All services publish events to RabbitMQ
- Audit.API consumes all events with wildcard binding

## Feature Implementation

### Party Management

**Implementation**: `backend/Party.API/`

```csharp
// Domain entity with encapsulated behavior
public class Party {
    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public string Email { get; private set; }
    private List<Role> _roles = new();
    public IReadOnlyCollection<Role> Roles => _roles.AsReadOnly();

    public void AssignRole(RoleType type) {
        if (_roles.Any(r => r.Type == type))
            throw new DomainException($"Party already has role '{type}'");
        _roles.Add(new Role(type));
    }
}
```

**Key Features**:
- Domain-driven entity with behavior
- FluentValidation for input validation
- Unique email constraint at database level
- Events published: PartyCreated, PartyUpdated, RoleAssigned, RoleRemoved

### Catalog Management

**Implementation**: `backend/Catalog.API/`

**Key Features**:
- Author validation via HTTP call to Party.API
- ISBN uniqueness constraint
- Denormalized AuthorName for read performance
- Reserve/Release endpoints for Lending.API integration

```csharp
// Service layer with external validation
public async Task<BookResponse> CreateAsync(CreateBookRequest request) {
    // Validate author exists with Author role
    var author = await _partyServiceClient.GetPartyAsync(request.AuthorId);
    if (!author.Roles.Contains("Author"))
        throw new ValidationException("Party does not have Author role");

    var book = new Book(
        request.Title,
        request.Isbn,
        request.AuthorId,
        author.Name,  // Denormalized
        request.CategoryId,
        request.TotalCopies
    );

    await _repository.AddAsync(book);
    await _dbContext.SaveChangesAsync();

    await _eventPublisher.PublishAsync(new BookCreatedEvent { ... }, "book.created");

    return _mapper.Map<BookResponse>(book);
}
```

### Lending Operations

**Implementation**: `backend/Lending.API/`

**Borrow Flow**:

```csharp
public async Task<BorrowingResponse> BorrowAsync(BorrowBookRequest request) {
    // 1. Validate customer
    var customer = await _partyServiceClient.GetPartyAsync(request.CustomerId);
    if (!customer.Roles.Contains("Customer"))
        throw new ValidationException("Customer role required");

    // 2. Check availability
    var availability = await _catalogServiceClient.GetAvailabilityAsync(request.BookId);
    if (!availability.IsAvailable)
        throw new ValidationException("No copies available");

    // 3. Reserve copy
    await _catalogServiceClient.ReserveAsync(request.BookId);

    try {
        // 4. Create borrowing
        var borrowing = new Borrowing(
            request.BookId,
            availability.Title,
            request.CustomerId,
            customer.Name
        );

        await _repository.AddAsync(borrowing);
        await _dbContext.SaveChangesAsync();

        // 5. Publish event
        await _eventPublisher.PublishAsync(new BookBorrowedEvent { ... }, "borrowing.borrowed");

        return _mapper.Map<BorrowingResponse>(borrowing);
    }
    catch {
        // Compensating transaction
        await _catalogServiceClient.ReleaseAsync(request.BookId);
        throw;
    }
}
```

**Key Features**:
- Orchestrates multi-service workflow
- Denormalized book title and customer name
- Compensating transaction on failure
- Polly retry and circuit breaker for HTTP calls

### Audit Trail

**Implementation**: `backend/Audit.API/`

**Event Consumer**:

```csharp
public class RabbitMqEventConsumer : BackgroundService {
    protected override async Task ExecuteAsync(CancellationToken ct) {
        var factory = new ConnectionFactory { HostName = _config.Host };
        using var connection = await factory.CreateConnectionAsync();
        using var channel = await connection.CreateChannelAsync();

        await channel.ExchangeDeclareAsync("library.events", ExchangeType.Topic);
        var queue = await channel.QueueDeclareAsync("audit.queue");

        // Bind to ALL events
        await channel.QueueBindAsync(queue.QueueName, "library.events", "#");

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (model, ea) => {
            var @event = Deserialize(ea.Body.ToArray());
            await _repository.StoreAsync(@event);
            await channel.BasicAckAsync(ea.DeliveryTag, false);
        };

        await channel.BasicConsumeAsync(queue.QueueName, false, consumer);
        await Task.Delay(Timeout.Infinite, ct);
    }
}
```

**Key Features**:
- MongoDB TTL index for automatic expiration
- Background cleanup job for guaranteed removal
- Filterable queries with pagination
- Wildcard event binding

## Design Decisions

### 1. Why Denormalized Data?

**Decision**: Store `AuthorName`, `BookTitle`, and `CustomerName` in multiple services.

**Rationale**:
- Avoids cross-service joins for read operations
- Lending summary is a simple local query
- Names rarely change in practice

**Trade-off**: Data can become stale. Mitigated by:
- Audit trail captures name at time of action
- Events could update denormalized data in future

### 2. Why Stored AvailableCopies?

**Decision**: Store `AvailableCopies` in Catalog database instead of calculating.

**Rationale**:
- Borrowings are in a separate database
- Calculating requires cross-service call on every read
- Local query is faster and simpler

**Trade-off**: Risk of inconsistency. Mitigated by:
- Saga pattern could be added in production
- Compensating transactions on failure

### 3. Why HTTP for Orchestration?

**Decision**: Lending.API makes synchronous HTTP calls to Party.API and Catalog.API.

**Rationale**:
- Provides immediate consistency
- Simpler to implement and reason about
- Fits assignment scope

**Trade-off**: Temporal coupling. Mitigated by:
- Polly retry and circuit breaker
- Services have health checks
- Could evolve to saga pattern later

### 4. Why MongoDB for Events?

**Decision**: Use MongoDB for audit event storage.

**Rationale**:
- Schema flexibility for different event types
- Built-in TTL for data retention
- Horizontal scaling for high write volume
- Fits write-heavy audit workload

## Testing Strategy

### Unit Tests

Each service has comprehensive unit tests:

```
tests/
├── Party.API.Tests/
│   ├── Domain/          # Entity behavior tests
│   ├── Application/     # Service logic tests
│   └── Controllers/     # API endpoint tests
├── Catalog.API.Tests/
├── Lending.API.Tests/
└── Audit.API.Tests/
```

**Example Unit Test**:

```csharp
[Fact]
public void AssignRole_WhenRoleAlreadyAssigned_ThrowsException() {
    // Arrange
    var party = new Party("John Doe", "john@example.com");
    party.AssignRole(RoleType.Author);

    // Act & Assert
    Assert.Throws<DomainException>(() => party.AssignRole(RoleType.Author));
}
```

### Integration Tests

- Database integration with EF Core InMemory provider
- HTTP client integration with WireMock
- RabbitMQ integration with Testcontainers (optional)

### Test Coverage

| Service | Unit Tests | Integration Tests | Coverage |
|---------|------------|-------------------|----------|
| Party.API | 50+ | 10+ | ~85% |
| Catalog.API | 40+ | 10+ | ~80% |
| Lending.API | 40+ | 10+ | ~80% |
| Audit.API | 60+ | 10+ | ~85% |

## Infrastructure

### Docker Compose

```yaml
# Infrastructure services
postgres:    # PostgreSQL 16 with 3 databases
mongo:       # MongoDB 7 for events
rabbitmq:    # RabbitMQ 3 with management UI

# Backend services
party-api:   # Port 5100
catalog-api: # Port 5200
lending-api: # Port 5300
audit-api:   # Port 5400

# Frontend
frontend:    # Port 4200
```

### Resilience Configuration

```csharp
// Polly policies for HTTP clients
services.AddHttpClient<IPartyServiceClient, PartyServiceClient>()
    .AddPolicyHandler(Policy
        .Handle<HttpRequestException>()
        .WaitAndRetryAsync(3, retryAttempt =>
            TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))))
    .AddPolicyHandler(Policy
        .Handle<HttpRequestException>()
        .CircuitBreakerAsync(5, TimeSpan.FromSeconds(30)));
```

## Frontend Implementation

A React SPA was built as a bonus feature:

**Stack**:
- React 19
- TypeScript
- Vite
- React Query for data fetching
- React Router for navigation

**Features**:
- Party management UI
- Book catalog browser
- Borrow/return interface
- Event audit viewer

## Lessons Learned

### What Worked Well

1. **Clear service boundaries** made parallel development possible
2. **Events for audit** simplified cross-service tracking
3. **Docker Compose** provided consistent development environment
4. **Polly** handled transient failures gracefully

### Challenges

1. **Data consistency** across services required careful design
2. **Testing async events** was complex
3. **Local development** with multiple services needed good tooling

### Future Improvements

1. Add API Gateway for unified entry point
2. Implement Saga pattern for distributed transactions
3. Add caching layer (Redis)
4. Implement authentication/authorization
5. Add monitoring and observability (Prometheus, Grafana)
