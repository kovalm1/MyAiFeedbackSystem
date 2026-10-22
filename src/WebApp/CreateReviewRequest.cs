using System.ComponentModel.DataAnnotations;
using MyAiFeedbackSystem.Core.Entities;

namespace MyAiFeedbackSystem.WebApp;

public sealed class CreateReviewRequest
{
    [Required]
    [MaxLength(ReviewFieldLimits.AuthorName)]
    public string? AuthorName { get; set; }

    [Required]
    [EmailAddress]
    [MaxLength(ReviewFieldLimits.AuthorEmail)]
    public string? AuthorEmail { get; set; }

    [Required]
    public string? Content { get; set; }
}
