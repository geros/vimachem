# Assignment Requirements

This page documents the original requirements provided for the Library Management System interview assignment (Version 3.0).

## Scope

Design and implement a Library Management System that will be used by a librarian to manage parties, customers and authors, books, and their reservations.

The system should be implemented using .NET (latest LTS). The system must follow SOLID principles, be containerized, and support event-driven communication.

## Section 1

The librarian should be able to perform the following actions:

- Manage parties and their roles
- Add books on behalf of Authors
- Manage book reservations on behalf of Customers

### Functional Requirements

#### Parties & Role Management

- CRUD parties
- CRUD roles:
    - Author
    - Customer
- A party can belong to both roles

#### Book & Category Management

- CRUD books
- CRUD categories:
    - Fiction
    - Mystery
- Track book availability:
    - By ID
    - By Title
- Support borrowing and returning books
- A book copy can be borrowed by only one Customer at a time

#### Borrowing Visibility

- The system must be able to return:
    - A list of book titles along with the Customers who have currently borrowed them

### Technical Requirements

- Apply Microservice architecture
- Use a relational database for transactional data
- Add initial data
- Ensure clear domain ownership
- Include unit tests
- All components must be containerized
- Provide a docker-compose setup

## Section 2

The librarian should be able to retrieve events regarding a book, a party, or a reservation.

Extend the system to support asynchronous communication, auditing, background processing, and scalable read models.

### Functional Requirements

#### Event Publishing

- All actions must be published as events
- Events should include at least:
    - Entity identifiers
    - Action type
    - Timestamp

#### Event History

- Persist events for querying and auditing
- Expose read-only endpoints to:
    - Retrieve user-related events
    - Retrieve book-related events
- Responses must be paginated

#### Data Retention

- Events older than 1 year must be deleted automatically
- The cleanup logic must be isolated from request handling

### Technical Requirements

- Use RabbitMQ
- Use a non-relational database for event storage
- Apply retry and error-handling strategies for message processing

## Deliverables

- Source code (GitHub repository)
- docker-compose.yml
- README including:
    - How to run the system
    - Architecture overview
    - Describe decisions and trade-offs
