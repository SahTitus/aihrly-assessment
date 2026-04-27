# ANSWERS

## 1. Schema

```text
applications
- id uuid pk
- job_id uuid not null fk jobs(id)
- candidate_name varchar(200) not null
- candidate_email varchar(200) not null
- cover_letter text null
- current_stage text not null
- created_at timestamptz not null

application_notes
- id uuid pk
- application_id uuid not null fk applications(id)
- type text not null
- description text not null
- created_by_id uuid not null fk team_members(id)
- created_at timestamptz not null

stage_history
- id uuid pk
- application_id uuid not null fk applications(id)
- from_stage text not null
- to_stage text not null
- changed_by_id uuid not null fk team_members(id)
- changed_at timestamptz not null
- reason text null

application_scores
- id uuid pk
- application_id uuid not null fk applications(id)
- dimension text not null
- score int not null
- comment text null
- set_by_id uuid not null fk team_members(id)
- set_at timestamptz not null
- updated_by_id uuid null fk team_members(id)
- updated_at timestamptz null
```

Indexes:

- `applications(job_id, candidate_email)` unique. This enforces the duplicate-application rule.
- `application_notes(application_id)`. This supports `GET /api/applications/{id}` and `GET /api/applications/{id}/notes`.
- `stage_history(application_id)`. This supports `GET /api/applications/{id}`.
- `application_scores(application_id)`. This supports `GET /api/applications/{id}`.
- `application_scores(application_id, dimension)` unique. This enforces one current score per dimension per application.

For `GET /api/applications/{id}`, the current code does one EF Core query that loads:

- the application row
- the related job
- notes with note authors
- stage history with the team member who changed each stage
- scores with `set_by` and `updated_by`

The SQL shape is a single `SELECT` from `applications` with joins to `jobs`, `application_notes`, `team_members`, `stage_history`, and `application_scores`. In the current implementation that is one database round-trip.

## 2. Scoring design trade-off

### 2a

Three separate endpoints are better here because the score dimensions are fixed and named in the prompt. The routes are explicit, the request body stays small, and each dimension can get its own rules later without making one generic endpoint harder to validate.

One generic `PUT /api/applications/{id}/scores` would be better if the client usually saves all scores together or if the score dimensions can change over time. In that case one endpoint reduces request count and keeps the route surface smaller.

### 2b

I would keep `application_scores` as the current-state table and add `application_score_history` as an append-only table. That history table would have `id`, `application_id`, `dimension`, `score`, `comment`, `changed_by_id`, and `changed_at`.

The three write endpoints can stay the same. Each write would insert one history row and then upsert the current row in `application_scores`. I would only add a read endpoint for score history if the product needed to show that timeline.

## 3. Debugging question

- Get the application id, recruiter name, and approximate time of the change.
- In the browser, inspect the `PATCH /api/applications/{id}/stage` request from yesterday and confirm the payload, response status, response body, and `X-Team-Member-Id`.
- Check API logs for that request and confirm whether the server returned `200`, `400`, `401`, or `500`.
- Query `stage_history` for that application ordered by `changed_at` and see whether an `Interview` row was written.
- Query `applications.current_stage` for the same application and compare it with the latest `stage_history.to_stage`.
- If there is no `Interview` history row, focus on why the request was rejected or never reached the API.
- If `stage_history` says `Interview` but `applications.current_stage` says `Screening`, inspect the stage update code path and transaction behavior.
- If both database values say `Interview` but the recruiter still sees `Screening`, check whether the browser hit the wrong environment or showed stale data.
- Check whether another valid stage change happened after the recruiter moved the candidate.

## 4. Honest self-assessment

`C#`: 3/5. Comfortable building small APIs, DTOs, and tests. Still building depth on framework internals.

`SQL`: 3/5. Comfortable with schema design, constraints, joins, and indexes. Less experienced with deeper performance tuning.

`Git`: 3/5. Comfortable with normal branch, commit, and PR flow. Less confident when history cleanup gets messy.

`REST API design`: 3/5. Comfortable with routes, validation, status codes, and DTO boundaries. Still learning longer-term API trade-offs.

`Writing tests`: 3/5. Comfortable covering core behavior with unit and integration tests. I still want more reps on edge cases and broader failure coverage.
