# Domain-Driven Design (DDD) Basics

## The Core Problem: Anemic Domain Models
In traditional layered architecture, classes are often designed as "dumb" data bags with public getters and setters (e.g., `Order.Status { get; set; }`). 
The business logic that governs state changes is smeared across massive "Service" classes. 
This is the **Anemic Domain Model** anti-pattern. It allows any developer to accidentally put an object into an invalid state (e.g., shipping an unpaid order).

## The Solution: Rich Domain Models
In DDD, we push the business logic directly into the models themselves. The model rigorously defends its own rules (Invariants).

### 1. Entities
*   **Definition**: An object defined by its **Identity** (an ID), not its attributes. User #123 is the same user even if their email changes.
*   **Equality**: `user1 == user2` ONLY if `user1.Id == user2.Id`.
*   **State Management**: Properties have `private set;`. State is mutated only through explicit, intention-revealing methods (e.g., `order.Ship()`, not `order.Status = Shipped`).

### 2. Value Objects
*   **Definition**: An object defined entirely by its **Attributes**, not identity. It has no ID. 
*   **Equality**: `moneyA == moneyB` if `moneyA.Amount == moneyB.Amount` AND `moneyA.Currency == moneyB.Currency` (Structural Equality).
*   **Immutability**: **Value Objects are strictly immutable.** Once created, they can never change. If you want to modify a Value Object, you must create and return a brand new instance. 

## Code Implementation (C#)
C# 9+ `record` types are perfect for Value Objects because they provide structural equality out of the box.

```csharp
// Value Object: Immutable, Structural Equality
public record Money(decimal Amount, string Currency)
{
    // The constructor defends the invariant
    public Money { if (Amount < 0) throw new ArgumentException(); }
    
    // Immutability: Mutating methods return a NEW object
    public Money Add(Money other) => new Money(this.Amount + other.Amount, this.Currency);
}

// Entity: Identity Equality, protects its own state
public class BankAccount
{
    public string Id { get; init; }
    public Money Balance { get; private set; } // private set!

    public BankAccount(string id) { Id = id; Balance = new Money(0, "USD"); }

    public void Deposit(Money amount) 
    {
        // Reassigns the Value Object pointer
        Balance = Balance.Add(amount); 
    }
}
```

## The Role of Application Services
If the Entity holds the business logic, what does the Service do?
The Application Service becomes a pure **Orchestrator**:
1. Open a DB Transaction.
2. Fetch the Entity from the Repository.
3. Call the domain method on the Entity (`account.Deposit(amount)`).
4. Save the Entity.
5. Commit the Transaction.
