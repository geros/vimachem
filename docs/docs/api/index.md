# API Reference

Complete reference for all REST API endpoints in the Library Management System.

## Base URLs

| Environment | Base URL |
|-------------|----------|
| Docker | `http://localhost:{port}` |
| Local | `http://localhost:{port}` |

| Service | Port |
|---------|------|
| Party.API | 5100 |
| Catalog.API | 5200 |
| Lending.API | 5300 |
| Audit.API | 5400 |

## Authentication

Currently, the APIs do not require authentication. All endpoints are publicly accessible.

## Content Types

All APIs accept and return JSON:

```
Content-Type: application/json
Accept: application/json
```

## Common Response Codes

| Code | Meaning |
|------|---------|
| 200 | Success |
| 201 | Created |
| 204 | No Content (successful deletion) |
| 400 | Bad Request (validation error) |
| 404 | Not Found |
| 500 | Internal Server Error |

## Error Response Format

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "Bad Request",
  "status": 400,
  "errors": {
    "Name": ["Name is required"],
    "Email": ["Email is invalid"]
  }
}
```

## API Documentation

- [Party API](party-api.md) - People and role management
- [Catalog API](catalog-api.md) - Books and categories
- [Lending API](lending-api.md) - Borrowing and returns
- [Audit API](audit-api.md) - Event store queries

## Swagger UI

Each service provides interactive API documentation via Swagger UI:

| Service | Swagger URL |
|---------|-------------|
| Party.API | http://localhost:5100/swagger |
| Catalog.API | http://localhost:5200/swagger |
| Lending.API | http://localhost:5300/swagger |
| Audit.API | http://localhost:5400/swagger |

## Testing with curl

### Example: Create a Party

```bash
curl -X POST http://localhost:5100/api/parties \
  -H "Content-Type: application/json" \
  -d '{
    "name": "John Doe",
    "email": "john@example.com"
  }'
```

### Example: List Books

```bash
curl http://localhost:5200/api/catalog/books
```

### Example: Borrow a Book

```bash
curl -X POST http://localhost:5300/api/lending/borrow \
  -H "Content-Type: application/json" \
  -d '{
    "bookId": "550e8400-e29b-41d4-a716-446655440000",
    "customerId": "550e8400-e29b-41d4-a716-446655440001"
  }'
```

### Example: Query Events

```bash
curl "http://localhost:5400/api/events?entityType=Book&page=1&pageSize=10"
```
