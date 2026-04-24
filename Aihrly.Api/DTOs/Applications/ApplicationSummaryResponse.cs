namespace Aihrly.Api.DTOs.Applications;

public class ApplicationSummaryResponse
{
    public Guid Id { get; set; }
    public string CandidateName { get; set; } = string.Empty;
    public string CandidateEmail { get; set; } = string.Empty;
    public string CurrentStage { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
