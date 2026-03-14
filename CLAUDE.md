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

## Git Workflow

### Commit Practices
- **All work must be committed** using Conventional Commits format
- Commit frequently with focused, atomic changes
- Each commit should represent a single logical change
- Write clear, descriptive commit messages that explain the "why" not just the "what"
- Use body for additional context when needed (separate from subject with blank line)
- Reference issues/PRs in footer when applicable

### Commit Message Format
```
<type>(<scope>): <subject>

<body>

<footer>
```

### Best Practices
- Keep subject line under 72 characters
- Use imperative mood in subject ("add" not "added", "fix" not "fixed")
- Do not end subject with a period
- Separate subject from body with a blank line
- Explain what and why in body, not how (code shows how)

### Feature Branch Workflow
1. **Rebase feature branches** onto the target branch before merging:
   ```bash
   git checkout feature/my-feature
   git fetch origin
   git rebase origin/main
   ```
2. **Merge with --no-ff** (no fast-forward) to preserve branch history:
   ```bash
   git checkout main
   git merge --no-ff feature/my-feature
   ```
3. **Write descriptive merge commit messages** summarizing the feature:
   ```
   Merge feature/party-role-management

   - Add Party aggregate root with role-based access control
   - Implement CRUD operations for Party management
   - Add validation for duplicate party names
   ```

## Team Workflow with Git Worktrees

### Overview
For parallel work across multiple services, use a **Lead Agent + Service Agents** model with isolated worktrees.

### Agent Roles

**Lead Agent:**
- Coordinates service agents
- Handles rebasing and merging when agents complete work
- Removes worktrees after successful merges
- Reports final status

**Service Agents (one per service):**
- Work in isolated worktrees on feature branches
- Enrich tests/implement features for their assigned service
- Commit using Conventional Commits format
- Notify lead upon completion

### Workflow Steps

1. **Create feature branches per service:**
   ```bash
   git checkout -b feature/extreme-tests-audit
   git checkout -b feature/extreme-tests-catalog
   git checkout -b feature/extreme-tests-lending
   git checkout -b feature/extreme-tests-party
   ```

2. **Service agents work in worktrees** (isolated mode):
   - Each agent works on their service's feature branch
   - Regular commits with focused, atomic changes
   - Push commits to their feature branch

3. **Lead rebases each branch:**
   ```bash
   git checkout feature/extreme-tests-{service}
   git fetch origin
   git rebase origin/master
   ```

4. **Lead merges with --no-ff:**
   ```bash
   git checkout master
   git merge --no-ff feature/extreme-tests-{service} -m "Merge feature/extreme-tests-{service}

   - Add extreme scenario tests for edge cases
   - Add boundary condition tests
   - Add stress and concurrency tests"
   ```

5. **Lead cleans up worktrees** after all merges:
   ```bash
   git worktree remove .claude/worktrees/feature/extreme-tests-{service}
   ```

### Commit Message Format for Team Work

**Service agent commits:**
```
test(audit-api): add extreme scenario tests for event repository

- Empty collection and null input handling
- Concurrent event processing (1000 threads)
- Database connection failure scenarios
```

**Lead merge commits:**
```
Merge feature/extreme-tests-audit

- Add 96 extreme scenario tests for event repository
- Add retention job stress tests for millions of events
- Add RabbitMQ consumer edge case tests
- Add controller boundary and pagination tests
```

### Example: Extreme Test Scenario Teams

This workflow was successfully used to add 400+ extreme scenario tests across all services:

| Service | Tests Added | Files Modified |
|---------|-------------|----------------|
| Audit.API | 96 | 4 new test files |
| Catalog.API | 50+ | 4 existing files |
| Lending.API | 51 | 2 existing files |
| Party.API | 214 | 3 new test files |

All tests passed with 0 failures. Some tests skipped for InMemory database limitations (would pass with PostgreSQL).

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
