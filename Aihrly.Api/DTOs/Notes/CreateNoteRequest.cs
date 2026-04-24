using System.ComponentModel.DataAnnotations;
using Aihrly.Api.Entities;

namespace Aihrly.Api.DTOs.Notes;

public class CreateNoteRequest
{
    [Required]
    public NoteType Type { get; set; }

    [Required]
    public string Description { get; set; } = string.Empty;
}
