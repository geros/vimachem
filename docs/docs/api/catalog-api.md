# Catalog API

**Base URL**: `http://localhost:5200`

Manages books and categories in the library catalog.

## Endpoints

### Books

#### List All Books

```
GET /api/catalog/books
```

Returns all books with their details.

**Response** (200 OK):
```json
[
  {
    "id": "550e8400-e29b-41d4-a716-446655440000",
    "title": "The Great Gatsby",
    "isbn": "978-0743273565",
    "authorId": "550e8400-e29b-41d4-a716-446655440001",
    "authorName": "F. Scott Fitzgerald",
    "categoryId": "550e8400-e29b-41d4-a716-446655440002",
    "totalCopies": 5,
    "availableCopies": 3
  }
]
```

#### Get Book by ID

```
GET /api/catalog/books/{id}
```

**Parameters**:
- `id` (path, required): Book GUID

**Response** (200 OK):
```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "title": "The Great Gatsby",
  "isbn": "978-0743273565",
  "authorId": "550e8400-e29b-41d4-a716-446655440001",
  "authorName": "F. Scott Fitzgerald",
  "categoryId": "550e8400-e29b-41d4-a716-446655440002",
  "totalCopies": 5,
  "availableCopies": 3
}
```

#### Search Books by Title

```
GET /api/catalog/books/search?title={title}
```

**Parameters**:
- `title` (query, required): Search term (case-insensitive partial match)

**Response** (200 OK):
```json
[
  {
    "id": "550e8400-e29b-41d4-a716-446655440000",
    "title": "The Great Gatsby",
    "isbn": "978-0743273565",
    "authorId": "550e8400-e29b-41d4-a716-446655440001",
    "authorName": "F. Scott Fitzgerald",
    "categoryId": "550e8400-e29b-41d4-a716-446655440002",
    "totalCopies": 5,
    "availableCopies": 3
  }
]
```

#### Check Book Availability

```
GET /api/catalog/books/{id}/availability
```

**Parameters**:
- `id` (path, required): Book GUID

**Response** (200 OK):
```json
{
  "bookId": "550e8400-e29b-41d4-a716-446655440000",
  "title": "The Great Gatsby",
  "isAvailable": true,
  "availableCopies": 3,
  "totalCopies": 5
}
```

#### Create Book

```
POST /api/catalog/books
```

Creates a new book in the catalog.

**Request Body**:
```json
{
  "title": "1984",
  "isbn": "978-0451524935",
  "authorId": "550e8400-e29b-41d4-a716-446655440003",
  "categoryId": "550e8400-e29b-41d4-a716-446655440002",
  "totalCopies": 5
}
```

**Validation Rules**:
- `title`: Required, 1-500 characters
- `isbn`: Required, unique, valid ISBN format
- `authorId`: Required, must exist in Party.API with Author role
- `categoryId`: Optional, must exist if provided
- `totalCopies`: Required, must be >= 0

**Response** (201 Created):
```json
{
  "id": "550e8400-e29b-41d4-a716-446655440004",
  "title": "1984",
  "isbn": "978-0451524935",
  "authorId": "550e8400-e29b-41d4-a716-446655440003",
  "authorName": "George Orwell",
  "categoryId": "550e8400-e29b-41d4-a716-446655440002",
  "totalCopies": 5,
  "availableCopies": 5
}
```

#### Update Book

```
PUT /api/catalog/books/{id}
```

Updates an existing book.

**Parameters**:
- `id` (path, required): Book GUID

**Request Body**:
```json
{
  "title": "Nineteen Eighty-Four",
  "isbn": "978-0451524935",
  "authorId": "550e8400-e29b-41d4-a716-446655440003",
  "categoryId": "550e8400-e29b-41d4-a716-446655440002",
  "totalCopies": 10
}
```

**Response** (200 OK):
```json
{
  "id": "550e8400-e29b-41d4-a716-446655440004",
  "title": "Nineteen Eighty-Four",
  "isbn": "978-0451524935",
  "authorId": "550e8400-e29b-41d4-a716-446655440003",
  "authorName": "George Orwell",
  "categoryId": "550e8400-e29b-41d4-a716-446655440002",
  "totalCopies": 10,
  "availableCopies": 8
}
```

#### Delete Book

```
DELETE /api/catalog/books/{id}
```

Deletes a book from the catalog.

**Parameters**:
- `id` (path, required): Book GUID

**Response** (204 No Content)

#### Reserve Copy (Internal)

```
PUT /api/catalog/books/{id}/reserve
```

Decrements available copies. Called by Lending.API during borrow flow.

**Parameters**:
- `id` (path, required): Book GUID

**Response** (200 OK):
```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "title": "The Great Gatsby",
  "availableCopies": 2
}
```

**Error Response** (400 Bad Request):
```json
{
  "title": "Bad Request",
  "status": 400,
  "detail": "No copies available"
}
```

#### Release Copy (Internal)

```
PUT /api/catalog/books/{id}/release
```

Increments available copies. Called by Lending.API during return flow.

**Parameters**:
- `id` (path, required): Book GUID

**Response** (200 OK):
```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "title": "The Great Gatsby",
  "availableCopies": 3
}
```

### Categories

#### List All Categories

```
GET /api/catalog/categories
```

**Response** (200 OK):
```json
[
  {
    "id": "550e8400-e29b-41d4-a716-446655440000",
    "name": "Fiction",
    "description": "Novels and stories"
  },
  {
    "id": "550e8400-e29b-41d4-a716-446655440001",
    "name": "Science Fiction",
    "description": "Sci-fi books"
  }
]
```

#### Get Category by ID

```
GET /api/catalog/categories/{id}
```

**Parameters**:
- `id` (path, required): Category GUID

**Response** (200 OK):
```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "name": "Fiction",
  "description": "Novels and stories"
}
```

#### Create Category

```
POST /api/catalog/categories
```

**Request Body**:
```json
{
  "name": "Mystery",
  "description": "Mystery and detective novels"
}
```

**Validation Rules**:
- `name`: Required, 1-255 characters, unique
- `description`: Optional, max 1000 characters

**Response** (201 Created):
```json
{
  "id": "550e8400-e29b-41d4-a716-446655440002",
  "name": "Mystery",
  "description": "Mystery and detective novels"
}
```

#### Update Category

```
PUT /api/catalog/categories/{id}
```

**Parameters**:
- `id` (path, required): Category GUID

**Request Body**:
```json
{
  "name": "Mystery & Thriller",
  "description": "Mystery, thriller, and suspense novels"
}
```

**Response** (200 OK):
```json
{
  "id": "550e8400-e29b-41d4-a716-446655440002",
  "name": "Mystery & Thriller",
  "description": "Mystery, thriller, and suspense novels"
}
```

#### Delete Category

```
DELETE /api/catalog/categories/{id}
```

**Parameters**:
- `id` (path, required): Category GUID

**Response** (204 No Content)

## Examples

### Create a Book with Author

```bash
# 1. First, create an author in Party.API
curl -X POST http://localhost:5100/api/parties \
  -H "Content-Type: application/json" \
  -d '{
    "name": "George Orwell",
    "email": "orwell@example.com"
  }'

# 2. Assign Author role
curl -X POST http://localhost:5100/api/parties/{authorId}/roles \
  -H "Content-Type: application/json" \
  -d '{"roleType": "Author"}'

# 3. Create a category
curl -X POST http://localhost:5200/api/catalog/categories \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Dystopian Fiction",
    "description": "Dystopian novels"
  }'

# 4. Create the book
curl -X POST http://localhost:5200/api/catalog/books \
  -H "Content-Type: application/json" \
  -d '{
    "title": "1984",
    "isbn": "978-0451524935",
    "authorId": "{authorId}",
    "categoryId": "{categoryId}",
    "totalCopies": 5
  }'
```

### Search Books

```bash
curl "http://localhost:5200/api/catalog/books/search?title=great"
```

### Check Availability

```bash
curl http://localhost:5200/api/catalog/books/{bookId}/availability
```

## Data Model

### Book

| Field | Type | Description |
|-------|------|-------------|
| id | UUID | Unique identifier |
| title | string | Book title (1-500 chars) |
| isbn | string | ISBN (unique) |
| authorId | UUID | Reference to Party |
| authorName | string | Denormalized author name |
| categoryId | UUID | Reference to Category |
| totalCopies | int | Total copies owned |
| availableCopies | int | Copies currently available |

### Category

| Field | Type | Description |
|-------|------|-------------|
| id | UUID | Unique identifier |
| name | string | Category name (1-255 chars, unique) |
| description | string | Optional description |

## Events Published

| Event | Routing Key | Description |
|-------|-------------|-------------|
| BookCreated | `book.created` | New book added |
| BookUpdated | `book.updated` | Book details changed |
| BookDeleted | `book.deleted` | Book removed |
