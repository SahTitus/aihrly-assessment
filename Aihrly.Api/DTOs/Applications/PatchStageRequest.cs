using System.ComponentModel.DataAnnotations;
using Aihrly.Api.Entities;

namespace Aihrly.Api.DTOs.Applications;

public class PatchStageRequest
{
    [Required]
    public ApplicationStage? TargetStage { get; set; }

    public string? Reason { get; set; }
}
