# Curriculum: Mid/Senior LLD & System Design

## Phase 1: Architecture & Domain Modeling
1. **SOLID, DI, and Clean Architecture**: Isolating the core domain from infrastructure, applying IoC effectively.
2. **Domain-Driven Design (DDD) Basics**: Entities, Value Objects, Aggregates, and Repositories in C#.

## Phase 2: Data & Contracts
3. **Database Schema & Concurrency**: Lost Updates, Optimistic vs Pessimistic locking.
4. **Database Design & Storage**: SQL vs NoSQL, Normalization vs Denormalization, Indexing strategies.
5. **API Design & Contracts**: RESTful best practices, idempotency keys, handling partial failures and retries.

## Phase 3: Advanced Machine Coding
6. **In-Memory Message Queue**: Implementing Pub/Sub with Topics, Partitions, and Consumer Groups.
7. **Task Scheduler / Job Executor**: Managing delayed/recurring execution and background thread pools.
8. **Complex Domains (e.g., Splitwise)**: Graph algorithms combined with rich domain modeling.

## Phase 4: Distributed Systems (HLD)
9. **CAP Theorem**: Network Partitions, Consistency vs Availability.
10. **Microservices vs Monoliths**: Service boundaries, API Gateways, and when to split.
11. **Event-Driven Architecture**: Distributed Sagas, Outbox Pattern, CQRS.
12. **Scaling & Partitioning**: Load Balancing, Database Sharding, Caching strategies.
