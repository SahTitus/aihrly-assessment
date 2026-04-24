using Aihrly.Api.DTOs.Notes;
using Aihrly.Api.DTOs.Scores;
using Aihrly.Api.DTOs.StageHistory;

namespace Aihrly.Api.DTOs.Applications;

public class ApplicationProfileResponse
{
    public Guid Id { get; set; }
    public Guid JobId { get; set; }
    public string JobTitle { get; set; } = string.Empty;
    public string CandidateName { get; set; } = string.Empty;
    public string CandidateEmail { get; set; } = string.Empty;
    public string? CoverLetter { get; set; }
    public string CurrentStage { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public List<ScoreResponse> Scores { get; set; } = [];
    public List<NoteResponse> Notes { get; set; } = [];
    public List<StageHistoryResponse> StageHistory { get; set; } = [];
}
