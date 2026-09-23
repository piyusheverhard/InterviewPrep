# Two-Week SDE1/SDE2 Interview Curriculum

## Cycle Calendar

This cycle runs from **2026-09-23 through 2026-10-06**. September 23 is already end-of-day, so Day 1 contains one problem only and should stop after feedback or one focused retry.

| Day | Date | Primary session | Target load |
|---:|:---|:---|:---|
| 1 | Sep 23 | Parking Lot LLD diagnostic | 45-60 min; one problem maximum |
| 2 | Sep 24 | URL Shortener HLD | 75-90 min |
| 3 | Sep 25 | Bounded Queue and Thread Pool | 75-90 min |
| 4 | Sep 26 | Idempotent Webhook Processing | 75-90 min |
| 5 | Sep 27 | Dependency-Aware Job Scheduler | 75-90 min |
| 6 | Sep 28 | Uber Eats Homepage HLD | 75-90 min |
| 7 | Sep 29 | Consolidation and recall | 30-45 min |
| 8 | Sep 30 | Rule Engine LLD | 75-90 min |
| 9 | Oct 1 | SQS and Kafka deep dive | 60-75 min |
| 10 | Oct 2 | Workflow System HLD | 75-90 min |
| 11 | Oct 3 | Ticket/Inventory data design | 75-90 min |
| 12 | Oct 4 | Degrees of Separation HLD | 75-90 min |
| 13 | Oct 5 | Timed mixed mock | 90 min |
| 14 | Oct 6 | Final mock and calibration | 45-60 min |

## Strategy

This cycle uses an interview-first loop. Existing notes already provide breadth; the priority is converting that knowledge into clear, correct decisions under time pressure.

Each normal session is approximately 90 minutes:

1. **Cold prompt (5 minutes):** The interviewer presents the problem without teaching the solution.
2. **Clarification and scope (10 minutes):** The candidate identifies functional requirements, constraints, and what can be deferred.
3. **Candidate-led design (20 minutes):** The candidate explains entities, APIs, data flow, invariants, and the simplest viable design.
4. **Interviewer pressure test (15 minutes):** Challenge concurrency, failures, scale, extensibility, and operational behavior.
5. **Implementation or deep dive (25 minutes):** Implement the critical path for LLD, or deepen data model and architecture for HLD.
6. **Feedback and retry (15 minutes):** Score the attempt, teach only the exposed gaps, and have the candidate restate or repair the weak section.

Do not reveal a complete solution before the candidate attempts the problem. If the candidate lacks a prerequisite, temporarily switch to teaching mode: concise concept brief, one concrete failure scenario, trade-offs, two or three knowledge-check questions, and then a fresh attempt.

## Evaluation Rubric

Score each dimension from 1 to 4 and record concrete evidence:

- Requirements and scope
- Correctness and invariants
- API and data modeling
- Extensibility and simplicity
- Concurrency and consistency
- Failure handling and recovery
- Testing or verification strategy
- Communication and trade-off reasoning

For SDE1, prioritize a correct core design, clean code, common edge cases, and clear explanation. For SDE2, additionally expect explicit concurrency boundaries, partial-failure recovery, data consistency, scale bottlenecks, observability, and evolution of the design.

## Week 1: Correctness and Interview Structure

### Day 1 — Sep 23 — LLD Diagnostic: Parking Lot

- Clarify scope before naming patterns.
- Model spot allocation, tickets, entry/exit invariants, and pricing.
- Discuss duplicate entry, concurrent allocation, and extensible allocation policy.
- Goal: establish a baseline for LLD communication and executable correctness.

### Day 2 — Sep 24 — HLD Framework: URL Shortener

- Functional and non-functional requirements.
- Back-of-the-envelope traffic and storage estimates.
- API, identifier generation, schema, redirect path, caching, and multi-region trade-offs.
- Goal: learn one repeatable HLD sequence rather than memorize an architecture.

### Day 3 — Sep 25 — Concurrency: Bounded Queue and Thread Pool

- Blocking semantics, producer/consumer coordination, fairness, shutdown, and cancellation.
- Compare `Monitor`, semaphores, and channels at the design level.
- Goal: translate concurrency knowledge into a small correct implementation.

### Day 4 — Sep 26 — API Reliability: Idempotent Webhook Processing

- Unique constraints, transaction boundaries, duplicate in-flight events, leases, retries, and response semantics.
- Relate the design to SQS at-least-once delivery and visibility timeouts.
- Goal: repair the gap between knowing the inbox pattern and designing crash-safe processing.

### Day 5 — Sep 27 — Machine Coding: Dependency-Aware Job Scheduler

- DAG validation, ready queue, bounded parallelism, job states, failure propagation, retry, and cancellation.
- Goal: combine modeling, concurrency, and extensibility in one exercise.

### Day 6 — Sep 28 — HLD: Uber Eats Homepage

- Location model, geospatial search, restaurant/menu data, ranking, fan-out, caching, and freshness.
- Goal: practice storage choices and read-heavy system design.

### Day 7 — Sep 29 — Light Consolidation

- Review feedback from Days 1-6.
- Re-explain the two weakest decisions without notes.
- Produce one short checklist each for LLD and HLD.
- No new full problem and no long coding session.

## Week 2: SDE2 Depth and Mock Performance

### Day 8 — Sep 30 — LLD: Rule Engine

- Rule composition, precedence, validation, explainability, versioning, and safe extension.
- Goal: demonstrate patterns through requirements rather than forcing named patterns.

### Day 9 — Oct 1 — Messaging Deep Dive: SQS and Kafka

- Delivery semantics, partitioning, ordering, consumer groups, acknowledgements, retries, DLQs, poison messages, and backpressure.
- Use the repository broker as a critique exercise rather than rebuilding Kafka.
- Goal: convert work experience into concise interview-quality trade-off explanations.

### Day 10 — Oct 2 — HLD: Workflow System

- Workflow definition, state persistence, task dispatch, timers, retries, idempotency, recovery, and versioning.
- Goal: integrate event-driven architecture, orchestration, and failure handling.

### Day 11 — Oct 3 — Data Design: Ticket Booking or Inventory Reservation

- Schema, indexes, isolation, optimistic versus pessimistic concurrency, reservation expiry, and payment consistency.
- Goal: strengthen transaction and database reasoning through a contention-heavy domain.

### Day 12 — Oct 4 — HLD: Billion-User Degrees of Separation

- Graph representation, traversal limits, bidirectional search, partitioning, caching, privacy, and approximate answers.
- Goal: handle an unfamiliar large-scale problem systematically.

### Day 13 — Oct 5 — Timed Mixed Mock

- One 45-minute LLD/machine-coding round.
- One 45-minute HLD round.
- Minimal intervention during the attempts; score both with the common rubric.

### Day 14 — Oct 6 — Final Mock and Next-Cycle Decision

- Repeat one previously weak problem with changed requirements.
- Compare evidence against the Day 1 baseline.
- Choose the next two-week cycle from observed gaps rather than continuing automatically.
- Keep the day lighter if fatigue would reduce the quality of the mock.

## Lightweight Verification Policy

Formal test projects are not required by default. During interview exercises, verify behavior with:

- a short table of normal, boundary, invalid, and concurrent cases;
- a few assertions or a tiny in-file harness when code execution matters;
- explicit invariant checks and complexity analysis;
- a concurrency interleaving walkthrough for race-sensitive code.

Use a real test project when the learning target specifically depends on repeatable concurrency behavior, regression across several iterations, or testability as a design requirement. Do not spend preparation time on test framework setup merely to imitate production repository structure.

## Later Backlog

- Twitter/news feed and Spotify LLD
- Splitwise or expense sharing
- File system shell (`mkdir`, `pwd`, wildcard `cd`)
- Database normalization, denormalization, and indexing drills
- Transactional outbox, sagas, and CQRS
- Microservice boundaries and monolith decomposition
- Multi-region consistency and disaster recovery
