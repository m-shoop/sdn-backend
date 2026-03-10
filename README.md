# sdn-backend

ASP.NET Core 8 REST API for a nail salon scheduling application. Handles appointment booking and serves data to the [sdn-frontend](https://github.com/m-shoop/sdn-frontend) SvelteKit client.

## Tech Stack

- **Framework:** ASP.NET Core 8
- **Language:** C#
- **Database:** PostgreSQL
- **ORM:** Entity Framework Core (with Migrations)
- **Architecture:** Layered — Controllers, Services, Repositories, DTOs, Models

## Features

- REST API for booking and managing salon appointments
- Clean separation of concerns via repository and service layers
- DTO pattern for safe, structured data transfer
- Database schema managed via EF Core migrations

## Getting Started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8)
- [PostgreSQL](https://www.postgresql.org/download/)

### Setup

1. Clone the repository:
   ```bash
   git clone https://github.com/m-shoop/sdn-backend.git
   cd sdn-backend
   ```

2. Update the connection string in `appsettings.json` to point to your local PostgreSQL instance:
   ```json
   "ConnectionStrings": {
     "DefaultConnection": "Host=localhost;Database=sdn;Username=youruser;Password=yourpassword"
   }
   ```

3. Apply database migrations:
   ```bash
   dotnet ef database update
   ```

4. Run the API:
   ```bash
   dotnet run
   ```

The API will be available at `https://localhost:5001` by default.

## Project Structure

```
Controllers/     # Route handlers and HTTP request/response logic
Services/        # Business logic layer
Repositories/    # Database access layer
Models/          # Entity definitions
Dtos/            # Data transfer objects for API input/output
Migrations/      # EF Core database migrations
```

## Related

- **Frontend:** [sdn-frontend](https://github.com/m-shoop/sdn-frontend) — SvelteKit client for this API
