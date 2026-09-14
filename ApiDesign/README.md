# API Design & Idempotency

## The Core Problem: Network Unreliability
In distributed systems, networks drop packets. A client might send a `POST /payments` request, the server successfully processes it, but the `200 OK` response is lost in transit. 
The client, seeing a timeout, assumes the request failed and retries. Without protection, the server processes the payment a second time, double-charging the customer.

## The Solution: Idempotency Keys
An API is **Idempotent** if making the exact same request multiple times leaves the server in the exact same state as making it once.

*   **Natively Idempotent HTTP Verbs**: `GET`, `PUT`, `DELETE`.
*   **Non-Idempotent Verbs**: `POST`, `PATCH`.

To make a `POST` request idempotent, the client must generate a unique `Idempotency-Key` (a GUID) and send it as an HTTP Header.

### The Inbox Pattern (Webhook Processing)
When receiving webhooks from providers like Stripe (which use "At-Least-Once" delivery and will retry if they don't get a fast 200 OK), the safest approach is the **Inbox Pattern**:

1.  **Receive Request**: Stripe sends `POST /webhooks` with an `eventId`.
2.  **Persist & Deduplicate**: Attempt to insert the `eventId` into an `EventInbox` database table. The `eventId` column must have a **Unique Constraint**.
    *   If it succeeds, it's a new event.
    *   If it throws a unique constraint violation, it's a retry. Return `200 OK` immediately.
3.  **Return 200 OK Fast**: Do not process the heavy business logic synchronously. Return `200 OK` to Stripe so they close the connection and stop retrying.
4.  **Async Processing**: A background worker (or a message queue like SQS) picks up the pending events from the `EventInbox` table and executes the business logic (e.g., `UpdateUserPlan`).
5.  **Transactional Integrity**: When the background worker finishes, it must update the business tables and mark the `EventInbox` row as `Processed` inside a **Single Database Transaction**.
