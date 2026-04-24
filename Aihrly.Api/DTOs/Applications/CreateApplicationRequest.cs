using System.ComponentModel.DataAnnotations;

namespace Aihrly.Api.DTOs.Applications;

public class CreateApplicationRequest
{
    [Required, MaxLength(200)]
    public string CandidateName { get; set; } = string.Empty;

    [Required, EmailAddress, MaxLength(200)]
    public string CandidateEmail { get; set; } = string.Empty;

    public string? CoverLetter { get; set; }
}
