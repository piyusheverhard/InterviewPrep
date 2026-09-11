# Clean Architecture & Dependency Inversion

## The Core Problem
In distributed systems, infrastructure (databases, Kafka, 3rd party APIs) is volatile. It changes, goes down, or scales differently than your business logic. 
If your business logic directly instantiates infrastructure (e.g., `new SqlConnection()`, `new TwilioClient()`), your system becomes:
1. **Impossible to Unit Test**: Tests will crash trying to hit real DBs or APIs.
2. **Fragile**: A Twilio outage could crash your entire checkout flow.
3. **Rigid**: Swapping from SQL Server to PostgreSQL requires rewriting business logic.

## The Solution: Ports and Adapters
Dependency Inversion (The 'D' in SOLID) states:
> *High-level modules should not depend on low-level modules. Both should depend on abstractions.*

We implement this via the **Ports and Adapters** pattern:
*   **Ports (Interfaces)**: Defined in the core domain. They dictate *what* the domain needs (e.g., `IUserRepository`, `INotificationClient`).
*   **Adapters (Concrete Implementations)**: Defined in the infrastructure layer. They implement the Ports (e.g., `SqlUserRepository`, `TwilioClient`).

### The Dependency Injection (DI) Container
The DI Container (like ASP.NET Core's `IServiceCollection`) is responsible for wiring Adapters to Ports at runtime. 
*   **Scoped**: One instance per HTTP request (Best for `DbContext` / Repositories to share transactions).
*   **Singleton**: One instance for the life of the app (Best for stateless clients like `HttpClient` or caching).
*   **Transient**: A new instance every time it is requested.

## Key Interview Takeaways & Code Patterns
### 1. Avoid DI Ambiguity
If you have multiple implementations of the same interface, standard DI will struggle to know which to inject.
*   **Bad**: `public MyService(INotificationClient sms, INotificationClient email)`
*   **Good**: Segregate interfaces (`ISmsClient : INotificationClient`) or use Keyed Services/Factories.

### 2. Resilient Execution Flow
When orchestrating multiple flaky adapters, never let one bring down the others unless required.
*   Wrap adapter calls in `try/catch`.
*   Return robust booleans or `Result` objects instead of letting exceptions bubble up and short-circuit fallback logic (like failing over to a message queue).
