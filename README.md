# Mini E-Commerce Order API
An ASP.NET Core Web API that includes user authentication, product management, and order processing with transactions.

## Used tech:
- .NET 8 Web API
- EF Core + Npgsql (PostgreSQL)
- JWT Bearer authentication, role-based authorization (Admin/Customer)
- FluentValidation for request validation
- Swagger for interactive API docs
- Docker + Docker Compose
- xUnit, Moq, SQLite (in-memory) for unit tests

## Architecture
Order creation checks product stock and decrements it in the same transaction the order is created.

Project is organized into folders by features:
Controllers - AppUserController, AuthController, ProductController, OrdersController
Services - One service per feature
DTOs - Request/response shapes + FluentValidation validators, one file per feature
Models - EF Core entities, Exceptions
Middlewares - HttpLoggingMiddleware, GlobalExceptionHandler, RateLimitMiddleware
Data - DataContext (EF Core), DataSeeder
Tests - xUnit test project — one test class per service

Controllers call services and don't catch app exceptions. A GlobalExceptionHandler maps every domain exception to the right HTTP status and a ProblemDetails response so no controller needs its own try/catch.

# Setup & Run
Docker Desktop is needed as a prerequisite.

To run:
In terminal, navigate to ECommerceAPI folder, where ECommerceAPI.csproj is located, and write
```bash
docker compose up --build
```
This starts Postgres and the API together, waits for Postgres to actually be ready (not just started) before the API connects. The API applies EF Core migrations automatically on startup.

- API: `http://localhost:8080`
- Interactive docs: `http://localhost:8080/swagger`

Configuration:
The API reads its connection string and JWT settings from environment variables, set in `docker-compose.yml`:
`ConnectionStrings__DefaultConnection` - Postgres connection string
`Jwt__Issuer`, `Jwt__Audience`, `Jwt__Key` - JWT signing configuration

## Tests
To run:
In terminal, navigate to root folder, where ECommerceAPI.sln is located, and write:
```bash
dotnet test
```
Tests use a temporary SQLite in-memory database for each test. SQLite was chosen over EF Cores own InMemory provider because the latter does not support transactions, and methods like OrderService.CreateOrderAsync, RateLimitService.CheckAsync use their own transactions. AuthService tests use Moq to fake ITokenService, so no real JWT signing key is needed to test login logic.

Tests cover:
- order creation (stock validation, multi-item totals, transaction rollback on failure), order status transitions (valid/invalid paths, role permissions)
- product CRUD/filtering/pagination
- user CRUD (ownership checks, duplicate email)
- auth (registration, login, credential validation)

# Key Approaches
- Stock concurrency safety: Product carries a Postgres xmin-based optimistic concurrency token. CreateOrderAsync catches DbUpdateConcurrencyException and retries with fresh data (up to 5 attempts). Thus, two simultaneous orders against `stock = 1` can't both succeed.
- Order status transitions: only Pending → Confirmed → Completed and Pending → Cancelled are permitted. Every transition is recorded in OrderStatusHistory with a timestamp. Customers may only cancel their own pending order, while Admins can also confirm or complete.
- Price captured at order time: When an order is placed, OrderItem.ProductPrice is copied from Product.Price, so change in product price in the future will not affect the total price in order history.
- DB-backed rate limiting: policies (RateLimitPolicy) and per-client counters (RateLimitCounter) live in the database, using a fixed window and a Serializable isolation transaction to keep increments race-free. Rate limits are not applied to Admins.
- HTTP Logging: HttpLoggingMiddleware saves every request/response (method, path, body, status, duration) to the database, while intentionally never allowing a logging failure to break the request it's observing (isolated in its own try/catch).
- Connection resiliency: EnableRetryOnFailure is enabled on the DbContext so transient Postgres connection drops are retried automatically. Methods that manage their own transaction (OrderService.CreateOrderAsync, RateLimitService.CheckAsync) wrap that transaction in Database.CreateExecutionStrategy().ExecuteAsync(...), since EF Core requires this to combine a retrying strategy with a manually-managed transaction.
- Idempotency Keys on order creation: POST /api/orders accepts an additional Idempotency-Key header, which is a client-generated string. It protects against a case where an order request succeeds server-side but the client never sees the response due to connection issues and resends the exact same request. It prevents such requests from creating duplicate orders and decrementing stock more than once.

# Main Solved Problems
- Originally, User and Product APIs were each separate microservices. However, they were combined under a single project later when Order creation was implemented, to avoid extra complexity that would arise from using transactions across two different databases (for Product and Order).
- Password hashes were exposed in API responses. Early versions of the user endpoints directly returned the user model which included PasswordHash. Fixed by introducing response DTOs that never include it.
- Duplicate emails were not fully prevented. Uniqueness was checked on registration method but not on create user method for admins or on profile update method. So, a user could change their email to another one already in use. Now checked consistently in all three methods.
- Postgres's first-boot double-start raced with the Compose healthcheck. On a fresh volume, Postgres starts, initializes, restarts. pg_isready could report "healthy" during the brief window before the restart, which let the API container start and connect right as Postgres cycled. Solved with EnableRetryOnFailure, which also gives general resilience against connection.
- SQLite's decimal handling doesn't behave like Postgres's for range queries. Unit tests for product price filtering/sorting failed under SQLite until Price was mapped to double (only for the test database). SQLite couldn't sort decimals stored as text by EF. The production uses real Postgres decimal values.
