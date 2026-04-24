namespace Aihrly.Api.DTOs.Scores;

public class ScoreResponse
{
    public string Dimension { get; set; } = string.Empty;
    public int Score { get; set; }
    public string? Comment { get; set; }
    public string SetByName { get; set; } = string.Empty;
    public DateTime SetAt { get; set; }
    public string? UpdatedByName { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
