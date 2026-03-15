# Assignment Requirements

This page documents the original requirements provided for the Library Management System interview assignment.

## Functional Requirements

### 1. Party Management

The system must manage people (parties) who interact with the library.

#### Requirements:
- Create, read, update, delete parties
- Each party has: name, email
- Assign roles to parties: **Author** and/or **Customer**
- A party can have multiple roles (e.g., both Author and Customer)
- Email addresses must be unique

#### Business Rules:
- Name cannot be empty
- Email must be valid format
- Only parties with Author role can be book authors
- Only parties with Customer role can borrow books

### 2. Catalog Management

The system must manage the library's book catalog.

#### Requirements:
- Create, read, update, delete books
- Each book has: title, ISBN, author, category, total copies
- Track available copies for lending
- Manage book categories
- Search books by title

#### Business Rules:
- ISBN must be unique
- Author must exist in Party system with Author role
- Total copies must be >= 0
- Available copies cannot exceed total copies
- Available copies cannot be negative

### 3. Lending Operations

The system must support book borrowing and returns.

#### Requirements:
- Borrow a book (customer + book)
- Return a borrowed book
- View currently borrowed books
- View borrowing history by customer
- View borrowing history by book

#### Business Rules:
- Customer must exist with Customer role
- Book must have available copies
- Customer cannot borrow same book twice simultaneously
- Due date is 14 days from borrow date
- Return must match active borrowing record

### 4. Audit Trail

The system must maintain an audit trail of all activities.

#### Requirements:
- Record all state changes as events
- Store events for 90 days
- Query events by entity type, action, date range
- View event history for specific party or book

#### Event Types:
- PartyCreated, PartyUpdated
- RoleAssigned, RoleRemoved
- BookCreated, BookUpdated, BookDeleted
- BookBorrowed, BookReturned

## Non-Functional Requirements

### Architecture

- **Microservices**: Four separate services with clear boundaries
- **Database per Service**: Each service owns its data
- **Event-Driven**: Async messaging for cross-service communication
- **API Gateway**: Not required, direct service access acceptable

### Technology Stack

- **Backend**: .NET 8+ (used .NET 10)
- **Databases**: PostgreSQL (relational), MongoDB (events)
- **Message Broker**: RabbitMQ
- **Containerization**: Docker, Docker Compose
- **Testing**: Unit and integration tests

### API Design

- RESTful APIs
- JSON request/response format
- Proper HTTP status codes
- Swagger/OpenAPI documentation
- Consistent error responses

### Data Consistency

- Strong consistency within services (ACID transactions)
- Eventual consistency for cross-service data (via events)
- Denormalized data acceptable for read optimization

### Resilience

- Retry logic for transient failures
- Circuit breaker for external service calls
- Graceful degradation where possible

## Constraints

### Time Constraints

- Assignment to be completed within one week
- Focus on core functionality over edge cases
- Documentation should be concise but complete

### Scope Constraints

- No authentication/authorization required
- No frontend required (but bonus if included)
- No production deployment required
- No CI/CD pipeline required

### Technical Constraints

- Use provided technology stack
- Follow C# coding conventions
- Use Entity Framework Core for PostgreSQL
- Use MongoDB driver for events
- Use RabbitMQ.Client for messaging

## Deliverables

### Code

- Complete source code in Git repository
- Clean commit history
- README with setup instructions
- Docker Compose configuration

### Documentation

- API documentation (Swagger)
- Architecture overview
- Setup instructions
- Design decisions explained

### Tests

- Unit tests for domain logic
- Integration tests for API endpoints
- Test coverage report (optional)

## Bonus Points

Additional features that demonstrate advanced skills:

- [x] Frontend application (React + Vite)
- [x] Comprehensive test coverage
- [x] Pagination for list endpoints
- [x] Event filtering and querying
- [x] Data retention policies
- [x] Health checks
- [x] Resilience patterns (Polly)
- [x] Docker multi-stage builds
- [x] Makefile for common tasks
- [x] Conventional commits

## Evaluation Rubric

| Category | Criteria | Points |
|----------|----------|--------|
| **Functionality** | All requirements implemented | 30 |
| | Correct business logic | 15 |
| **Architecture** | Clean service boundaries | 15 |
| | Proper use of patterns | 10 |
| **Code Quality** | Readable and maintainable | 10 |
| | Proper error handling | 5 |
| **Testing** | Unit tests | 5 |
| | Integration tests | 5 |
| **Documentation** | Clear README | 3 |
| | API documentation | 2 |
| **Bonus** | Additional features | +10 |

**Total**: 100 points (+10 bonus)
