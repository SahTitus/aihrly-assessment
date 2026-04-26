using System.Net;
using System.Net.Http.Json;
using Aihrly.Api.DTOs.Applications;
using Aihrly.Api.DTOs.Jobs;
using Aihrly.Api.DTOs.Notes;
using Aihrly.Api.DTOs.Scores;

namespace Aihrly.Tests;

// each test creates its own data with a random email so runs are independent
public class IntegrationTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    // seeded in AppDbContext — name must stay in sync with seed data
    private static readonly Guid Alice = new("a1b2c3d4-0001-0000-0000-000000000000");

    [Fact]
    public async Task Note_author_is_resolved_to_name_not_stored_as_id()
    {
        var (_, appId) = await CreateJobAndApplication();

        var req = new HttpRequestMessage(HttpMethod.Post, $"/api/applications/{appId}/notes");
        req.Headers.Add("X-Team-Member-Id", Alice.ToString());
        req.Content = JsonContent.Create(new { type = "General", description = "Solid candidate." });

        var postResp = await _client.SendAsync(req);
        postResp.EnsureSuccessStatusCode();

        var notes = await _client.GetFromJsonAsync<List<NoteResponse>>(
            $"/api/applications/{appId}/notes");

        Assert.NotNull(notes);
        Assert.Single(notes);
        Assert.Equal("Alice Johnson", notes[0].AuthorName);
    }

    [Fact]
    public async Task Score_second_submission_overwrites_first_and_tracks_updater()
    {
        var (_, appId) = await CreateJobAndApplication();

        await PutCultureFitScore(appId, Alice, 2);
        await PutCultureFitScore(appId, Alice, 5);

        var profile = await _client.GetFromJsonAsync<ApplicationProfileResponse>(
            $"/api/applications/{appId}");

        var score = Assert.Single(profile!.Scores);
        Assert.Equal(5, score.Score);
        Assert.NotNull(score.UpdatedAt);
        Assert.Equal("Alice Johnson", score.UpdatedByName);
    }

    [Fact]
    public async Task Applying_twice_with_the_same_email_returns_409()
    {
        var email = $"dup-{Guid.NewGuid()}@test.example";
        var (jobId, _) = await CreateJobAndApplication(email);

        var response = await _client.PostAsJsonAsync(
            $"/api/jobs/{jobId}/applications",
            new { candidateName = "Second Person", candidateEmail = email });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    // — helpers —

    private async Task<(Guid jobId, Guid appId)> CreateJobAndApplication(string? email = null)
    {
        var jobResp = await _client.PostAsJsonAsync("/api/jobs", new
        {
            title = "Test Role",
            description = "Automated test job.",
            location = "Remote",
        });
        jobResp.EnsureSuccessStatusCode();
        var job = await jobResp.Content.ReadFromJsonAsync<JobResponse>();

        var candidateEmail = email ?? $"candidate-{Guid.NewGuid()}@test.example";
        var appResp = await _client.PostAsJsonAsync(
            $"/api/jobs/{job!.Id}/applications",
            new { candidateName = "Test Candidate", candidateEmail });
        appResp.EnsureSuccessStatusCode();
        var app = await appResp.Content.ReadFromJsonAsync<ApplicationSummaryResponse>();

        return (job.Id, app!.Id);
    }

    private async Task PutCultureFitScore(Guid appId, Guid memberId, int score)
    {
        var req = new HttpRequestMessage(HttpMethod.Put,
            $"/api/applications/{appId}/scores/culture-fit");
        req.Headers.Add("X-Team-Member-Id", memberId.ToString());
        req.Content = JsonContent.Create(new { score });

        var resp = await _client.SendAsync(req);
        resp.EnsureSuccessStatusCode();
    }
}
