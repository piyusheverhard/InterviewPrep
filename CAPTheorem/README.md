# HLD: The CAP Theorem

## The Fundamental Rule of HLD
In High-Level System Design, the underlying assumption is that **hardware fails**. Hard drives corrupt, servers crash, and fiber optic cables get cut by backhoes. 
To survive this, data must be distributed across multiple physical nodes.

## The Problem: Network Partitions (P)
A Network Partition occurs when a communication link between two running nodes breaks. Node A is alive, and Node B is alive, but they cannot talk to each other.

When a Network Partition happens, a distributed database **must** choose between two opposing forces:

### 1. Consistency (C)
Every read receives the most recent write, or an error.
*   **The Trade-off:** If Node A cannot sync with Node B, Node A must reject all incoming writes (and potentially reads) by throwing an error to the user.
*   **Result:** The system becomes unavailable, but data integrity is mathematically flawless.
*   **Use Cases:** Financial ledgers, ATM withdrawals, Medical records.

### 2. Availability (A)
Every request receives a non-error response, without the guarantee that it contains the most recent write.
*   **The Trade-off:** If Node A cannot sync with Node B, Node A still accepts the user's password change. Now, Node A and Node B have conflicting data (Stale reads).
*   **Result:** The user is happy because the app works, but the backend must eventually merge conflicting data once the network partition is fixed.
*   **Use Cases:** YouTube Likes, Rate Limiters, Social Media Feeds.

## Eventual Consistency (The Reality of AP Systems)
In AP systems (like globally distributed Redis caches or Cassandra databases), data is synchronized asynchronously in the background. 
This means if you write to the European server, it might take 100ms for the Asian server to see the update. This is called **Replication Lag**. 

Senior engineers accept replication lag and occasional stale reads as a necessary trade-off to keep the system globally fast and available.
