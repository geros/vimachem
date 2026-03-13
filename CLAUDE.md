# Library Management System - Coding Conventions

## C# Style Guide

### Formatting
- **Indentation**: Tabs (width 4)
- **Line endings**: LF
- **Charset**: UTF-8
- **Trim trailing whitespace**: Yes
- **Insert final newline**: Yes

### Braces - Same Line (K&R / Java Style)
```csharp
// Correct
public class MyClass {
	public void MyMethod() {
		if (condition) {
			DoSomething();
		} else {
			DoOtherThing();
		}
	}
}

// Incorrect
public class MyClass
{
	public void MyMethod()
	{
		if (condition)
		{
		}
	}
}
```

### Namespaces
- Use file-scoped namespaces
```csharp
namespace Party.API.Domain;

public class Party { }
```

### var Preferences
- Use `var` for built-in types
- Use `var` when type is apparent
- Use `var` elsewhere (suggestion)

### Expression-Bodied Members
- Properties: expression-bodied preferred
- Methods: when on single line
- Constructors: expression-bodied discouraged

## Conventional Commits

Format: `<type>(<scope>): <description>`

Types:
- `feat`: New feature
- `fix`: Bug fix
- `docs`: Documentation
- `style`: Formatting changes
- `refactor`: Code restructuring
- `test`: Tests
- `chore`: Build/process changes

Scopes:
- `party-api`
- `catalog-api`
- `lending-api`
- `audit-api`
- `shared`
- `infra`

Examples:
```
feat(party-api): add Party aggregate with role management
fix(catalog-api): handle null author in book creation
docs: update README with architecture diagram
```

## Project Structure

```
src/
├── Party.API/
│   ├── Domain/
│   ├── Application/
│   ├── Infrastructure/
│   └── Controllers/
├── Catalog.API/
│   ├── Domain/
│   ├── Application/
│   ├── Infrastructure/
│   ├── HttpClients/
│   └── Controllers/
├── Lending.API/
│   ├── Domain/
│   ├── Application/
│   ├── Infrastructure/
│   ├── HttpClients/
│   └── Controllers/
├── Audit.API/
│   ├── Domain/
│   ├── Application/
│   ├── Infrastructure/
│   └── Controllers/
└── Shared/
    └── Events/

tests/
├── Party.API.Tests/
├── Catalog.API.Tests/
├── Lending.API.Tests/
└── Audit.API.Tests/
```

## Architecture Patterns

### Domain-Driven Design
- Entities are aggregate roots with encapsulated behavior
- Domain exceptions for invariants
- Value objects where appropriate

### CQRS (Query/Command separation)
- Commands modify state
- Queries return DTOs
- No domain entities in API responses

### Event-Driven Architecture
- Integration events via RabbitMQ
- Topic exchange: `library.events`
- Events: `PartyCreated`, `BookCreated`, `BookBorrowed`, `BookReturned`

## Technology Stack

- **.NET 8/9** - Web APIs
- **PostgreSQL** - SQL databases (Party, Catalog, Lending)
- **MongoDB** - Event store (Audit)
- **RabbitMQ** - Message broker
- **EF Core** - ORM
- **FluentValidation** - Input validation
- **Polly** - Resilience patterns
- **xUnit** - Testing framework
