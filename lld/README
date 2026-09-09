# Low-Level Design (LLD) & System Design Interview Revision Guide

> A high-yield revision guide crafted for technical interviews. Combines GoF design patterns, architectural trade-offs, applied problem blueprints, and battle-tested live interview tactics.

---

## 📑 Table of Contents
1. [Quick-Reference Pattern Matrix](#1-quick-reference-pattern-matrix)
2. [Architecture: In-Process vs. Distributed Messaging](#2-architecture-in-process-vs-distributed-messaging)
3. [Core Design Patterns (GoF) with C# Implementations](#3-core-design-patterns-gof-with-c-implementations)
   - [Creational Patterns](#creational-patterns)
     - [Builder Pattern](#builder-pattern)
     - [Factory Method Pattern](#factory-method-pattern)
   - [Structural Patterns](#structural-patterns)
     - [Decorator Pattern](#decorator-pattern)
     - [Facade Pattern](#facade-pattern)
   - [Behavioral Patterns](#behavioral-patterns)
     - [Chain of Responsibility](#chain-of-responsibility)
     - [Strategy Pattern](#strategy-pattern)
     - [Observer Pattern (Pub/Sub)](#observer-pattern-pubsub)
     - [Command Pattern](#command-pattern)
     - [State Pattern](#state-pattern)
4. [Pattern Disambiguation: "Which Pattern When?"](#4-pattern-disambiguation-which-pattern-when)
   - [Adding Layers / Steps: Builder vs. Decorator vs. Chain of Responsibility](#adding-layers--steps-builder-vs-decorator-vs-chain-of-responsibility)
   - [Dynamic Behavior: Strategy vs. State](#dynamic-behavior-strategy-vs-state)
5. [Applied LLD Problem Playbooks](#5-applied-lld-problem-playbooks)
   - [Problem 1: Tic-Tac-Toe](#problem-1-tic-tac-toe)
   - [Problem 2: The Parking Lot](#problem-2-the-parking-lot)
   - [Problem 3: LRU (Least Recently Used) Cache](#problem-3-lru-least-recently-used-cache)
   - [Problem 4: Rate Limiter](#problem-4-rate-limiter)
   - [Problem 5: Notification Service](#problem-5-notification-service)
6. [Live Interview Strategy & Tactics](#6-live-interview-strategy--tactics)
   - [The "Verbal Interface" Hack](#the-verbal-interface-hack)
   - [Execution Over Abstraction (Priorities)](#execution-over-abstraction-priorities)
   - [45-Minute Interview Pacing Guide](#45-minute-interview-pacing-guide)
7. [Concurrency & Thread-Safety Quick Reference](#7-concurrency--thread-safety-quick-reference)

---

## 1. Quick-Reference Pattern Matrix

| Pattern | Type | Primary Intent | Codebase / LLD Example | Red Flag / Code Smell It Fixes |
| :--- | :--- | :--- | :--- | :--- |
| **Builder** | Creational | Construct complex objects step-by-step with fluent method chaining | [ParkingLotBuilder](file:///home/piyush/src/lld/ParkingLot.cs) | Telescoping constructors (`new X(true, 5, null, 10, false)`) |
| **Factory Method** | Creational | Delegate object creation to subclasses/methods based on runtime type | [NotificationService](file:///home/piyush/src/lld/NotificationServiceBetter.cs), `PlayerFactory` | Direct `new ConcreteClass()` spread throughout business logic |
| **Decorator** | Structural | Dynamically attach extra responsibilities/layers to an object at runtime | [ParkingLot.cs](file:///home/piyush/src/lld/ParkingLot.cs) Fee Calculator (`WeekendSurgeFee(BaseFee)`) | Class explosion from inheritance (`BaseFee`, `WeekendFee`, `HolidayWeekendFee`) |
| **Facade** | Structural | Provide a simple, unified interface over a complex subsystem | `OrderFacade.PlaceOrder()` | Client directly managing 5+ micro-services/subsystems |
| **Chain of Responsibility** | Behavioral | Pass a request down a handler chain; any handler can process, reject, or short-circuit | Ad Campaign Validation, ASP.NET Core Middleware | Deeply nested if/else validation blocks; tightly coupled handlers |
| **Strategy** | Behavioral | Encapsulate interchangeable algorithms behind a common contract | [TicTacToe.cs](file:///home/piyush/src/lld/TicTacToeBetter.cs) `IWinRule` (Row, Col, Diagonal) | Giant switch/case or if/else checks inside an execution loop |
| **Observer** | Behavioral | One-to-many event notification when subject state changes | [TicTacToe.cs](file:///home/piyush/src/lld/TicTacToeBetter.cs) Board move broadcasting to UI | Polling; publisher directly coupling to consumers |
| **Command** | Behavioral | Encapsulate a request as an object with undo/redo/queue capabilities | [TicTacToe.cs](file:///home/piyush/src/lld/TicTacToeBetter.cs) `PlacePieceCommand` | Direct mutation preventing rollback, undo, or command queuing |
| **State** | Behavioral | Allow an object to alter its behavior when internal state changes | Vending Machine, Order lifecycle (`Pending`, `Paid`, `Shipped`) | Massive `switch(state)` blocks repeated across every method |

---

## 2. Architecture: In-Process vs. Distributed Messaging

A critical distinction frequently probed in senior LLD and system design interviews:

| Dimension | `MediatR` (In-Process / Intra-Service) | Message Brokers (`Kafka`, `RabbitMQ`, `AWS SQS`) |
| :--- | :--- | :--- |
| **Boundary** | **Intra-service** (within a single OS process) | **Inter-service** (distributed across machines/networks) |
| **Transport** | In-memory pointer dereference & local CPU invocation | Network sockets (TCP, HTTP, AMQP, gRPC) |
| **Durability** | **Zero durability** (if process dies, queued messages vanish) | **Durable & Persistent** (persisted to disk/replicated across cluster) |
| **Scaling Model** | Scales vertically with CPU cores / thread pool | Scales horizontally across partitions, consumer groups, and nodes |
| **Mechanism** | Uses Dependency Injection (IoC) & Reflection to route requests | Pub/Sub topic brokers, partition consumer offsets, dead-letter queues |
| **Design Patterns Used** | Mediator Pattern + Observer Pattern | Enterprise Integration Patterns (Publish-Subscribe, Message Channel) |
| **Best Used For** | CQRS command/query decoupling inside a monolith or microservice | Asynchronous background jobs, distributed event-driven microservices |

> [!IMPORTANT]
> **Interview Soundbite**: "MediatR is an architectural tool to organize code within a single application process and decouple handlers from controllers. It does **not** replace Kafka or SQS, which provide distributed durability, consumer backpressure, and network fault tolerance."

---

## 3. Core Design Patterns (GoF) with C# Implementations

### Creational Patterns

#### Builder Pattern
* **Problem**: The **"Telescoping Constructor"** anti-pattern. Constructors with 8+ parameters where most are optional/nullable (`new ParkingLot(true, false, 5, 10, null, true)`).
* **Intent**: Assemble a complex object step-by-step. The client dictates *what* gets built, while the Builder handles *how* it is assembled and validated.
* **LLD Example ([ParkingLot.cs](file:///home/piyush/src/lld/ParkingLot.cs))**:

```csharp
public class ParkingLotBuilder
{
    private readonly List<IFloor> _floors = new();
    private IFeeCalculator _feeCalculator = new FlatRateFeeCalculator();

    public ParkingLotBuilder AddFloor(int numMotorcycle, int numCompact, int numLarge)
    {
        _floors.Add(new OneDimensionalFloor(numMotorcycle, numCompact, numLarge));
        return this; // Fluent API chaining
    }

    public ParkingLotBuilder WithSurgePricing(double multiplier)
    {
        _feeCalculator = new SurgeFeeDecorator(_feeCalculator, multiplier);
        return this;
    }

    public ParkingLot Build()
    {
        if (_floors.Count == 0)
            throw new InvalidOperationException("Parking lot must have at least one floor.");

        return new ParkingLot(_floors, _feeCalculator);
    }
}

// Client Usage:
var parkingLot = new ParkingLotBuilder()
    .AddFloor(numMotorcycle: 10, numCompact: 20, numLarge: 5)
    .AddFloor(numMotorcycle: 5,  numCompact: 10, numLarge: 2)
    .WithSurgePricing(1.5)
    .Build();
```

---

#### Factory Method Pattern
* **Problem**: The `new` keyword couples client execution logic to specific concrete implementations, violating the Open-Closed Principle (OCP) and Dependency Inversion Principle (DIP).
* **Intent**: Delegate object instantiation to a specialized factory method or interface.
* **LLD Example ([TicTacToeBetter.cs](file:///home/piyush/src/lld/TicTacToeBetter.cs) / [NotificationServiceBetter.cs](file:///home/piyush/src/lld/NotificationServiceBetter.cs))**:

```csharp
public enum PlayerType { Human, AiEasy, AiHard }

public static class PlayerFactory
{
    public static IPlayer Create(PlayerType type, string name, Piece piece)
    {
        return type switch
        {
            PlayerType.Human  => new HumanPlayer(name, piece),
            PlayerType.AiEasy => new RandomBotPlayer(name, piece),
            PlayerType.AiHard => new MinimaxBotPlayer(name, piece),
            _ => throw new ArgumentOutOfRangeException(nameof(type), $"Unsupported player: {type}")
        };
    }
}

// Client Usage:
IPlayer player2 = PlayerFactory.Create(PlayerType.AiHard, "AlphaBot", Piece.O);
```

---

### Structural Patterns

#### Decorator Pattern
* **Problem**: Adding optional or dynamic behaviors to a base object via inheritance leads to **"Class Explosion"** (`BaseFee`, `BaseFeeWithSurge`, `BaseFeeWithWeekendSurge`, etc.).
* **Intent**: Wrap a base object dynamically. Each layer implements the identical interface and augments behavior before or after delegating to the wrapped inner instance.
* **LLD Example ([ParkingLot.cs](file:///home/piyush/src/lld/ParkingLot.cs) Dynamic Pricing)**:

```csharp
public interface IFeeCalculator 
{ 
    decimal Calculate(Ticket ticket); 
}

// Base Component
public class FlatRateFeeCalculator : IFeeCalculator 
{
    public decimal Calculate(Ticket ticket) => 10.00m; // Base entrance fee
}

// Decorator 1: Duration-based Fee
public class DurationFeeDecorator : IFeeCalculator 
{
    private readonly IFeeCalculator _inner;
    private readonly decimal _hourlyRate;

    public DurationFeeDecorator(IFeeCalculator inner, decimal hourlyRate = 5.0m)
    {
        _inner = inner;
        _hourlyRate = hourlyRate;
    }

    public decimal Calculate(Ticket ticket)
    {
        decimal baseCost = _inner.Calculate(ticket);
        var hours = (decimal)(ticket.ExitTime - ticket.EntryTime).TotalHours;
        return baseCost + (Math.Ceiling(hours) * _hourlyRate);
    }
}

// Decorator 2: Weekend Surge Fee
public class WeekendSurgeDecorator : IFeeCalculator 
{
    private readonly IFeeCalculator _inner;
    public WeekendSurgeDecorator(IFeeCalculator inner) => _inner = inner;

    public decimal Calculate(Ticket ticket)
    {
        decimal baseCost = _inner.Calculate(ticket);
        bool isWeekend = ticket.ExitTime.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;
        return isWeekend ? baseCost * 1.5m : baseCost;
    }
}

// Client Usage: Composing multiple dynamic layers
IFeeCalculator pricingPipeline = new WeekendSurgeDecorator(
    new DurationFeeDecorator(
        new FlatRateFeeCalculator(), 
        hourlyRate: 5.0m
    )
);
decimal totalFee = pricingPipeline.Calculate(activeTicket);
```

---

#### Facade Pattern
* **Problem**: A client needs to perform a business operation, but doing so requires orchestrating multiple complex, lower-level subsystems with diverse APIs.
* **Intent**: Provide a unified, simple, high-level entry point that hides the complexity of underlying subsystems.
* **Real-World / System Design Example**:

```csharp
public class OrderProcessingFacade
{
    private readonly IInventoryService _inventory;
    private readonly IPaymentGateway _payment;
    private readonly INotificationClient _notifier;

    public OrderProcessingFacade(
        IInventoryService inventory, 
        IPaymentGateway payment, 
        INotificationClient notifier)
    {
        _inventory = inventory;
        _payment = payment;
        _notifier = notifier;
    }

    public async Task<bool> PlaceOrderAsync(string userId, string itemId, decimal amount)
    {
        if (!await _inventory.ReserveStockAsync(itemId)) return false;
        if (!await _payment.ChargeAsync(userId, amount))
        {
            await _inventory.ReleaseStockAsync(itemId); // Compensating action
            return false;
        }
        await _notifier.SendAsync(userId, "Your order has been placed successfully!");
        return true;
    }
}
```

---

### Behavioral Patterns

#### Chain of Responsibility
* **Problem**: A request must undergo a sequence of validations, checks, or steps. The sender should not know which handler executes, and any handler must have the authority to **short-circuit** (halt the chain immediately).
* **Intent**: Chain handler objects together in a pipeline. Each handler either processes and delegates to `Next`, or terminates execution.
* **LLD Example (Ad Campaign Request Validation / Middleware)**:

```csharp
public abstract class CampaignValidationHandler
{
    protected CampaignValidationHandler? Next;

    public CampaignValidationHandler SetNext(CampaignValidationHandler next)
    {
        Next = next;
        return next; // Allows fluent chaining: h1.SetNext(h2).SetNext(h3);
    }

    public abstract bool Validate(CampaignRequest request);
}

public class BudgetCheckHandler : CampaignValidationHandler
{
    public override bool Validate(CampaignRequest request)
    {
        if (request.Budget <= 0 || request.Budget > request.AccountBalance)
        {
            Console.WriteLine("Validation Failed: Insufficient campaign balance.");
            return false; // SHORT-CIRCUIT: Execution stops here!
        }
        return Next?.Validate(request) ?? true;
    }
}

public class ContentModerationHandler : CampaignValidationHandler
{
    public override bool Validate(CampaignRequest request)
    {
        if (request.Text.Contains("PROHIBITED_WORD"))
        {
            Console.WriteLine("Validation Failed: Inappropriate content.");
            return false; // SHORT-CIRCUIT
        }
        return Next?.Validate(request) ?? true;
    }
}

// Client Usage:
var pipeline = new BudgetCheckHandler();
pipeline.SetNext(new ContentModerationHandler());

bool isApproved = pipeline.Validate(newRequest);
```

---

#### Strategy Pattern
* **Problem**: A method contains a large, brittle `switch` or `if/else` block representing variations of an algorithm (e.g., checking win conditions across row, column, diagonal, or custom board shapes).
* **Intent**: Define a family of interchangeable algorithms, encapsulate each into its own class, and make them swappable at runtime.
* **LLD Example ([TicTacToeBetter.cs](file:///home/piyush/src/lld/TicTacToeBetter.cs))**:

```csharp
public interface IWinRule 
{ 
    bool IsWin(Board board, Cell lastMove, Piece piece); 
}

public class RowWinRule : IWinRule 
{
    public bool IsWin(Board board, Cell move, Piece piece) =>
        Enumerable.Range(0, board.Size).All(col => board.GetPiece(move.Row, col) == piece);
}

public class ColumnWinRule : IWinRule 
{
    public bool IsWin(Board board, Cell move, Piece piece) =>
        Enumerable.Range(0, board.Size).All(row => board.GetPiece(row, move.Col) == piece);
}

public class GameEngine
{
    private readonly List<IWinRule> _winRules;

    public GameEngine(List<IWinRule> winRules) => _winRules = winRules;

    public bool CheckWin(Board board, Cell lastMove, Piece piece)
    {
        // Engine delegates blindly; adding a new rule doesn't touch this class (OCP).
        return _winRules.Any(rule => rule.IsWin(board, lastMove, piece));
    }
}
```

---

#### Observer Pattern (Pub/Sub)
* **Problem**: Multiple dependent components (UI, audit log, analytics) need updates when a subject changes state, without the subject having hard dependencies on them.
* **Intent**: Define a 1-to-N dependency where state changes automatically notify all registered subscribers.
* **LLD Example ([TicTacToeBetter.cs](file:///home/piyush/src/lld/TicTacToeBetter.cs))**:

```csharp
public interface IBoardObserver
{
    void OnMoveApplied(Cell cell, Piece piece);
}

public class Board // The Subject / Publisher
{
    private readonly List<IBoardObserver> _observers = new();

    public void Subscribe(IBoardObserver observer) => _observers.Add(observer);
    public void Unsubscribe(IBoardObserver observer) => _observers.Remove(observer);

    public void ApplyMove(Cell cell, Piece piece)
    {
        // Apply move internally...
        NotifyObservers(cell, piece);
    }

    private void NotifyObservers(Cell cell, Piece piece)
    {
        foreach (var observer in _observers)
        {
            observer.OnMoveApplied(cell, piece);
        }
    }
}

public class ScoreboardUI : IBoardObserver // The Subscriber
{
    public void OnMoveApplied(Cell cell, Piece piece) =>
        Console.WriteLine($"[UI] Rendered piece {piece} at ({cell.Row}, {cell.Col})");
}
```

---

#### Command Pattern
* **Problem**: Need to execute actions while also supporting undo/redo, command queues, transaction logging, or scheduled execution.
* **Intent**: Encapsulate an operation and all data required to execute or reverse it into a standalone command object.
* **LLD Example (Tic-Tac-Toe Undo Feature)**:

```csharp
public interface ICommand
{
    void Execute();
    void Undo();
}

public class PlacePieceCommand : ICommand
{
    private readonly Board _board;
    private readonly Cell _cell;
    private readonly Piece _piece;

    public PlacePieceCommand(Board board, Cell cell, Piece piece)
    {
        _board = board;
        _cell = cell;
        _piece = piece;
    }

    public void Execute() => _board.SetPiece(_cell, _piece);
    public void Undo() => _board.ClearCell(_cell);
}

// In Game Orchestrator:
public class GameController
{
    private readonly Stack<ICommand> _history = new();

    public void MakeMove(ICommand command)
    {
        command.Execute();
        _history.Push(command);
    }

    public bool UndoLastMove()
    {
        if (_history.Count == 0) return false;
        var lastCommand = _history.Pop();
        lastCommand.Undo();
        return true;
    }
}
```

---

#### State Pattern
* **Problem**: An entity's behavior changes fundamentally depending on its internal lifecycle state, resulting in massive, error-prone `switch(state)` blocks inside every single method.
* **Intent**: Represent each state as a separate class implementing a common interface. State transitions swap the active state instance.
* **Real-World / LLD Example (Vending Machine / Order Lifecycle)**:

```csharp
public interface IVendingMachineState
{
    void InsertCoin(VendingMachine context, decimal amount);
    void SelectItem(VendingMachine context, string itemCode);
    void Dispense(VendingMachine context);
}

public class IdleState : IVendingMachineState
{
    public void InsertCoin(VendingMachine ctx, decimal amount)
    {
        ctx.Balance += amount;
        ctx.SetState(new HasCoinState());
    }

    public void SelectItem(VendingMachine ctx, string code) =>
        throw new InvalidOperationException("Please insert coin first.");

    public void Dispense(VendingMachine ctx) =>
        throw new InvalidOperationException("No item selected.");
}

public class DispensingState : IVendingMachineState
{
    public void InsertCoin(VendingMachine ctx, decimal amount) =>
        throw new InvalidOperationException("Currently dispensing. Please wait.");

    public void SelectItem(VendingMachine ctx, string code) =>
        throw new InvalidOperationException("Already dispensing.");

    public void Dispense(VendingMachine ctx)
    {
        // Dispense item logic...
        ctx.SetState(new IdleState());
    }
}
```

---

## 4. Pattern Disambiguation: "Which Pattern When?"

### Adding Layers / Steps: Builder vs. Decorator vs. Chain of Responsibility

A common interview trap is conflating patterns that involve multiple steps or layers:

```
                  ┌─────────────────────────────────────────┐
                  │ How do you need to combine your layers? │
                  └────────────────────┬────────────────────┘
                                       │
         ┌─────────────────────────────┼─────────────────────────────┐
         ▼                             ▼                             ▼
   [ Object Creation ]        [ Behavior Wrapping ]       [ Sequential Execution ]
         │                             │                             │
         ▼                             ▼                             ▼
   Builder Pattern             Decorator Pattern             Chain of Responsibility
 • Constructing an object    • Every layer executes        • Any handler can short-circuit
 • Client sets parameters    • Passes execution to inner   • Request passes until done
 • Produces final object     • Same interface across all   • Example: Middleware, Auth
```

| Trait | **Builder Pattern** | **Decorator Pattern** | **Chain of Responsibility** |
| :--- | :--- | :--- | :--- |
| **Category** | Creational | Structural | Behavioral |
| **Primary Goal** | Assembling a single complex object step-by-step | Extending runtime behavior without modifying original class | Routing a request through a pipeline of handlers |
| **Execution Flow** | Builder methods configure internal state, then `.Build()` creates target | Calls wrapped inner object: `Outer.Run() -> Inner.Run()` | Handlers decide whether to call `Next.Handle()` or **short-circuit** |
| **Key LLD Example** | `ParkingLotBuilder.AddFloor(...).Build()` | `WeekendSurgeFee(DurationFee(BaseFee))` | `AuthHandler -> BudgetHandler -> SpamHandler` |

---

### Dynamic Behavior: Strategy vs. State

| Trait | **Strategy Pattern** | **State Pattern** |
| :--- | :--- | :--- |
| **Intent** | Swapping *how* an algorithm is performed | Swapping *how* an object behaves based on *where* it is in its lifecycle |
| **Awareness** | Strategies are independent of each other (RowWinRule doesn't know ColWinRule) | States frequently know about other states to orchestrate transitions (`Idle -> HasMoney -> Dispensing`) |
| **Initiator** | Usually injected or chosen by the client | Typically triggered internally as actions alter the entity's lifecycle |

---

## 5. Applied LLD Problem Playbooks

---

### Problem 1: Tic-Tac-Toe
*Reference Implementation*: [TicTacToeBetter.cs](file:///home/piyush/src/lld/TicTacToeBetter.cs)

#### 1. Requirements & Core Challenge
* Support an $N \times N$ board and arbitrary $M$ players (extensible beyond just 2 players).
* Implement modular win and draw validation.
* Maintain clean turn rotation and allow move history / undo.

#### 2. Key Architectural Decisions & Patterns
* **Strategy Pattern**: Abstract win validation via `IWinRule` (`RowWinRule`, `ColWinRule`, `DiagonalWinRule`). Easy to add custom rules without mutating the engine.
* **Queue-based Turn Management**: Use `Queue<Player>` instead of `turnsCount % numberOfPlayers`.
  ```csharp
  // Naturally handles N players and variable turns without index arithmetic:
  var currentPlayer = _playerQueue.Dequeue();
  // ... execute move ...
  _playerQueue.Enqueue(currentPlayer);
  ```
* **Command Pattern**: Encapsulate each move in `PlacePieceCommand` with `.Execute()` and `.Undo()`. Store inside `Stack<ICommand> _moveHistory` for instant undo capability.

#### 3. Critical Optimizations & Pitfalls
* ❌ **Anti-Pattern (Exceptions for Flow Control)**: Throwing an exception when a player clicks an occupied cell.
  * *Fix*: Return validation booleans: `board.IsValidMove(cell)`. Exceptions are for truly exceptional failures, not normal invalid user input.
* ⚡ **The $O(1)$ Win Check Algorithm**:
  * *Naive Scan*: Iterating through all rows, columns, and diagonals takes $O(N)$ per move.
  * *Optimized $O(1)$*: Maintain score arrays for each player or directional counters:
    ```csharp
    // For 2 players (Player 1 = +1, Player 2 = -1):
    int[] rowSum = new int[N];
    int[] colSum = new int[N];
    int diagSum = 0, antiDiagSum = 0;

    // When move is made at (r, c) by val (+1 or -1):
    rowSum[r] += val;
    colSum[c] += val;
    if (r == c) diagSum += val;
    if (r + c == N - 1) antiDiagSum += val;

    // Check if absolute value equals N in O(1):
    if (Math.Abs(rowSum[r]) == N || Math.Abs(colSum[c]) == N ||
        Math.Abs(diagSum) == N || Math.Abs(antiDiagSum) == N)
    {
        return Winner;
    }
    ```

---

### Problem 2: The Parking Lot
*Reference Implementation*: [ParkingLot.cs](file:///home/piyush/src/lld/ParkingLot.cs)

#### 1. Requirements & Core Challenge
* Multi-floor parking facility with diverse vehicle types (`Motorcycle`, `Car`, `Truck`) and spot sizes (`Motorcycle`, `Compact`, `Large`).
* Compatibility Rules:
  * Motorcycle $\rightarrow$ Motorcycle, Compact, or Large
  * Car $\rightarrow$ Compact or Large
  * Truck $\rightarrow$ Large only
* Find nearest compatible spot on entry; issue ticket; calculate fee upon exit.

#### 2. Key Architectural Decisions & Patterns
* **Builder Pattern**: Constructing multi-level parking lots with varying floor configurations.
* **Decorator Pattern**: Composable fee structure (`BaseFee` + `HourlyDurationFee` + `WeekendSurgeFee`).

#### 3. Critical Optimizations & Pitfalls
* ❌ **The $O(N)$ Search Bottleneck**: Iterating through a flat array of 10,000 spots to find an empty one causes unacceptable latency.
  * ⚡ *Optimal $O(1)$ Allocation*: Maintain dedicated queues/free-lists per spot size per floor:
    ```csharp
    public class ParkingFloor
    {
        // O(1) spot acquisition
        private readonly Queue<ParkingSpot> _freeMotorcycleSpots = new();
        private readonly Queue<ParkingSpot> _freeCompactSpots = new();
        private readonly Queue<ParkingSpot> _freeLargeSpots = new();
    }
    ```
* ❌ **The Enum Casting Trap**:
  * Writing `(int)vehicleType <= (int)spotType` looks clever, but is extremely brittle. If an engineer inserts `ElectricCar` into the enum, ordering breaks silently.
  * *Fix*: Explicitly map compatibility using a lookup set or method:
    ```csharp
    public static bool CanFit(VehicleType vehicle, ParkingSpotType spot) => (vehicle, spot) switch
    {
        (VehicleType.Motorcycle, _) => true,
        (VehicleType.Car, ParkingSpotType.Compact or ParkingSpotType.Large) => true,
        (VehicleType.Truck, ParkingSpotType.Large) => true,
        _ => false
    };
    ```
* ⚡ **Exit Efficiency**: Store active vehicles in a `Dictionary<string, ParkingSpot>` (`LicensePlate -> Spot`) to enable $O(1)$ departure processing.

---

### Problem 3: LRU (Least Recently Used) Cache
*Reference Implementation*: [LRUCache.cs](file:///home/piyush/src/lld/LRUCache.cs)

#### 1. Requirements & Core Constraints
* In-memory cache with fixed `Capacity`.
* `Get(key)`: Returns value and marks item as Most Recently Used.
* `Put(key, value)`: Inserts/updates item. If capacity is exceeded, evicts the Least Recently Used item.
* **Hard Constraint**: Both `Get` and `Put` must execute in strict **$O(1)$ Time Complexity** and **$O(\text{Capacity})$ Space Complexity**.

#### 2. The Naive Approach (Lazy Eviction Queue) & Why It Fails
In initial iterations, engineers often attempt a `Queue<(string Key, int Timestamp)>` combined with a timestamp dictionary. While conceptually intuitive, this fails interview criteria:

| Flaw | Consequence |
| :--- | :--- |
| **Unbounded Memory Leak** | The queue grows with every single operation ($O(\text{Operations})$), violating the $O(\text{Capacity})$ memory bound. |
| **Latency Spikes ($O(N)$ Eviction)** | Dequeueing stale entries requires a `while` loop over old timestamp entries, causing worst-case $O(N)$ time latency. |
| **Integer Overflow Risk** | Incremental integer timestamps (`_currentTimeStamp++`) overflow long-running production systems. |

#### 3. The Canonical Solution: Doubly Linked List + Dictionary

```
                      ┌─────────────────────────────────┐
                      │   Dictionary<string, Node>      │
                      │  "A" ──► Node("A", 10)          │
                      │  "B" ──► Node("B", 20)          │
                      └────────────────┬────────────────┘
                                       │ Direct O(1) Pointer Access
                                       ▼
  [HEAD] <───► [ Node("B", 20) ] <───► [ Node("A", 10) ] <───► [TAIL]
  (MRU)                                                          (LRU)
```

* **Data Structure Anatomy**:
  1. `Dictionary<string, LinkedListNode<CacheItem>>`: Provides $O(1)$ lookups to the exact memory address/node.
  2. `DoublyLinkedList`: Allows $O(1)$ node removal and $O(1)$ head insertion without array shifts.
* **Operations Workflow**:
  * **`Get(key)`**: Look up node in dictionary. Detach node from current position in DLL, re-insert at `Head`. Return value. ($O(1)$)
  * **`Put(key, value)`**:
    * If key exists: Update value, move node to `Head`.
    * If key does not exist:
      * If `Count == Capacity`: Remove `Tail` node from DLL, remove key from dictionary.
      * Create new node, insert at `Head`, add to dictionary. ($O(1)$)

---

### Problem 4: Rate Limiter
*Reference Implementation*: [RateLimiter.cs](file:///home/piyush/src/lld/RateLimiter.cs)

#### 1. Requirements & Core Challenge
* Protect API gateway by enforcing $N$ requests per time window $T$ per user (e.g., 5 req/min).
* Return `HTTP 429 Too Many Requests` when limit is exceeded.

#### 2. Comparison of Core Algorithms

| Algorithm | Mechanism | Major Flaw / Trade-off | Space Complexity |
| :--- | :--- | :--- | :--- |
| **Fixed Window Counter** | Counts requests in fixed time slices (e.g., 10:00 - 10:01). Resets counter at boundary. | **Boundary Burst Problem**: A client can send $N$ requests at 10:00:59 and $N$ requests at 10:01:01 (2x traffic burst). | $O(1)$ per user |
| **Sliding Window Log** | Stores timestamp of every request in a `Queue<DateTime>`. Purges entries older than $(Now - T)$. | **Memory Explosion**: Storing full timestamps causes $O(M \times N)$ space consumption. Under high traffic, this crashes API gateway memory. | $O(N)$ per user |
| **Token Bucket (Optimal)** | Bucket holds tokens up to `Capacity`. Refills at rate $R$ tokens/sec. Each request consumes 1 token. | Handles bursts smoothly up to capacity; guarantees steady-state throughput. | **$O(1)$ per user** |

#### 3. Token Bucket Algorithm: The "Credit" Approach
Instead of running a continuous background timer or storing timestamp lists, calculate token refills **lazily on demand**:

```csharp
public class TokenBucketRateLimiter
{
    private readonly int _capacity;
    private readonly double _refillRatePerSecond;

    // Per-User State: Only 2 variables needed!
    private double _tokens;
    private DateTime _lastRefillTimestamp;

    public TokenBucketRateLimiter(int capacity, double refillRatePerSecond)
    {
        _capacity = capacity;
        _refillRatePerSecond = refillRatePerSecond;
        _tokens = capacity;
        _lastRefillTimestamp = DateTime.UtcNow;
    }

    public bool AllowRequest()
    {
        Refill();

        if (_tokens >= 1.0)
        {
            _tokens -= 1.0;
            return true;
        }

        return false; // 429 Rate Limit Exceeded
    }

    private void Refill()
    {
        var now = DateTime.UtcNow;
        double elapsedSeconds = (now - _lastRefillTimestamp).TotalSeconds;
        
        // Lazily compute earned tokens based on elapsed time:
        double tokensToAdd = elapsedSeconds * _refillRatePerSecond;
        _tokens = Math.Min(_capacity, _tokens + tokensToAdd);
        _lastRefillTimestamp = now;
    }
}
```

> [!TIP]
> **Key Interview Takeaway**: The Token Bucket algorithm achieves strict $O(1)$ time and $O(1)$ space per user by lazily computing refills on each incoming request rather than using background threads or storing individual request timestamps.

---

### Problem 5: Notification Service
*Reference Implementation*: [NotificationServiceBetter.cs](file:///home/piyush/src/lld/NotificationServiceBetter.cs)

#### 1. Requirements & Core Challenge
* Dispatch notifications via multiple external channels (`Email`, `SMS`, `Push`).
* Channel clients have distinct configuration lifecycles (SMTP hosts/credentials vs. Twilio API tokens).

#### 2. Key Architectural Decisions
* **Factory Method Pattern**: Decouple client creation logic (`EmailNotificationClient`, `SmsNotificationClient`) from consumer dispatch orchestration.
* **Separation of Concerns**: Separate the transport client (`INotificationClient.SendAsync(dest, msg)`) from domain user resolution (`INotificationService.NotifyUserAsync(userId, msg)`).

---

## 6. Live Interview Strategy & Tactics

### The "Verbal Interface" Hack
In a 45-minute live coding session, typing out 10 interfaces (`IPlayer`, `IBoard`, `IScoreKeeper`, `ITurnManager`) with complete Dependency Injection wiring exhausts your time before you write a single line of business logic.

> [!TIP]
> **The Strategy**:
> Write concrete classes first to get your executable loop working in the first 15 minutes, but **verbally state your architectural intent to the interviewer**:
> 
> *"For time efficiency, I'm writing a concrete `Board` class right now. In a production environment, I would extract this to an `IBoard` interface to support alternative grid implementations (e.g., 3D or hexagonal boards) and facilitate unit testing via mock objects."*
>
> **Outcome**: You earn full SOLID / architectural design points while completely avoiding the typing penalty.

---

### Execution Over Abstraction (Priorities)

```
        ╔═══════════════════════════════════════════════════════════╗
        ║       PRIORITY 1: Working, Bug-Free Core Execution        ║
        ║      (A functional, simple solution beats an unrunnable   ║
        ║               over-engineered architecture)               ║
        ╚─────────────────────────────┬─────────────────────────────╝
                                      │
                                      ▼
        ╔═══════════════════════════════════════════════════════════╗
        ║       PRIORITY 2: Correct Data Structures & $O(1)$ Tricks  ║
        ║        (Queues for allocation, Dictionaries for lookups)   ║
        ╚─────────────────────────────┬─────────────────────────────╝
                                      │
                                      ▼
        ╔═══════════════════════════════════════════════════════════╗
        ║       PRIORITY 3: Design Patterns & Extensibility         ║
        ║      (Refactor to Strategy, Factory, Decorator as needed) ║
        ╚═══════════════════════════════════════════════════════════╝
```

---

### 45-Minute Interview Pacing Guide

| Time Window | Focus Area | Action Items |
| :--- | :--- | :--- |
| **00:00 - 05:00** | **Requirements & Scope** | Clarify boundaries, identify constraints (e.g., $N$ players, scale, in-memory vs distributed). Agree on 2-3 core APIs. |
| **05:00 - 15:00** | **Core Domain Models** | Define core entities, enums, and data relationships. Choose primary data structures (e.g., DLL + Map for LRU). |
| **15:00 - 30:00** | **Core Execution Loop** | Implement end-to-end happy path. Ensure the code compiles, runs, and satisfies fundamental constraints. |
| **30:00 - 40:00** | **Refactor & Extensibility** | Apply design patterns where friction exists (e.g., inject Strategy for rules, Decorator for fees). Mention the "Verbal Interface" where typing is redundant. |
| **40:00 - 45:00** | **Edge Cases & Concurrency** | Walk through empty/null inputs, boundary conditions, and concurrency considerations (locking, race conditions). |

---

## 7. Concurrency & Thread-Safety Quick Reference

When transitioning from single-threaded LLD to production-grade concurrent code, choose synchronization primitives deliberately:

| Synchronization Primitive | Best Use Case | Interview Gotcha |
| :--- | :--- | :--- |
| `lock(syncObject)` / `Monitor` | Guarding short, in-memory critical sections (e.g., updating DLL pointers in LRUCache) | **Cannot use `await` inside a `lock` block!** Will cause compiler error CS1996. |
| `ReaderWriterLockSlim` | Read-heavy workloads (e.g., 95% `Get`, 5% `Put`) | Must release locks in `finally` blocks to avoid orphaned locks. |
| `SemaphoreSlim` | Asynchronous locking where you need to `await` inside critical section, or throttling concurrency | Must always initialize with capacity: `new SemaphoreSlim(1, 1)`. |
| `ConcurrentDictionary<TKey, TVal>` | Lock-free or fine-grained partitioned thread-safe lookups | Methods like `GetOrAdd` execute the value factory delegate outside the lock; avoid side-effects inside delegates. |

### Thread-Safe LRU Cache Pattern
```csharp
public class ConcurrentLruCache<TKey, TValue> where TKey : notnull
{
    private readonly int _capacity;
    private readonly object _lock = new();
    private readonly Dictionary<TKey, LinkedListNode<CacheItem>> _map;
    private readonly LinkedList<CacheItem> _list = new();

    public ConcurrentLruCache(int capacity)
    {
        _capacity = capacity;
        _map = new(capacity);
    }

    public bool TryGet(TKey key, out TValue? value)
    {
        lock (_lock) // Short, predictable in-memory lock
        {
            if (_map.TryGetValue(key, out var node))
            {
                _list.Remove(node);
                _list.AddFirst(node); // Move to MRU head
                value = node.Value.Value;
                return true;
            }
            value = default;
            return false;
        }
    }
}
```
