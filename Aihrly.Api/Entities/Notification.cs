namespace Aihrly.Api.Entities;

public class Notification
{
    public Guid Id { get; set; }
    public Guid ApplicationId { get; set; }
    public Application Application { get; set; } = null!;
    // "hired" or "rejected"
    public string Type { get; set; } = string.Empty;
    public DateTime SentAt { get; set; }
}
