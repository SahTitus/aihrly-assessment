# Aihrly ATS API

ASP.NET Core Web API for jobs, applications, notes, scores, stage history, and async notifications.

## Stack

- .NET 9
- ASP.NET Core Web API
- PostgreSQL with EF Core and Npgsql
- xUnit

## Run locally

Create the databases:

```sql
CREATE DATABASE aihrly;
CREATE DATABASE aihrly_test;
```

Create `Aihrly.Api/appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "Default": "Host=localhost;Database=aihrly;Username=postgres;Password=postgres"
  }
}
```

Apply migrations:

```bash
dotnet ef database update --project Aihrly.Api --startup-project Aihrly.Api
```

Start the API:

```bash
dotnet run --project Aihrly.Api
```

In `Development`, the API also applies migrations on startup.

## Run tests

```bash
dotnet test
```

Tests use the `aihrly_test` database from `Aihrly.Tests/ApiFactory.cs`.

## Seeded team members

Use these IDs in `X-Team-Member-Id` on note, score, and stage change requests.

| Name | ID | Role |
| --- | --- | --- |
| Alice Johnson | `a1b2c3d4-0001-0000-0000-000000000000` | Recruiter |
| Bob Martinez | `a1b2c3d4-0002-0000-0000-000000000000` | HiringManager |
| Carol Chen | `a1b2c3d4-0003-0000-0000-000000000000` | Recruiter |

## Assumptions

- Missing, invalid, or unknown `X-Team-Member-Id` returns `401`.
- Candidate email is lowercased before save, and `(job_id, candidate_email)` is unique.
- Scores store the current value only. A second `PUT` overwrites the previous value.
- `Hired` and `Rejected` are terminal stages.

## Part 2 choice

I picked Option A. When an application moves to `Hired` or `Rejected`, the API saves the stage change, enqueues a notification job, and returns the HTTP response without waiting for the notification work. A hosted background service reads that queue, writes a log line, and inserts a row into `notifications`. I used an in-memory queue because it keeps the take-home small and keeps the request path simple. The trade-off is that queued notifications would be lost if the process stops before the worker handles them.

## What I would improve with more time

- Add more integration tests around invalid headers and invalid stage changes.
- Replace the in-memory notification queue with durable storage.
- Add Docker setup for API and Postgres.
