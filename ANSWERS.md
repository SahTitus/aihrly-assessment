# ANSWERS.md

## Question 1 -- Schema design

**Tables (plain-text sketch):**

```
Applications       (Id, JobId FK, CandidateName, CandidateEmail, CoverLetter, CurrentStage, CreatedAt)
ApplicationNotes   (Id, ApplicationId FK, Type, Description, CreatedById FK, CreatedAt)
StageHistory       (Id, ApplicationId FK, FromStage, ToStage, ChangedById FK, ChangedAt, Reason)
ApplicationScores  (Id, ApplicationId FK, Dimension, Score, Comment, SetById FK, SetAt, UpdatedById FK?, UpdatedAt?)
```

**Indexes and why:**

- Unique `(JobId, CandidateEmail)` on `Applications` -- enforces the one-application-per-job rule at the DB level so no application code can accidentally allow duplicates.
- Non-unique index on `ApplicationId` in `ApplicationNotes`, `StageHistory`, and `ApplicationScores` -- every read of the full profile filters all three of these tables by application ID; without this index each would be a sequential scan.
- Unique `(ApplicationId, Dimension)` on `ApplicationScores` -- one score row per dimension per application; the DB rejects a second INSERT so the upsert logic does an UPDATE instead.

**`GET /api/applications/{id}` -- query and round-trips:**

One round-trip. EF Core's `.Include()` chain generates a single SELECT with LEFT JOINs across all four child tables, plus a join to `Jobs` for the title and a join to `TeamMembers` for each author/reviewer column. It returns all needed data in one database call.

---

## Question 2 -- Scoring design

**(a) Why three separate endpoints vs one generic endpoint, and when would the opposite be true?**

Three separate endpoints (`/scores/culture-fit`, `/scores/interview`, `/scores/assessment`) make the contract explicit -- a client knows exactly what it is submitting from the URL alone, and you can add per-dimension validation rules or different required fields later without touching the other two. HTTP-level caching and rate limiting are also easier to apply per-endpoint.

The generic approach (`PUT /scores` with a `"dimension"` field in the body) would be better if the set of dimensions was dynamic or configurable per-organisation -- hardcoding three routes only works when the dimensions are fixed and known at build time.

**(b) If the product team wanted full score change history, how would the schema change? Would the endpoints change?**

Schema change: add a `ScoreHistory` table with columns `(Id, ApplicationScoreId FK, OldScore, NewScore, ChangedById FK, ChangedAt)`. Every time a PUT overwrites an existing score, insert a row into `ScoreHistory` before applying the update. The `ApplicationScores` table stays as-is -- it still holds the current value.

Endpoint change: the three PUT endpoints keep the same signatures; the history write is an internal side-effect, invisible to the caller. You would add a new `GET /api/applications/{id}/scores/{dimension}/history` endpoint to expose the log, but the write side does not need to change.

---

## Question 3 -- Debugging a missing stage transition

A recruiter says: "I moved a candidate to Interview yesterday and today the system says they are still in Screening."

- **Check `StageHistory` directly.** Query the DB for all rows where `ApplicationId` matches. If a `Screening -> Interview` row exists, the transition did persist -- the bug is in what is being displayed, not in the write path.
- **If no `StageHistory` row exists,** the PATCH never completed successfully. Check the API logs for a `PATCH /api/applications/{id}/stage` request around the reported time -- look at the HTTP status code returned.
- **If the log shows a 400,** the transition was rejected. The most likely cause: the application was not actually in `Screening` at that moment (maybe another team member had already moved it, or it was in a different stage than the recruiter thought).
- **If the log shows a 200 but no DB row,** there is a bug in the write logic -- either `SaveChangesAsync()` was not called, an exception was swallowed, or the request hit the wrong environment/instance.
- **If there is no log entry at all,** the request never reached the server. Open the browser network tab, reproduce the action, and check whether the request was actually sent and what response came back.
- **Check `CurrentStage` on the `Applications` row.** It should always match the `ToStage` of the most recent `StageHistory` entry. If they differ, there is an atomicity bug -- the history row was written but the application row was not updated in the same transaction.
- **Check for a concurrent PATCH.** If two team members acted on the same application around the same time, one could have overwritten the other. `StageHistory` would show both transitions in sequence.
- **Try to reproduce manually.** Use Postman or curl to send the exact same PATCH against a test application in the same stage. If it works, the original failure was a one-off (network drop, browser tab closed mid-request, etc.).

---

## Question 4 -- Honest self-assessment

**C#: 2/5** -- I can read and write working C# and understand async/await, but I am still learning the deeper parts of the language and the .NET ecosystem.

**SQL: 3/5** -- Comfortable with schema design, joins, indexes, and writing queries by hand; less experienced with query optimisation and reading execution plans.

**Git: 2/5** -- I use commits, branches, and push/pull confidently but do not have much experience with rebasing, resolving complex merge conflicts, or advanced history tools.

**REST API design: 3/5** -- I understand resource-oriented routes, correct verb usage, and standard status codes; I would need more practice on versioning strategies and edge-case error handling.

**Writing tests: 2/5** -- I wrote the required tests and they pass, but my coverage is thin and I am still learning how to design tests that are genuinely meaningful rather than just exercising happy paths.
