# Party API

**Base URL**: `http://localhost:5100`

Manages parties (people) and their roles in the library system.

## Endpoints

### List All Parties

```
GET /api/parties
```

Returns all parties with their roles.

**Response** (200 OK):
```json
[
  {
    "id": "550e8400-e29b-41d4-a716-446655440000",
    "name": "John Doe",
    "email": "john@example.com",
    "roles": ["Author", "Customer"],
    "createdAt": "2026-03-15T10:00:00Z",
    "updatedAt": "2026-03-15T10:05:00Z"
  }
]
```

### Get Party by ID

```
GET /api/parties/{id}
```

**Parameters**:
- `id` (path, required): Party GUID

**Response** (200 OK):
```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "name": "John Doe",
  "email": "john@example.com",
  "roles": ["Author", "Customer"],
  "createdAt": "2026-03-15T10:00:00Z",
  "updatedAt": "2026-03-15T10:05:00Z"
}
```

**Response** (404 Not Found):
```json
{
  "title": "Not Found",
  "status": 404,
  "detail": "Party with id '550e8400-e29b-41d4-a716-446655440000' not found"
}
```

### Create Party

```
POST /api/parties
```

Creates a new party.

**Request Body**:
```json
{
  "name": "Jane Smith",
  "email": "jane@example.com"
}
```

**Validation Rules**:
- `name`: Required, 1-255 characters
- `email`: Required, valid email format, must be unique

**Response** (201 Created):
```json
{
  "id": "550e8400-e29b-41d4-a716-446655440001",
  "name": "Jane Smith",
  "email": "jane@example.com",
  "roles": [],
  "createdAt": "2026-03-15T10:10:00Z",
  "updatedAt": null
}
```

### Update Party

```
PUT /api/parties/{id}
```

Updates an existing party.

**Parameters**:
- `id` (path, required): Party GUID

**Request Body**:
```json
{
  "name": "Jane Doe",
  "email": "jane.doe@example.com"
}
```

**Response** (200 OK):
```json
{
  "id": "550e8400-e29b-41d4-a716-446655440001",
  "name": "Jane Doe",
  "email": "jane.doe@example.com",
  "roles": [],
  "createdAt": "2026-03-15T10:10:00Z",
  "updatedAt": "2026-03-15T10:15:00Z"
}
```

### Delete Party

```
DELETE /api/parties/{id}
```

Deletes a party.

**Parameters**:
- `id` (path, required): Party GUID

**Response** (204 No Content)

### Assign Role

```
POST /api/parties/{id}/roles
```

Assigns a role to a party.

**Parameters**:
- `id` (path, required): Party GUID

**Request Body**:
```json
{
  "roleType": "Author"
}
```

**Valid Role Types**:
- `Author` - Can be assigned as book author
- `Customer` - Can borrow books

**Response** (200 OK):
```json
{
  "id": "550e8400-e29b-41d4-a716-446655440001",
  "name": "Jane Doe",
  "email": "jane.doe@example.com",
  "roles": ["Author"],
  "createdAt": "2026-03-15T10:10:00Z",
  "updatedAt": "2026-03-15T10:20:00Z"
}
```

**Error Response** (400 Bad Request):
```json
{
  "title": "Bad Request",
  "status": 400,
  "detail": "Party already has role 'Author'"
}
```

### Remove Role

```
DELETE /api/parties/{id}/roles/{roleType}
```

Removes a role from a party.

**Parameters**:
- `id` (path, required): Party GUID
- `roleType` (path, required): Role to remove (`Author` or `Customer`)

**Response** (200 OK):
```json
{
  "id": "550e8400-e29b-41d4-a716-446655440001",
  "name": "Jane Doe",
  "email": "jane.doe@example.com",
  "roles": [],
  "createdAt": "2026-03-15T10:10:00Z",
  "updatedAt": "2026-03-15T10:25:00Z"
}
```

## Examples

### Create an Author

```bash
# 1. Create the party
curl -X POST http://localhost:5100/api/parties \
  -H "Content-Type: application/json" \
  -d '{
    "name": "George Orwell",
    "email": "orwell@example.com"
  }'

# Response: {"id": "abc-123", ...}

# 2. Assign Author role
curl -X POST http://localhost:5100/api/parties/abc-123/roles \
  -H "Content-Type: application/json" \
  -d '{"roleType": "Author"}'
```

### Create a Customer

```bash
# 1. Create the party
curl -X POST http://localhost:5100/api/parties \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Alice Johnson",
    "email": "alice@example.com"
  }'

# 2. Assign Customer role
curl -X POST http://localhost:5100/api/parties/{id}/roles \
  -H "Content-Type: application/json" \
  -d '{"roleType": "Customer"}'
```

## Data Model

### Party

| Field | Type | Description |
|-------|------|-------------|
| id | UUID | Unique identifier |
| name | string | Full name (1-255 chars) |
| email | string | Email address (unique) |
| roles | array | List of assigned roles |
| createdAt | datetime | Creation timestamp |
| updatedAt | datetime | Last update timestamp |

### RoleType Enum

| Value | Description |
|-------|-------------|
| Author | Can be assigned as book author |
| Customer | Can borrow books |

## Events Published

| Event | Routing Key | Description |
|-------|-------------|-------------|
| PartyCreated | `party.created` | New party created |
| PartyUpdated | `party.updated` | Party details updated |
| RoleAssigned | `party.role_assigned` | Role added |
| RoleRemoved | `party.role_removed` | Role removed |
