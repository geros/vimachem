# Assignment Overview

This documentation covers the original interview assignment requirements and how they were implemented.

## Purpose

This project was developed as a technical interview assignment for Vimachem. It demonstrates proficiency in:

- .NET microservices architecture
- Domain-Driven Design (DDD)
- Event-driven communication
- Polyglot persistence
- API design and implementation
- Testing strategies

## Assignment Structure

- [Requirements](requirements.md) - Original assignment requirements
- [Implementation](implementation.md) - How requirements were implemented

## Project Scope

The assignment required building a Library Management System with:

1. **Party Management** - People and roles (Author, Customer)
2. **Catalog Management** - Books and categories
3. **Lending Operations** - Borrow and return workflows
4. **Audit Trail** - Event store for all activities

## Key Deliverables

- Four microservices with clear domain boundaries
- REST APIs with comprehensive endpoints
- Event-driven communication via RabbitMQ
- Relational databases (PostgreSQL) for transactional data
- Document database (MongoDB) for event storage
- Unit and integration tests
- Docker Compose setup for easy deployment

## Evaluation Criteria

The implementation was evaluated on:

| Criteria | Weight | Description |
|----------|--------|-------------|
| Architecture | High | Clean separation of concerns, DDD principles |
| Code Quality | High | Readability, maintainability, testability |
| Functionality | High | All requirements implemented correctly |
| Testing | Medium | Comprehensive test coverage |
| Documentation | Medium | Clear README and API documentation |

## Time Investment

Estimated development time: 40-60 hours

- Architecture design: 4 hours
- Service implementation: 24 hours
- Testing: 12 hours
- Documentation: 4 hours
- Docker/deployment: 4 hours

## Technologies Used

- .NET 10
- PostgreSQL 16
- MongoDB 7
- RabbitMQ 3
- Entity Framework Core
- FluentValidation
- Polly
- xUnit
- Docker & Docker Compose

## Next Steps

- Review the [detailed requirements](requirements.md)
- See how they were [implemented](implementation.md)
- Explore the [API documentation](../api/index.md)
