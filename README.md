# Aihrly ATS - Backend Assessment

ASP.NET Core 10 Web API backed by PostgreSQL 17. Models a minimal recruiting pipeline: jobs, applications, stage transitions, notes, scores, and async notifications.

---

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- PostgreSQL 17 running locally on the default port (5432)
- `dotnet-ef` tool (`dotnet tool install -g dotnet-ef`)

---

## Running locally

1. **Create the database** (if it does not already exist):

   ```sql
   CREATE DATABASE aihrly;
   ```

2. **Set your database password.**
   Create `Aihrly.Api/appsettings.Development.json` (git-ignored) with your real credentials:

   ```json
   {
     "ConnectionStrings": {
       "Default": "Host=localhost;Database=aihrly;Username=postgres;Password=YOUR_PASSWORD"
     }
   }
   ```

3. **Apply migrations and start the API:**
   ```bash
   cd Aihrly.Api
   dotnet run
   ```
   Migrations run automatically on startup when the environment is `Development`.

The API is available at `https://localhost:7xxx` / `http://localhost:5xxx` (ports printed on startup).

---

## Seeded team members

Three team members are seeded automatically via the migration. Use their IDs in the `X-Team-Member-Id` header for any write operation that requires reviewer identity.

| Name          | ID                                     | Role           |
| ------------- | -------------------------------------- | -------------- |
| Alice Johnson | `a1b2c3d4-0001-0000-0000-000000000000` | Recruiter      |
| Bob Smith     | `a1b2c3d4-0002-0000-0000-000000000000` | Hiring Manager |
| Carol Lee     | `a1b2c3d4-0003-0000-0000-000000000000` | Interviewer    |

---

## API overview

| Method  | Route                                            | Purpose                                             |
| ------- | ------------------------------------------------ | --------------------------------------------------- |
| `POST`  | `/api/jobs`                                      | Create a job                                        |
| `GET`   | `/api/jobs`                                      | List jobs (filterable by `status`, paginated)       |
| `GET`   | `/api/jobs/{id}`                                 | Get a single job                                    |
| `POST`  | `/api/jobs/{jobId}/applications`                 | Submit an application                               |
| `GET`   | `/api/jobs/{jobId}/applications`                 | List applications for a job (filterable by `stage`) |
| `GET`   | `/api/applications/{id}`                         | Full candidate profile                              |
| `PATCH` | `/api/applications/{id}/stage`                   | Advance or reject an application                    |
| `POST`  | `/api/applications/{id}/notes`                   | Add a note                                          |
| `GET`   | `/api/applications/{id}/notes`                   | List notes                                          |
| `PUT`   | `/api/applications/{id}/scores/culture-fit`      | Upsert culture-fit score                            |
| `PUT`   | `/api/applications/{id}/scores/technical-skills` | Upsert technical-skills score                       |
| `PUT`   | `/api/applications/{id}/scores/communication`    | Upsert communication score                          |

### Headers required on reviewer actions

```
X-Team-Member-Id: <team-member-guid>
```

Required on: `PATCH /stage`, `POST /notes`, all three score `PUT` endpoints.

---

## Part 2 - Background notifications (Option A)

When an application moves to `Hired` or `Rejected`, a `NotificationJob` is pushed onto an in-process `Channel<T>` queue. A singleton `BackgroundService` (`NotificationWorker`) drains the channel, creates a scoped `AppDbContext`, logs the send, and writes a row to the `Notifications` table.

No external infrastructure (Redis, RabbitMQ, Hangfire) is required. The trade-off is that queued jobs are lost if the process crashes; for production this would be replaced with durable storage.

---

## Running the tests

The integration tests connect to a separate `aihrly_test` database. The database is created automatically when EF Core migrations run on first launch of the test server.

```bash
cd Aihrly.Tests
dotnet test
```

Or from the solution root:

```bash
dotnet test
```

You do **not** need to create `aihrly_test` manually - EF Core handles it.

---

## Assumptions

- Authentication is out of scope; reviewer identity is passed via a plain HTTP header.
- The three score dimensions (CultureFit, TechnicalSkills, Communication) are modelled as separate `PUT` endpoints that upsert. A reviewer can only hold one value per dimension per application.
- Stage transitions follow a strictly linear pipeline; skipping stages and re-opening terminal states (`Hired`, `Rejected`) are both rejected with `400`.
- Duplicate applications are identified by `(job, candidate email)` and rejected with `409`.
- Pagination defaults to `page=1, pageSize=20`.

---

## What I would improve with more time

- Replace the in-process notification queue with Hangfire + PostgreSQL persistence so queued jobs survive restarts.
- Add real authentication (JWT or API key) rather than a trusted header.
- Expand test coverage: per-stage filter tests, invalid enum values in PATCH body, score boundary validation.
- Add `GET /api/applications` (search across all jobs).
- Soft-delete jobs instead of hard-delete, to preserve historical application data.

