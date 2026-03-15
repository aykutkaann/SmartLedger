# SmartLedger

A production-grade banking API built with **.NET 8**, **PostgreSQL**, and **React + TypeScript**.
Demonstrates row-level security, serializable transactions, JSONB fraud signals, CQRS, JWT auth with refresh token rotation, and OpenTelemetry tracing.

---

## Tech stack

| Layer          | Technology                                      |
|----------------|-------------------------------------------------|
| API            | ASP.NET Core 8 Minimal API                      |
| Domain         | Clean architecture, CQRS + MediatR              |
| ORM            | EF Core 8 (writes) + Dapper (reads)             |
| Database       | PostgreSQL 16 — RLS, JSONB, partial indexes     |
| Auth           | JWT (15 min) + refresh token rotation           |
| Background     | .NET BackgroundService fraud scoring worker     |
| Observability  | OpenTelemetry → Jaeger                          |
| Frontend       | React 18 + TypeScript + Vite                    |
| Tests          | xUnit + Testcontainers (real Postgres)          |
| CI/CD          | GitHub Actions                                  |
| Containers     | Docker + Docker Compose                         |

---

## Quick start

### Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/)
- [Node.js 20+](https://nodejs.org/)

### 1. Clone and scaffold

```bash
git clone https://github.com/you/SmartLedger.git
cd SmartLedger
chmod +x setup.sh && ./setup.sh
```

### 2. Start infrastructure

```bash
docker compose up postgres redis jaeger -d
```

### 3. Run the API

```bash
cd src/SmartLedger.API
dotnet run
# API: http://localhost:5000
# Swagger: http://localhost:5000/swagger
```

### 4. Run the fraud worker

```bash
cd src/SmartLedger.Worker
dotnet run
```

### 5. Run the frontend

```bash
cd frontend
npm install && npm run dev
# http://localhost:5173
```

### 6. Full stack via Docker Compose

```bash
docker compose up --build
```

| Service       | URL                         |
|---------------|-----------------------------|
| API           | http://localhost:5000        |
| Swagger UI    | http://localhost:5000/swagger|
| Frontend      | http://localhost:5173        |
| Jaeger UI     | http://localhost:16686       |
| PostgreSQL    | localhost:5432               |

---

## Running tests

```bash
# Unit tests (no infrastructure needed)
dotnet test tests/SmartLedger.UnitTests

# Integration tests (spins up Postgres via Testcontainers automatically)
dotnet test tests/SmartLedger.IntegrationTests
```

---

## Architecture highlights

### Row-level security
Every query against the `accounts` table is silently filtered by PostgreSQL
to only return rows belonging to the authenticated user — enforced at the
database engine level, not in application code.

```sql
CREATE POLICY user_isolation ON accounts
  USING (user_id = current_setting('app.current_user_id')::uuid);
```

### Serializable transfers
Transfers run under `IsolationLevel.Serializable` with `SELECT FOR UPDATE`
locking on both account rows — preventing double-spend race conditions even
under concurrent load.

### JSONB fraud signals
Each fraud rule writes a named key into a `jsonb` column:
```json
{
  "velocity":      { "recent_count": 8, "points": 30 },
  "amount_anomaly":{ "amount": 50000, "avg": 1200, "ratio": 41.67, "points": 30 },
  "new_recipient": { "to_account_id": "...", "points": 20 }
}
```
Queryable via `WHERE fraud_signals->>'velocity' IS NOT NULL`.

### Fraud scoring rules

| Rule             | Trigger                              | Score |
|------------------|--------------------------------------|-------|
| Velocity         | > 5 txns in 10 min from same account | +30   |
| Amount anomaly   | > 3× 90-day average                  | +20–30|
| New recipient    | First transfer to destination        | +20   |
| Round number     | Amount divisible by 500 or 1000      | +10   |
| Large transfer   | Amount > 50,000                      | +15   |
| **Flag threshold**| **Score ≥ 70 → status = Flagged**  |       |

---

## Project structure

```
SmartLedger/
├── src/
│   ├── SmartLedger.Domain/          # Entities, interfaces, domain services
│   ├── SmartLedger.Application/     # CQRS commands/queries (MediatR)
│   ├── SmartLedger.Infrastructure/  # EF Core, Dapper, fraud worker
│   ├── SmartLedger.API/             # Minimal API endpoints, middleware
│   └── SmartLedger.Worker/          # Fraud scoring background host
├── tests/
│   ├── SmartLedger.UnitTests/       # Domain logic, fraud rules
│   └── SmartLedger.IntegrationTests/# Full HTTP tests with Testcontainers
├── frontend/                        # React + TypeScript (Vite)
├── docker-compose.yml
├── Dockerfile.api
├── Dockerfile.worker
└── .github/workflows/ci.yml
```

---

## Environment variables

| Variable                        | Description                     | Default              |
|---------------------------------|---------------------------------|----------------------|
| `ConnectionStrings__Postgres`   | PostgreSQL connection string    | see appsettings.json |
| `Jwt__Key`                      | HS256 signing key (≥32 chars)   | —                    |
| `Jwt__Issuer`                   | JWT issuer claim                | SmartLedger          |
| `Jwt__ExpiryMinutes`            | Access token lifetime           | 15                   |
| `OTEL_EXPORTER_OTLP_ENDPOINT`   | Jaeger OTLP endpoint            | http://jaeger:4317   |

> **Important:** change `Jwt__Key` before any deployment. Use `dotnet user-secrets` locally.