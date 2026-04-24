using System.ComponentModel.DataAnnotations;

namespace Aihrly.Api.DTOs.Jobs;

public class CreateJobRequest
{
    [Required, MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Description { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string Location { get; set; } = string.Empty;
}
