# Database Design

The Library Management System uses polyglot persistence - different databases for different service needs.

## Overview

| Service | Database | Purpose |
|---------|----------|---------|
| Party.API | PostgreSQL | Relational data for parties and roles |
| Catalog.API | PostgreSQL | Relational data for books and categories |
| Lending.API | PostgreSQL | Relational data for borrowings |
| Audit.API | MongoDB | Document store for events |

## PostgreSQL

Three separate PostgreSQL databases provide ACID compliance for transactional data.

### Party Database (party_db)

**Tables:**

```sql
CREATE TABLE Parties (
    Id UUID PRIMARY KEY,
    Name VARCHAR(255) NOT NULL,
    Email VARCHAR(255) NOT NULL UNIQUE,
    CreatedAt TIMESTAMP NOT NULL,
    UpdatedAt TIMESTAMP
);

CREATE TABLE Roles (
    Id UUID PRIMARY KEY,
    PartyId UUID NOT NULL REFERENCES Parties(Id),
    Type VARCHAR(50) NOT NULL,
    CreatedAt TIMESTAMP NOT NULL,
    UNIQUE(PartyId, Type)
);
```

**Indexes:**
- `Parties.Email` - Unique lookup
- `Roles.PartyId` - Foreign key

### Catalog Database (catalog_db)

**Tables:**

```sql
CREATE TABLE Categories (
    Id UUID PRIMARY KEY,
    Name VARCHAR(255) NOT NULL,
    Description TEXT,
    CreatedAt TIMESTAMP NOT NULL,
    UpdatedAt TIMESTAMP
);

CREATE TABLE Books (
    Id UUID PRIMARY KEY,
    Title VARCHAR(500) NOT NULL,
    Isbn VARCHAR(20) NOT NULL UNIQUE,
    AuthorId UUID NOT NULL,
    AuthorName VARCHAR(255) NOT NULL,
    CategoryId UUID REFERENCES Categories(Id),
    TotalCopies INT NOT NULL DEFAULT 0,
    AvailableCopies INT NOT NULL DEFAULT 0,
    CreatedAt TIMESTAMP NOT NULL,
    UpdatedAt TIMESTAMP
);
```

**Indexes:**
- `Books.Isbn` - Unique lookup
- `Books.AuthorId` - Query by author
- `Books.CategoryId` - Query by category
- `Books.Title` - Search by title

### Lending Database (lending_db)

**Tables:**

```sql
CREATE TABLE Borrowings (
    Id UUID PRIMARY KEY,
    BookId UUID NOT NULL,
    BookTitle VARCHAR(500) NOT NULL,
    CustomerId UUID NOT NULL,
    CustomerName VARCHAR(255) NOT NULL,
    BorrowedAt TIMESTAMP NOT NULL,
    DueDate TIMESTAMP NOT NULL,
    ReturnedAt TIMESTAMP,
    Status VARCHAR(20) NOT NULL,
    CreatedAt TIMESTAMP NOT NULL,
    UpdatedAt TIMESTAMP
);
```

**Indexes:**
- `Borrowings.BookId` - Query by book
- `Borrowings.CustomerId` - Query by customer
- `Borrowings.Status` - Filter by status
- `Borrowings.DueDate` - Overdue queries

## MongoDB

MongoDB stores audit events as documents with flexible schemas.

### Database: library_audit

**Collection: Events**

```javascript
{
  "_id": ObjectId("..."),
  "EventType": "BookBorrowed",
  "EntityType": "Borrowing",
  "EntityId": "550e8400-e29b-41d4-a716-446655440000",
  "Timestamp": ISODate("2026-03-15T10:30:00Z"),
  "Payload": {
    "BookId": "550e8400-e29b-41d4-a716-446655440001",
    "BookTitle": "The Great Gatsby",
    "CustomerId": "550e8400-e29b-41d4-a716-446655440002",
    "CustomerName": "John Doe",
    "DueDate": "2026-03-29T10:30:00Z"
  },
  "ExpireAt": ISODate("2026-06-13T10:30:00Z")  // TTL index
}
```

**Indexes:**

```javascript
// TTL index for automatic expiration (90 days)
db.Events.createIndex(
  { "ExpireAt": 1 },
  { expireAfterSeconds: 0 }
);

// Query indexes
db.Events.createIndex({ "EntityId": 1, "Timestamp": -1 });
db.Events.createIndex({ "EntityType": 1, "Timestamp": -1 });
db.Events.createIndex({ "EventType": 1 });
db.Events.createIndex({ "Timestamp": -1 });
```

## Design Decisions

### Why Separate Databases?

1. **Service Independence**: Each service owns its data
2. **Schema Isolation**: Changes in one service don't affect others
3. **Scaling**: Each database can be scaled independently
4. **Technology Fit**: Different needs = different solutions

### Why Denormalized Data?

Several fields are duplicated across services:

| Field | Source | Copied To | Reason |
|-------|--------|-----------|--------|
| AuthorName | Party.API | Catalog.Books | Avoid cross-service joins |
| BookTitle | Catalog.API | Lending.Borrowings | Display without Catalog call |
| CustomerName | Party.API | Lending.Borrowings | Display without Party call |

**Trade-off**: Data can become stale if source changes. Acceptable because:
- Names rarely change
- Audit trail captures name at time of action
- Eventual consistency via events

### Why AvailableCopies as Stored Field?

Instead of calculating `TotalCopies - ActiveBorrowings`:

```sql
-- Would require cross-service query
SELECT b.*, (
    SELECT COUNT(*) FROM lending.Borrowings
    WHERE BookId = b.Id AND Status = 'Active'
) as ActiveBorrowings
FROM catalog.Books b
```

**Solution**: Store `AvailableCopies` in Catalog database

**Trade-off**: Risk of inconsistency if reserve succeeds but borrowing fails. Mitigated by:
- Saga pattern in production
- Compensating transactions
- Eventual consistency via events

## Entity Framework Configuration

### Party.API Context

```csharp
public class PartyDbContext : DbContext {
    public DbSet<Party> Parties { get; set; }
    public DbSet<Role> Roles { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        modelBuilder.Entity<Party>(entity => {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
        });

        modelBuilder.Entity<Role>(entity => {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.PartyId, e.Type }).IsUnique();
        });
    }
}
```

### Catalog.API Context

```csharp
public class CatalogDbContext : DbContext {
    public DbSet<Book> Books { get; set; }
    public DbSet<Category> Categories { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        modelBuilder.Entity<Book>(entity => {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Isbn).IsUnique();
            entity.HasIndex(e => e.AuthorId);
            entity.HasIndex(e => e.CategoryId);
        });
    }
}
```

## Migrations

### Creating Migrations

```bash
cd backend/Party.API
dotnet ef migrations add InitialCreate

cd backend/Catalog.API
dotnet ef migrations add InitialCreate

cd backend/Lending.API
dotnet ef migrations add InitialCreate
```

### Applying Migrations

```bash
# During development
dotnet ef database update

# In Docker (automatic on startup)
docker compose up -d
```

### Seeding Data

Each service seeds initial data on first startup:

- **Party.API**: Sample parties with Author and Customer roles
- **Catalog.API**: Categories and sample books
- **Lending.API**: No initial data (empty borrowing records)

## Connection Strings

### Development

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=party_db;Username=postgres;Password=postgres"
  }
}
```

### Docker

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=postgres;Database=party_db;Username=postgres;Password=postgres"
  }
}
```

### MongoDB

```json
{
  "MongoDB": {
    "ConnectionString": "mongodb://localhost:27017",
    "DatabaseName": "library_audit"
  }
}
```

## Backup and Recovery

### PostgreSQL

```bash
# Backup
docker compose exec postgres pg_dump -U postgres party_db > party_backup.sql

# Restore
docker compose exec -T postgres psql -U postgres party_db < party_backup.sql
```

### MongoDB

```bash
# Backup
docker compose exec mongo mongodump --db library_audit --out /backup

# Restore
docker compose exec mongo mongorestore --db library_audit /backup/library_audit
```

## Performance Considerations

### Query Optimization

1. **Use indexes** for foreign keys and search fields
2. **Pagination** for list endpoints (default 20 items)
3. **Projection** - select only needed columns
4. **Async** - all database operations are async

### Caching Opportunities

Future enhancements could include:
- Redis for frequently accessed data
- Response caching for read-heavy endpoints
- Materialized views for complex queries
