# Database Schema & Concurrency

## The Core Problem: The Lost Update
In a distributed environment (e.g., a web API running on 5 different servers), C# in-memory `lock` statements are useless.
If two users try to purchase the last laptop at the exact same millisecond:
1. Server A reads `Stock = 1`.
2. Server B reads `Stock = 1`.
3. Server A updates `Stock = 0`.
4. Server B updates `Stock = 0`.
Both users are told they successfully purchased the laptop, but the database only subtracted 1. This is the **Lost Update** problem.

## Concurrency Control Strategies

### 1. Pessimistic Locking
*   **Mechanism**: The database physically locks the row upon reading.
*   **SQL**: `SELECT * FROM Products WHERE Id = 1 FOR UPDATE;`
*   **Trade-offs**: Guarantees zero collisions, but absolutely destroys throughput and introduces high risks of deadlocks. Rarely used in high-scale web APIs unless required by legacy financial systems.

### 2. Optimistic Concurrency Control (OCC)
*   **Mechanism**: Assume collisions are rare. Do not lock the row on read. Instead, add a `Version` (int) or `RowVersion` (timestamp) column to the schema. When saving, atomically verify the version hasn't changed.
*   **SQL (Compare-And-Swap)**:
    ```sql
    UPDATE Products 
    SET Stock = 0, Version = 2 
    WHERE Id = 1 AND Version = 1;
    ```
*   **Execution**:
    *   If Server A executes this first, it succeeds (1 row affected).
    *   When Server B executes it a millisecond later, the `WHERE Version = 1` clause fails (because Server A made it 2). 
    *   Server B gets `0 rows affected`.
*   **Entity Framework Core**: Automatically detects `0 rows affected` and throws a `DbUpdateConcurrencyException`.

## How to Handle the Concurrency Exception in C#
When OCC detects a collision, your application must handle it. 
1. **Client-side resolution**: Return HTTP 409 Conflict and tell the user "The data changed, please refresh and try again." (Good for Wiki edits).
2. **Server-side retry**: Catch the exception, re-fetch the latest data from the DB, re-apply the business logic against the fresh data, and try saving again. (Good for automated background jobs or high-contention counters).
