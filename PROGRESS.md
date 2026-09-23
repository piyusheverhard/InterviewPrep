# Progress Log

## Resume Here

- **Cycle:** 2026-09-23 to 2026-10-06
- **Current day:** Day 1 — Parking Lot LLD diagnostic
- **Status:** Scheduled
- **Stage:** `not_started`
- **Level:** Begin at SDE1 core; apply SDE2 pressure tests after the core is coherent
- **Last checkpoint:** Repository audit and curriculum setup completed
- **Next action:** Present the Parking Lot prompt and wait for the candidate's clarification questions
- **Today's cap:** One problem; stop after feedback or one focused retry
- **Revision due:** None yet

## Current State

Repository audit completed on 2026-09-23. The dated two-week cycle is ready to begin. No exercise in the new cycle has been evaluated yet.

## Demonstrated Strengths

- Recognizes shared-state races and the need to protect compound data structures with one synchronization boundary.
- Understands why an LRU `Get` mutates state and why partition-local locks improve concurrency.
- Models nondestructive message consumption using independent offsets.
- Uses dependency inversion, ports/adapters, factories, strategies, and composable behavior in design discussions.
- Understands value-object immutability, domain invariants, optimistic concurrency, idempotency keys, and unique-key deduplication at a conceptual level.
- Has relevant production context in C#, .NET, AWS SQS, event-driven services, microservices, migrations, concurrency, and distributed systems.

## Highest-Priority Gaps to Validate or Improve

- Produce compiling, internally consistent interview code with constructor validation and preserved invariants.
- Identify atomic and transactional boundaries, especially across business updates and idempotency records.
- Design recovery from crashes, abandoned work, duplicate in-flight requests, and partial failures.
- Use deterministic dependencies such as clocks when they materially improve reasoning and verification.
- Build complete HLD answers: requirements, estimates, APIs, schema, architecture, bottlenecks, failures, observability, and evolution.
- Make trade-offs concrete and calibrate depth for SDE1 versus SDE2.

These are evidence gaps from the repository, not assumptions about concepts the candidate has never encountered.

## Existing Work Status

### Conceptually Covered

- GoF patterns: Builder, Factory Method, Decorator, Facade, Chain of Responsibility, Strategy, Observer, Command, and State
- Concurrency foundations and common .NET synchronization primitives
- CAP theorem and eventual consistency basics
- Clean Architecture and dependency inversion basics
- DDD entity and value-object basics
- Optimistic concurrency and idempotency basics

### Implemented but Needs Repair or Re-evaluation

- Tic-Tac-Toe
- Parking Lot
- LRU Cache and thread-safe LRU Cache
- Sliding-window Rate Limiter
- Notification Service
- Receipt Dispatcher
- Bank Account and Money domain model
- Ride assignment with concurrency control
- Stripe-style webhook processor
- In-memory simple and partitioned message brokers

### Not Yet Attempted in This Repository

- Bounded queue and thread pool
- Dependency-aware job scheduler
- Rule engine
- Twitter/news feed
- Splitwise
- Complete URL shortener HLD
- Uber Eats homepage HLD
- Workflow system HLD
- Billion-user degrees-of-separation HLD
- Transactional outbox, saga, and CQRS exercises

## Session Log

Add one entry after each evaluated session:

```text
Date / Exercise:
Level attempted: SDE1 or SDE2
What was demonstrated:
What broke under challenge:
One correction to retain:
Rubric scores:
Next action:
```
