# Audit API

**Base URL**: `http://localhost:5400`

Event store and query service for the Library Management System.

## Overview

Audit.API consumes all events published by other services and stores them in MongoDB. This provides:

- Complete audit trail of all system activities
- Event history for any entity (party, book, borrowing)
- Filterable event queries
- Automatic data retention (90 days)

## Endpoints

### Get All Events

```
GET /api/events
```

Returns paginated events with optional filtering.

**Query Parameters**:

| Parameter | Type | Description |
|-----------|------|-------------|
| entityType | string | Filter by entity type (Party, Book, Borrowing) |
| action | string | Filter by action (Created, Updated, Borrowed, etc.) |
| entityId | string | Filter by specific entity ID |
| from | datetime | Start of date range (ISO 8601) |
| to | datetime | End of date range (ISO 8601) |
| page | int | Page number (default: 1) |
| pageSize | int | Items per page (default: 20, max: 100) |

**Response** (200 OK):
```json
{
  "items": [
    {
      "id": "64a1b2c3d4e5f6g7h8i9j0k1",
      "eventType": "BookBorrowed",
      "entityType": "Borrowing",
      "entityId": "550e8400-e29b-41d4-a716-446655440002",
      "timestamp": "2026-03-15T10:00:00Z",
      "payload": {
        "BorrowingId": "550e8400-e29b-41d4-a716-446655440002",
        "BookId": "550e8400-e29b-41d4-a716-446655440000",
        "BookTitle": "The Great Gatsby",
        "CustomerId": "550e8400-e29b-41d4-a716-446655440001",
        "CustomerName": "John Doe",
        "BorrowedAt": "2026-03-15T10:00:00Z",
        "DueDate": "2026-03-29T10:00:00Z"
      }
    }
  ],
  "page": 1,
  "pageSize": 20,
  "totalCount": 150,
  "totalPages": 8
}
```

### Get Events for Party

```
GET /api/events/parties/{partyId}
```

Returns all events related to a specific party.

**Parameters**:
- `partyId` (path, required): Party GUID

**Query Parameters**:
- `page`: Page number (default: 1)
- `pageSize`: Items per page (default: 20, max: 100)

**Response** (200 OK):
```json
{
  "items": [
    {
      "id": "64a1b2c3d4e5f6g7h8i9j0k1",
      "eventType": "PartyCreated",
      "entityType": "Party",
      "entityId": "550e8400-e29b-41d4-a716-446655440000",
      "timestamp": "2026-03-15T09:00:00Z",
      "payload": {
        "PartyId": "550e8400-e29b-41d4-a716-446655440000",
        "Name": "John Doe",
        "Email": "john@example.com"
      }
    },
    {
      "id": "64a1b2c3d4e5f6g7h8i9j0k2",
      "eventType": "RoleAssigned",
      "entityType": "Party",
      "entityId": "550e8400-e29b-41d4-a716-446655440000",
      "timestamp": "2026-03-15T09:05:00Z",
      "payload": {
        "PartyId": "550e8400-e29b-41d4-a716-446655440000",
        "RoleType": "Customer"
      }
    }
  ],
  "page": 1,
  "pageSize": 20,
  "totalCount": 2,
  "totalPages": 1
}
```

### Get Events for Book

```
GET /api/events/books/{bookId}
```

Returns all events related to a specific book.

**Parameters**:
- `bookId` (path, required): Book GUID

**Query Parameters**:
- `page`: Page number (default: 1)
- `pageSize`: Items per page (default: 20, max: 100)

**Response** (200 OK):
```json
{
  "items": [
    {
      "id": "64a1b2c3d4e5f6g7h8i9j0k1",
      "eventType": "BookCreated",
      "entityType": "Book",
      "entityId": "550e8400-e29b-41d4-a716-446655440000",
      "timestamp": "2026-03-15T08:00:00Z",
      "payload": {
        "BookId": "550e8400-e29b-41d4-a716-446655440000",
        "Title": "The Great Gatsby",
        "Isbn": "978-0743273565",
        "AuthorId": "550e8400-e29b-41d4-a716-446655440001"
      }
    },
    {
      "id": "64a1b2c3d4e5f6g7h8i9j0k2",
      "eventType": "BookBorrowed",
      "entityType": "Borrowing",
      "entityId": "550e8400-e29b-41d4-a716-446655440002",
      "timestamp": "2026-03-15T10:00:00Z",
      "payload": {
        "BorrowingId": "550e8400-e29b-41d4-a716-446655440002",
        "BookId": "550e8400-e29b-41d4-a716-446655440000",
        "CustomerId": "550e8400-e29b-41d4-a716-446655440003"
      }
    },
    {
      "id": "64a1b2c3d4e5f6g7h8i9j0k3",
      "eventType": "BookReturned",
      "entityType": "Borrowing",
      "entityId": "550e8400-e29b-41d4-a716-446655440002",
      "timestamp": "2026-03-20T14:30:00Z",
      "payload": {
        "BorrowingId": "550e8400-e29b-41d4-a716-446655440002",
        "BookId": "550e8400-e29b-41d4-a716-446655440000",
        "CustomerId": "550e8400-e29b-41d4-a716-446655440003"
      }
    }
  ],
  "page": 1,
  "pageSize": 20,
  "totalCount": 3,
  "totalPages": 1
}
```

## Examples

### Query All Events

```bash
# Get all events (paginated)
curl http://localhost:5400/api/events

# Get page 2 with 50 items per page
curl "http://localhost:5400/api/events?page=2&pageSize=50"
```

### Filter by Entity Type

```bash
# Get only Book-related events
curl "http://localhost:5400/api/events?entityType=Book"

# Get only Borrowing-related events
curl "http://localhost:5400/api/events?entityType=Borrowing"
```

### Filter by Action

```bash
# Get only creation events
curl "http://localhost:5400/api/events?action=Created"

# Get only borrow events
curl "http://localhost:5400/api/events?action=Borrowed"
```

### Filter by Date Range

```bash
# Events from last 24 hours
curl "http://localhost:5400/api/events?from=2026-03-14T10:00:00Z&to=2026-03-15T10:00:00Z"
```

### Combined Filters

```bash
# Book borrow events in March 2026
curl "http://localhost:5400/api/events?entityType=Borrowing&action=Borrowed&from=2026-03-01T00:00:00Z&to=2026-03-31T23:59:59Z"
```

### Get Party History

```bash
curl http://localhost:5400/api/events/parties/550e8400-e29b-41d4-a716-446655440000
```

### Get Book History

```bash
curl http://localhost:5400/api/events/books/550e8400-e29b-41d4-a716-446655440000
```

## Data Model

### Event

| Field | Type | Description |
|-------|------|-------------|
| id | string | MongoDB document ID |
| eventType | string | Type of event (e.g., BookCreated) |
| entityType | string | Category (Party, Book, Borrowing) |
| entityId | string | ID of the affected entity |
| timestamp | datetime | When the event occurred |
| payload | object | Event-specific data |
| expireAt | datetime | Auto-deletion timestamp (90 days) |

### PagedResponse

| Field | Type | Description |
|-------|------|-------------|
| items | array | List of events |
| page | int | Current page number |
| pageSize | int | Items per page |
| totalCount | int | Total matching events |
| totalPages | int | Total pages available |

## Event Types Reference

### Party Events

| Event Type | Entity Type | Description |
|------------|-------------|-------------|
| PartyCreated | Party | New party registered |
| PartyUpdated | Party | Party details changed |
| RoleAssigned | Party | Role added to party |
| RoleRemoved | Party | Role removed from party |

### Catalog Events

| Event Type | Entity Type | Description |
|------------|-------------|-------------|
| BookCreated | Book | New book added |
| BookUpdated | Book | Book details changed |
| BookDeleted | Book | Book removed |

### Lending Events

| Event Type | Entity Type | Description |
|------------|-------------|-------------|
| BookBorrowed | Borrowing | Book checked out |
| BookReturned | Borrowing | Book checked in |

## Data Retention

Events are automatically deleted after 90 days using:

1. **MongoDB TTL Index**: Checks every ~60 seconds for expired documents
2. **Background Cleanup Job**: Runs daily for guaranteed removal

This ensures the event store doesn't grow indefinitely while maintaining recent history.

## Event Consumption

Audit.API uses a RabbitMQ consumer with wildcard binding:

```csharp
// Binds to ALL routing keys
channel.QueueBind(queue, "library.events", routingKey: "#");
```

This means Audit.API receives every event published to the exchange, regardless of type.

## Use Cases

### Audit Trail

```bash
# Get complete history for a book
curl http://localhost:5400/api/events/books/{bookId}

# Response shows: created → borrowed → returned → borrowed → returned
```

### Activity Monitoring

```bash
# Check recent borrowing activity
curl "http://localhost:5400/api/events?entityType=Borrowing&from=2026-03-14T00:00:00Z"
```

### Debugging

```bash
# Find all events for a specific entity
curl "http://localhost:5400/api/events?entityId=550e8400-e29b-41d4-a716-446655440000"
```

### Reporting

```bash
# Count borrows in a date range
curl "http://localhost:5400/api/events?action=Borrowed&from=2026-03-01T00:00:00Z&to=2026-03-31T23:59:59Z"
```
