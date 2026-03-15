# Lending API

**Base URL**: `http://localhost:5300`

Orchestrates book borrowing and return workflows.

## Endpoints

### Borrow Book

```
POST /api/lending/borrow
```

Creates a new borrowing record. This endpoint orchestrates the entire borrow flow:
1. Validates the customer exists and has Customer role
2. Checks book availability
3. Reserves a copy in Catalog.API
4. Creates the borrowing record
5. Publishes BookBorrowed event

**Request Body**:
```json
{
  "bookId": "550e8400-e29b-41d4-a716-446655440000",
  "customerId": "550e8400-e29b-41d4-a716-446655440001"
}
```

**Validation Rules**:
- `bookId`: Required, valid GUID
- `customerId`: Required, valid GUID
- Customer must exist in Party.API with Customer role
- Book must have available copies

**Response** (201 Created):
```json
{
  "id": "550e8400-e29b-41d4-a716-446655440002",
  "bookId": "550e8400-e29b-41d4-a716-446655440000",
  "bookTitle": "The Great Gatsby",
  "customerId": "550e8400-e29b-41d4-a716-446655440001",
  "customerName": "John Doe",
  "borrowedAt": "2026-03-15T10:00:00Z",
  "dueDate": "2026-03-29T10:00:00Z",
  "returnedAt": null,
  "status": "Active"
}
```

**Error Response** (400 Bad Request):
```json
{
  "title": "Bad Request",
  "status": 400,
  "detail": "Customer does not have Customer role"
}
```

```json
{
  "title": "Bad Request",
  "status": 400,
  "detail": "No copies available for borrowing"
}
```

### Return Book

```
POST /api/lending/{bookId}/return
```

Processes a book return. This endpoint:
1. Finds the active borrowing for the book and customer
2. Releases the copy in Catalog.API
3. Marks the borrowing as returned
4. Publishes BookReturned event

**Parameters**:
- `bookId` (path, required): Book GUID

**Request Body**:
```json
{
  "customerId": "550e8400-e29b-41d4-a716-446655440001"
}
```

**Response** (200 OK):
```json
{
  "id": "550e8400-e29b-41d4-a716-446655440002",
  "bookId": "550e8400-e29b-41d4-a716-446655440000",
  "bookTitle": "The Great Gatsby",
  "customerId": "550e8400-e29b-41d4-a716-446655440001",
  "customerName": "John Doe",
  "borrowedAt": "2026-03-15T10:00:00Z",
  "dueDate": "2026-03-29T10:00:00Z",
  "returnedAt": "2026-03-20T14:30:00Z",
  "status": "Returned"
}
```

**Error Response** (400 Bad Request):
```json
{
  "title": "Bad Request",
  "status": 400,
  "detail": "No active borrowing found for this book and customer"
}
```

### Get Borrowed Books Summary

```
GET /api/lending/summary
```

Returns a summary of all currently borrowed books with customer information.

**Response** (200 OK):
```json
[
  {
    "borrowingId": "550e8400-e29b-41d4-a716-446655440002",
    "bookId": "550e8400-e29b-41d4-a716-446655440000",
    "bookTitle": "The Great Gatsby",
    "customerId": "550e8400-e29b-41d4-a716-446655440001",
    "customerName": "John Doe",
    "borrowedAt": "2026-03-15T10:00:00Z",
    "dueDate": "2026-03-29T10:00:00Z",
    "isOverdue": false
  }
]
```

### Get Borrowing by ID

```
GET /api/lending/{id}
```

**Parameters**:
- `id` (path, required): Borrowing GUID

**Response** (200 OK):
```json
{
  "id": "550e8400-e29b-41d4-a716-446655440002",
  "bookId": "550e8400-e29b-41d4-a716-446655440000",
  "bookTitle": "The Great Gatsby",
  "customerId": "550e8400-e29b-41d4-a716-446655440001",
  "customerName": "John Doe",
  "borrowedAt": "2026-03-15T10:00:00Z",
  "dueDate": "2026-03-29T10:00:00Z",
  "returnedAt": null,
  "status": "Active"
}
```

### Get Customer's Borrowings

```
GET /api/lending/by-customer/{customerId}
```

Returns all borrowing records for a specific customer.

**Parameters**:
- `customerId` (path, required): Customer GUID

**Response** (200 OK):
```json
[
  {
    "id": "550e8400-e29b-41d4-a716-446655440002",
    "bookId": "550e8400-e29b-41d4-a716-446655440000",
    "bookTitle": "The Great Gatsby",
    "customerId": "550e8400-e29b-41d4-a716-446655440001",
    "customerName": "John Doe",
    "borrowedAt": "2026-03-15T10:00:00Z",
    "dueDate": "2026-03-29T10:00:00Z",
    "returnedAt": null,
    "status": "Active"
  },
  {
    "id": "550e8400-e29b-41d4-a716-446655440003",
    "bookId": "550e8400-e29b-41d4-a716-446655440004",
    "bookTitle": "1984",
    "customerId": "550e8400-e29b-41d4-a716-446655440001",
    "customerName": "John Doe",
    "borrowedAt": "2026-02-01T09:00:00Z",
    "dueDate": "2026-02-15T09:00:00Z",
    "returnedAt": "2026-02-10T16:00:00Z",
    "status": "Returned"
  }
]
```

### Get Book's Borrowings

```
GET /api/lending/by-book/{bookId}
```

Returns all borrowing records for a specific book.

**Parameters**:
- `bookId` (path, required): Book GUID

**Response** (200 OK):
```json
[
  {
    "id": "550e8400-e29b-41d4-a716-446655440002",
    "bookId": "550e8400-e29b-41d4-a716-446655440000",
    "bookTitle": "The Great Gatsby",
    "customerId": "550e8400-e29b-41d4-a716-446655440001",
    "customerName": "John Doe",
    "borrowedAt": "2026-03-15T10:00:00Z",
    "dueDate": "2026-03-29T10:00:00Z",
    "returnedAt": null,
    "status": "Active"
  }
]
```

## Examples

### Complete Borrow/Return Flow

```bash
# 1. Create a customer
curl -X POST http://localhost:5100/api/parties \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Alice Johnson",
    "email": "alice@example.com"
  }'
# Save the returned customerId

# 2. Assign Customer role
curl -X POST http://localhost:5100/api/parties/{customerId}/roles \
  -H "Content-Type: application/json" \
  -d '{"roleType": "Customer"}'

# 3. Get a book ID from Catalog
curl http://localhost:5200/api/catalog/books
# Save a bookId

# 4. Borrow the book
curl -X POST http://localhost:5300/api/lending/borrow \
  -H "Content-Type: application/json" \
  -d '{
    "bookId": "{bookId}",
    "customerId": "{customerId}"
  }'
# Save the returned borrowingId

# 5. Check borrowed books summary
curl http://localhost:5300/api/lending/summary

# 6. Return the book
curl -X POST http://localhost:5300/api/lending/{bookId}/return \
  -H "Content-Type: application/json" \
  -d '{"customerId": "{customerId}"}'
```

### Check Customer History

```bash
curl http://localhost:5300/api/lending/by-customer/{customerId}
```

### Check Book History

```bash
curl http://localhost:5300/api/lending/by-book/{bookId}
```

## Data Model

### Borrowing

| Field | Type | Description |
|-------|------|-------------|
| id | UUID | Unique identifier |
| bookId | UUID | Reference to Book |
| bookTitle | string | Denormalized book title |
| customerId | UUID | Reference to Party (Customer) |
| customerName | string | Denormalized customer name |
| borrowedAt | datetime | When book was borrowed |
| dueDate | datetime | When book is due (14 days) |
| returnedAt | datetime | When book was returned (null if active) |
| status | string | Active, Returned, or Overdue |

### BorrowingStatus Enum

| Value | Description |
|-------|-------------|
| Active | Book is currently borrowed |
| Returned | Book has been returned |
| Overdue | Past due date, not returned |

### BorrowedBookSummary

| Field | Type | Description |
|-------|------|-------------|
| borrowingId | UUID | Reference to borrowing |
| bookId | UUID | Reference to book |
| bookTitle | string | Book title |
| customerId | UUID | Reference to customer |
| customerName | string | Customer name |
| borrowedAt | datetime | Borrow timestamp |
| dueDate | datetime | Due timestamp |
| isOverdue | boolean | Whether past due date |

## Business Rules

1. **Customer Validation**: Customer must exist and have the Customer role
2. **Availability Check**: Book must have at least one available copy
3. **Single Active Borrowing**: A customer cannot borrow the same book twice simultaneously
4. **Due Date**: Set to 14 days from borrow date
5. **Return Window**: Returns must match an active borrowing for the book and customer

## Events Published

| Event | Routing Key | Description |
|-------|-------------|-------------|
| BookBorrowed | `borrowing.borrowed` | Book checked out |
| BookReturned | `borrowing.returned` | Book checked in |

## Event Payloads

### BookBorrowed

```json
{
  "BorrowingId": "550e8400-e29b-41d4-a716-446655440002",
  "BookId": "550e8400-e29b-41d4-a716-446655440000",
  "BookTitle": "The Great Gatsby",
  "CustomerId": "550e8400-e29b-41d4-a716-446655440001",
  "CustomerName": "John Doe",
  "BorrowedAt": "2026-03-15T10:00:00Z",
  "DueDate": "2026-03-29T10:00:00Z"
}
```

### BookReturned

```json
{
  "BorrowingId": "550e8400-e29b-41d4-a716-446655440002",
  "BookId": "550e8400-e29b-41d4-a716-446655440000",
  "CustomerId": "550e8400-e29b-41d4-a716-446655440001",
  "ReturnedAt": "2026-03-20T14:30:00Z"
}
```
