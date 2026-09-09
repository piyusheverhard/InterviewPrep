---

# Concurrency & Multithreading: Expanded Master Notes

## 1. The Foundations: OS & Memory Architecture

To understand concurrency, you must understand where data lives and how the OS manages execution.

* **Process (The Container):** The heavy, isolated OS container for your application. It owns its own Virtual Memory (The Heap). Process A cannot read Process B's memory.
* **Thread (The Worker):** The actual execution path managed by the OS Scheduler.
* **The Context Switch:** A CPU core can only run one thread at a time. To simulate multitasking, the OS pauses a thread, saves its exact state (CPU registers) to RAM, loads another thread's state, and resumes. This is highly expensive (microseconds). High-performance concurrency is about minimizing context switches.


* **The Stack (Thread-Private):** Fast, sequential memory. Every thread gets its own private Stack (~1MB). Local variables declared inside a method live here. **Data on the Stack is inherently thread-safe** because no other thread can reach it.
* **The Heap (Process-Shared):** Large, chaotic memory managed by the Garbage Collector. All reference types (`new Class()`) live here. Because all threads share the Heap, **the Heap is where race conditions happen.**

## 2. The Core Problem: The Illusion of Atomicity

Code that looks like one step in C# is multiple steps on the CPU.

* `count++` is actually three instructions: **Read** (fetch from RAM to CPU register) ➔ **Modify** (add 1) ➔ **Write** (save register back to RAM).
* **The Race Condition:** Thread A reads `5`. The OS context-switches. Thread B reads `5`, modifies to `6`, writes `6`. The OS switches back. Thread A modifies its stale `5` to `6` and writes `6`. You lost an update because the operations interleaved.
* **The `static` Trap:** Declaring `static int count` binds the variable to the Type itself, forcing it onto the shared Heap. If you don't use `static` and instantiate a new object *inside* the thread, it stays isolated, and no race condition occurs.

## 3. The Low-Level Synchronization Toolkit

Tools to enforce atomicity, ordered from hardware to OS level.

### A. `Interlocked` (The Hardware Sniper)

* **Mechanism:** Uses specific CPU instructions (like `LOCK XADD` on x86). It sends a signal to the motherboard to lock the physical hardware cache line for a fraction of a nanosecond.
* **Speed:** ~2-10 nanoseconds. Bypasses the OS entirely. Zero context switching.
* **Constraints:** Only works on single variables (int, long, pointer swaps).
* **The `ref` Rule:** You must pass the variable by reference (`ref`). Therefore, you **cannot use Properties** (which are just getter/setter methods). You must point `Interlocked` at a raw private backing Field.

### B. `lock` / `Monitor` (The OS Traffic Cop)

* **Mechanism:** Modifies a hidden bit in a .NET object's "Object Header" (the SyncBlock). If the lock is taken, the OS suspends the thread (Context Switch) and moves it to a Wait Queue.
* **Syntactic Sugar:** `lock(_sync)` is automatically rewritten by the compiler to `Monitor.Enter` wrapped in a `try/finally` block.
* **Why `try/finally`?** If an exception occurs inside a lock and you don't release it, every other thread will freeze forever waiting for it. This is a fatal **Deadlock**.
* **Starvation Risk:** C# locks are **Unfair**. An awake thread can steal a newly released lock before the OS has time to wake up a sleeping thread, potentially starving the sleeping thread under heavy load.

### C. `ReaderWriterLockSlim` (The Library)

* **Mechanism:** Maintains two queues. Multiple threads can acquire a Read lock simultaneously. Only one thread can acquire a Write lock (and only when zero readers are active).
* **Danger:** If you use a Read lock for an operation that actually mutates state (like a `Get` in an LRU cache, which moves items in a LinkedList), you will corrupt your data structure.

### D. `SemaphoreSlim` (The User-Mode Bouncer)

* **Mechanism:** Throttles concurrency. Initialized with a capacity (e.g., allow 3 threads).
* **Warning:** Unless capacity is 1, **it does not protect against race conditions.** It only limits throughput. If 3 threads are let in, they can still collide on shared data.
* **`Semaphore` (Kernel) vs `SemaphoreSlim`:** Standard `Semaphore` is an OS-level object. It can be named (`"Global\MyAPI"`) to lock resources *across completely different applications* on the same server, but it is extremely slow. `SemaphoreSlim` is fast, pure .NET, but limited to a single process.

## 4. Modern .NET: Async, Tasks, and the State Machine

* **Task != Thread:** A Thread is an OS execution path. A `Task` is just a C# data structure on the Heap representing a promise that work will finish eventually.
* **CPU-Bound vs I/O-Bound:**
* Calculating Pi is CPU-bound (Requires a physical thread).
* Waiting for an SQL database is I/O-bound (Does *not* require a physical thread; the network card/OS handles the waiting).


* **The State Machine:** When you `await` an I/O call, the C# compiler saves your local variables to the Heap and **releases the physical thread back to the ThreadPool**. When the DB replies, the OS wakes up *any* available thread to resume the method.
* **The `lock` Restriction:** Because Thread 1 might enter an `await`, but Thread 5 might resume it, **you cannot put an `await` inside a `lock{}` block.** Locks belong to the thread that opened them.
* **The Async Lock Solution:** Use `SemaphoreSlim(1, 1)` and `await semaphore.WaitAsync()`.
* **The Performance Trap:** NEVER use `WaitAsync()` for pure, in-memory CPU work (like swapping cache pointers). Creating a `Task` object takes 100x longer than letting a thread spin-wait for 5 nanoseconds on a standard `lock`.

## 5. The "Thundering Herd" & AsyncLazy Pattern

* **The Problem (Cache Stampede):** 10,000 users request `"User_123"`. It's not in the cache. All 10,000 threads query the database simultaneously. The database crashes.
* **The Bad Fix:** Putting a `lock` around the database call freezes all 10,000 threads (Thread Pool Starvation), crashing your web server.
* **The Architecture Fix (AsyncLazy):** Instead of caching the `string`, you cache the `Task<string>`.
* Use `ConcurrentDictionary<string, Task<string>>`.
* Thread 1 hits `GetOrAdd`, fires the DB query, and inserts the pending `Task`.
* Threads 2 to 10,000 hit `GetOrAdd` and receive the *exact same pending Task*.
* They all `await` it. Their threads are freed. Only exactly ONE database query was executed.
* *Gotcha:* If the DB query throws an exception, you must evict the faulted Task from the dictionary, or it will be permanently cached as an error.



## 6. Application: The Thread-Safe LRU Cache

* **The Dual Data Structure Trap:** An O(1) LRU Cache requires a `Dictionary` (for O(1) lookup) AND a `LinkedList` (for O(1) eviction/ordering).
* **Why `ConcurrentDictionary` Fails Here:** It only protects the dictionary. If multiple threads mutate the `LinkedList` simultaneously, the `next/prev` pointers tear, causing infinite loops. You must wrap *both* structures in a single `lock`.
* **Why `Get` is a Write:** Every `Get` moves the accessed item to the head of the LinkedList. Because it mutates state, you must use an exclusive lock, not a Reader lock.
* **Scaling via Lock Striping (Sharding):** A single lock becomes a bottleneck at millions of ops/sec.
* *Solution:* Create an array of 32 completely independent LRU caches (shards), each with its own `lock`.
* *Routing:* Route requests using `key.GetHashCode() % 32`. Thread A locking Shard 5 has zero impact on Thread B reading Shard 12.


* **The Negative Hash Bug:** In .NET, `GetHashCode()` frequently returns negative numbers. `-14 % 32 = -14`. This causes an `IndexOutOfRangeException` when accessing the shard array.
* *Fix:* Always use `Math.Abs(key.GetHashCode()) % numberOfShards`.



## 7. Concurrency Control Strategies (Types of Concurrency)

When multiple threads (or distributed servers) need to read and write to the same data, you must choose a "Concurrency Control" strategy to handle conflicts. There are three primary models:

### A. Pessimistic Concurrency (Assume the Worst)

**The Philosophy:** *"Conflicts will happen constantly. I must prevent them before they start by locking the data down completely."*
**The Analogy:** A single-occupancy public restroom. You walk in, lock the door, do your business, and unlock it. If 10 people arrive, they must wait in a blocked line.

* **How it works:** A thread requests exclusive access to the data. No other thread can read or write to this data until the lock is released.
* **In .NET:** Using `lock`, `Monitor`, `ReaderWriterLockSlim`, or `SemaphoreSlim`.
* **In Databases:** Using SQL commands like `SELECT ... FOR UPDATE`, which physically locks the row on the hard drive.
* **When to use:** High Contention. When you have 50 threads constantly fighting to update the exact same counter or financial balance.
* **Pros:** 100% safe. Data collisions are literally impossible. Easy to reason about.
* **Cons:** Massive performance bottlenecks. High risk of deadlocks. If a thread crashes or a DB connection drops while holding the lock, the system freezes.

### B. Optimistic Concurrency (Forgiveness over Permission)

**The Philosophy:** *"Conflicts are rare. Let everyone read the data and do their work freely. We will only check for a collision at the exact millisecond they try to save."*
**The Analogy:** Git Version Control. You and a coworker pull the `main` branch (v1). You both work locally. You push your changes first (v2). When your coworker tries to push their v1-based changes, Git rejects it (Merge Conflict). They must pull your v2, resolve it, and try again.

* **How it works (The CAS Loop):**
1. **Read:** Note the current state or "version" (e.g., `v1`).
2. **Work:** Do the expensive math/updates on a private copy.
3. **Verify & Swap:** Atomically check: *Is the database/memory still at `v1`?*
* If YES: Overwrite it to `v2`.
* If NO: Throw away the work, re-read the new state, and try again (Retry Logic).




* **In .NET:** The `Interlocked.CompareExchange` Compare-And-Swap (CAS) loop.
* **In Web APIs:** Using HTTP `ETag` headers. (Client sends `If-Match: "v1"`. Server rejects with `412 Precondition Failed` if data is already at `v2`).
* **In Databases (EF Core):** Adding a `[Timestamp]` or `RowVersion` column to an SQL table. EF Core will automatically throw a `DbUpdateConcurrencyException` if the row version changed behind your back.
* **When to use:** Low Contention. (e.g., 10,000 threads reading data, only 1 occasionally updating it) or when updating distributed systems where holding an active lock over a network is dangerous.
* **Pros:** Incredible throughput. No threads are ever put to sleep waiting in line.
* **Cons:** You *must* write retry logic. If contention is actually high, the CPU thrashes itself endlessly calculating work that gets thrown away.

### C. Multi-Version Concurrency Control (MVCC)

**The Philosophy:** *"Readers never block writers, and writers never block readers."*
**The Analogy:** Wikipedia. Millions of people can read the live, published article without any delay, even while you are spending an hour drafting a massive edit in the background. When you hit "Publish," the global pointer instantly flips to your new version.

* **How it works:** Data is treated as **immutable**. Instead of mutating data in-place, the writer creates a brand-new copy of the data, applies changes to the copy, and then executes a single atomic pointer swap to replace the old dataset with the new dataset.
* **In .NET:** The `System.Collections.Immutable` namespace (e.g., `ImmutableDictionary`). To add an item, you create a new dictionary in the background and use `Interlocked.Exchange` to swap the pointer.
* **In Databases:** This is the default architecture of **PostgreSQL**, and can be enabled in SQL Server via `READ COMMITTED SNAPSHOT` isolation.
* **When to use:** Extreme read-heavy workloads where you cannot afford to pause readers just because a background worker is updating the cache.
* **Pros:** Maximum read performance. Zero blocking.
* **Cons:** High memory consumption and Garbage Collection pressure, because you are duplicating data structures for every write.

---

### Interview Cheat Sheet: Which one to pick?

* **Updating an LRU Cache linked list in RAM?** -> **Pessimistic** (`lock`). Memory is fast; locks take nanoseconds.
* **Updating a User Profile in an SQL Database?** -> **Optimistic** (`RowVersion`). Humans rarely edit their profile at the exact same millisecond. Don't lock DB rows unnecessarily.
* **Refreshing a massive configuration dictionary every hour?** -> **MVCC** (`ImmutableDictionary`). Let the 10,000 API requests read the old config seamlessly while the new one builds in the background.
