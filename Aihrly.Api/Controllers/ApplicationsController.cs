using Aihrly.Api.Data;
using Aihrly.Api.DTOs.Applications;
using Aihrly.Api.DTOs.Notes;
using Aihrly.Api.DTOs.Scores;
using Aihrly.Api.DTOs.StageHistory;
using Aihrly.Api.Entities;
using Aihrly.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Aihrly.Api.Controllers;

[ApiController]
[Route("api/applications")]
public class ApplicationsController(AppDbContext db, NotificationQueue notificationQueue) : ControllerBase
{
    [HttpGet("{id}")]
    public async Task<IActionResult> GetApplication(Guid id)
    {
        // load everything needed for the profile
        var app = await db.Applications
            .Include(a => a.Job)
            .Include(a => a.Notes).ThenInclude(n => n.CreatedBy)
            .Include(a => a.StageHistory).ThenInclude(s => s.ChangedBy)
            .Include(a => a.Scores).ThenInclude(s => s.SetBy)
            .Include(a => a.Scores).ThenInclude(s => s.UpdatedBy)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (app is null) return NotFound();

        return Ok(new ApplicationProfileResponse
        {
            Id = app.Id,
            JobId = app.JobId,
            JobTitle = app.Job.Title,
            CandidateName = app.CandidateName,
            CandidateEmail = app.CandidateEmail,
            CoverLetter = app.CoverLetter,
            CurrentStage = app.CurrentStage.ToString(),
            CreatedAt = app.CreatedAt,
            Scores = app.Scores.Select(s => new ScoreResponse
            {
                Dimension = s.Dimension.ToString(),
                Score = s.Score,
                Comment = s.Comment,
                SetByName = s.SetBy.Name,
                SetAt = s.SetAt,
                UpdatedByName = s.UpdatedBy?.Name,
                UpdatedAt = s.UpdatedAt
            }).ToList(),
            Notes = app.Notes
                .OrderByDescending(n => n.CreatedAt)
                .Select(n => new NoteResponse
                {
                    Id = n.Id,
                    Type = n.Type.ToString(),
                    Description = n.Description,
                    AuthorName = n.CreatedBy.Name,
                    CreatedAt = n.CreatedAt
                }).ToList(),
            StageHistory = app.StageHistory
                .OrderBy(s => s.ChangedAt)
                .Select(s => new StageHistoryResponse
                {
                    FromStage = s.FromStage.ToString(),
                    ToStage = s.ToStage.ToString(),
                    ChangedByName = s.ChangedBy.Name,
                    ChangedAt = s.ChangedAt,
                    Reason = s.Reason
                }).ToList()
        });
    }

    [HttpPatch("{id}/stage")]
    public async Task<IActionResult> PatchStage(Guid id, PatchStageRequest request)
    {
        if (!TryGetTeamMemberId(out var memberId))
            return Problem(detail: "X-Team-Member-Id header is missing or invalid.", statusCode: 401);

        var member = await db.TeamMembers.FindAsync(memberId);
        if (member is null)
            return Problem(detail: "Team member not found.", statusCode: 401);

        var app = await db.Applications.FindAsync(id);
        if (app is null) return NotFound();

        var target = request.TargetStage!.Value;

        // guard against invalid transition
        if (!StageTransitionValidator.IsValid(app.CurrentStage, target))
        {
            var valid = StageTransitionValidator.ValidNext(app.CurrentStage);
            return Problem(
                detail: $"Cannot move from {app.CurrentStage} to {target}. Valid next stages: {string.Join(", ", valid)}.",
                statusCode: 400);
        }

        db.StageHistories.Add(new StageHistory
        {
            Id = Guid.NewGuid(),
            ApplicationId = app.Id,
            FromStage = app.CurrentStage,
            ToStage = target,
            ChangedById = memberId,
            ChangedAt = DateTime.UtcNow,
            Reason = request.Reason
        });

        app.CurrentStage = target;
        await db.SaveChangesAsync();

        // fire and forget, don't block request
        if (target is ApplicationStage.Hired or ApplicationStage.Rejected)
            notificationQueue.Enqueue(new NotificationJob(app.Id, target.ToString()));

        return Ok(new { currentStage = app.CurrentStage.ToString() });
    }

    [HttpPost("{id}/notes")]
    public async Task<IActionResult> AddNote(Guid id, CreateNoteRequest request)
    {
        if (!TryGetTeamMemberId(out var memberId))
            return Problem(detail: "X-Team-Member-Id header is missing or invalid.", statusCode: 401);

        var member = await db.TeamMembers.FindAsync(memberId);
        if (member is null)
            return Problem(detail: "Team member not found.", statusCode: 401);

        if (!await db.Applications.AnyAsync(a => a.Id == id))
            return NotFound();

        var note = new ApplicationNote
        {
            Id = Guid.NewGuid(),
            ApplicationId = id,
            Type = request.Type,
            Description = request.Description,
            CreatedById = memberId,
            CreatedAt = DateTime.UtcNow
        };

        db.ApplicationNotes.Add(note);
        await db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetNotes), new { id }, new NoteResponse
        {
            Id = note.Id,
            Type = note.Type.ToString(),
            Description = note.Description,
            AuthorName = member.Name,
            CreatedAt = note.CreatedAt
        });
    }

    [HttpGet("{id}/notes")]
    public async Task<IActionResult> GetNotes(Guid id)
    {
        if (!await db.Applications.AnyAsync(a => a.Id == id))
            return NotFound();

        var notes = await db.ApplicationNotes
            .Where(n => n.ApplicationId == id)
            .Include(n => n.CreatedBy)
            .OrderByDescending(n => n.CreatedAt)
            .Select(n => new NoteResponse
            {
                Id = n.Id,
                Type = n.Type.ToString(),
                Description = n.Description,
                AuthorName = n.CreatedBy.Name,
                CreatedAt = n.CreatedAt
            })
            .ToListAsync();

        return Ok(notes);
    }

    [HttpPut("{id}/scores/culture-fit")]
    public Task<IActionResult> PutCultureFitScore(Guid id, ScoreRequest request)
        => UpsertScore(id, ScoreDimension.CultureFit, request);

    [HttpPut("{id}/scores/interview")]
    public Task<IActionResult> PutInterviewScore(Guid id, ScoreRequest request)
        => UpsertScore(id, ScoreDimension.Interview, request);

    [HttpPut("{id}/scores/assessment")]
    public Task<IActionResult> PutAssessmentScore(Guid id, ScoreRequest request)
        => UpsertScore(id, ScoreDimension.Assessment, request);

    private async Task<IActionResult> UpsertScore(Guid applicationId, ScoreDimension dimension, ScoreRequest request)
    {
        if (!TryGetTeamMemberId(out var memberId))
            return Problem(detail: "X-Team-Member-Id header is missing or invalid.", statusCode: 401);

        var member = await db.TeamMembers.FindAsync(memberId);
        if (member is null)
            return Problem(detail: "Team member not found.", statusCode: 401);

        if (!await db.Applications.AnyAsync(a => a.Id == applicationId))
            return NotFound();

        var existing = await db.ApplicationScores
            .FirstOrDefaultAsync(s => s.ApplicationId == applicationId && s.Dimension == dimension);

        if (existing is null)
        {
            db.ApplicationScores.Add(new ApplicationScore
            {
                Id = Guid.NewGuid(),
                ApplicationId = applicationId,
                Dimension = dimension,
                Score = request.Score,
                Comment = request.Comment,
                SetById = memberId,
                SetAt = DateTime.UtcNow
            });
        }
        else
        {
            // overwrite, track who last changed it
            existing.Score = request.Score;
            existing.Comment = request.Comment;
            existing.UpdatedById = memberId;
            existing.UpdatedAt = DateTime.UtcNow;
        }

        await db.SaveChangesAsync();

        return Ok(new { dimension = dimension.ToString(), score = request.Score });
    }

    private bool TryGetTeamMemberId(out Guid memberId)
    {
        memberId = Guid.Empty;
        return Request.Headers.TryGetValue("X-Team-Member-Id", out var value)
            && Guid.TryParse(value, out memberId);
    }
}
