using System.ComponentModel.DataAnnotations;

namespace MyAiFeedbackSystem.WebApp.Client.Pages;

public sealed class SubmitReviewDraft
{
    [Required(ErrorMessage = "Вкажіть ім’я.")]
    [MaxLength(200, ErrorMessage = "Ім’я може містити не більше 200 символів.")]
    public string AuthorName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Вкажіть email.")]
    [EmailAddress(ErrorMessage = "Вкажіть коректний email.")]
    [MaxLength(320, ErrorMessage = "Email може містити не більше 320 символів.")]
    public string AuthorEmail { get; set; } = string.Empty;

    [Required(ErrorMessage = "Напишіть відгук.")]
    public string Content { get; set; } = string.Empty;
}
