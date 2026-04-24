namespace Aihrly.Api.Entities;

public class ApplicationNote
{
    public Guid Id { get; set; }
    public Guid ApplicationId { get; set; }
    public Application Application { get; set; } = null!;
    public NoteType Type { get; set; }
    public string Description { get; set; } = string.Empty;
    public Guid CreatedById { get; set; }
    public TeamMember CreatedBy { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
}
