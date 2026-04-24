using Aihrly.Api.Data;
using Aihrly.Api.DTOs;
using Aihrly.Api.DTOs.Applications;
using Aihrly.Api.DTOs.Jobs;
using Aihrly.Api.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Aihrly.Api.Controllers;

[ApiController]
[Route("api/jobs")]
public class JobsController(AppDbContext db) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> CreateJob(CreateJobRequest request)
    {
        var job = new Job
        {
            Id = Guid.NewGuid(),
            Title = request.Title,
            Description = request.Description,
            Location = request.Location,
            Status = JobStatus.Open,
            CreatedAt = DateTime.UtcNow
        };

        db.Jobs.Add(job);
        await db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetJob), new { id = job.Id }, MapToResponse(job));
    }

    [HttpGet]
    public async Task<IActionResult> ListJobs(
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = db.Jobs.AsQueryable();

        if (!string.IsNullOrEmpty(status) && Enum.TryParse<JobStatus>(status, true, out var parsed))
            query = query.Where(j => j.Status == parsed);

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(j => j.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return Ok(new PagedResponse<JobResponse>
        {
            Items = items.Select(MapToResponse).ToList(),
            Page = page,
            PageSize = pageSize,
            Total = total
        });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetJob(Guid id)
    {
        var job = await db.Jobs.FindAsync(id);
        return job is null ? NotFound() : Ok(MapToResponse(job));
    }

    [HttpPost("{jobId}/applications")]
    public async Task<IActionResult> SubmitApplication(Guid jobId, CreateApplicationRequest request)
    {
        var job = await db.Jobs.FindAsync(jobId);
        if (job is null) return NotFound();

        var application = new Application
        {
            Id = Guid.NewGuid(),
            JobId = jobId,
            CandidateName = request.CandidateName,
            // normalize email so duplicates can't sneak in via casing
            CandidateEmail = request.CandidateEmail.ToLowerInvariant(),
            CoverLetter = request.CoverLetter,
            CurrentStage = ApplicationStage.Applied,
            CreatedAt = DateTime.UtcNow
        };

        db.Applications.Add(application);

        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: "23505" })
        {
            return Conflict(Problem(
                detail: "This candidate has already applied for this position.",
                statusCode: 409));
        }

        return CreatedAtAction(
            actionName: "GetApplication",
            controllerName: "Applications",
            routeValues: new { id = application.Id },
            value: new ApplicationSummaryResponse
            {
                Id = application.Id,
                CandidateName = application.CandidateName,
                CandidateEmail = application.CandidateEmail,
                CurrentStage = application.CurrentStage.ToString(),
                CreatedAt = application.CreatedAt
            });
    }

    [HttpGet("{jobId}/applications")]
    public async Task<IActionResult> ListApplications(Guid jobId, [FromQuery] string? stage)
    {
        if (!await db.Jobs.AnyAsync(j => j.Id == jobId))
            return NotFound();

        var query = db.Applications.Where(a => a.JobId == jobId);

        if (!string.IsNullOrEmpty(stage) && Enum.TryParse<ApplicationStage>(stage, true, out var parsed))
            query = query.Where(a => a.CurrentStage == parsed);

        var items = await query
            .OrderByDescending(a => a.CreatedAt)
            .Select(a => new ApplicationSummaryResponse
            {
                Id = a.Id,
                CandidateName = a.CandidateName,
                CandidateEmail = a.CandidateEmail,
                CurrentStage = a.CurrentStage.ToString(),
                CreatedAt = a.CreatedAt
            })
            .ToListAsync();

        return Ok(items);
    }

    private static JobResponse MapToResponse(Job job) => new()
    {
        Id = job.Id,
        Title = job.Title,
        Description = job.Description,
        Location = job.Location,
        Status = job.Status.ToString(),
        CreatedAt = job.CreatedAt
    };
}
