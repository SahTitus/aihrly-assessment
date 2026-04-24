namespace Aihrly.Api.Entities;

public class ApplicationScore
{
    public Guid Id { get; set; }
    public Guid ApplicationId { get; set; }
    public Application Application { get; set; } = null!;
    public ScoreDimension Dimension { get; set; }
    public int Score { get; set; }
    public string? Comment { get; set; }
    public Guid SetById { get; set; }
    public TeamMember SetBy { get; set; } = null!;
    public DateTime SetAt { get; set; }
    // tracks who last updated it, not just who created it
    public Guid? UpdatedById { get; set; }
    public TeamMember? UpdatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
