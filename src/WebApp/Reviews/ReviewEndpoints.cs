using System.ComponentModel.DataAnnotations;
using MyAiFeedbackSystem.Application.Services;

namespace MyAiFeedbackSystem.WebApp.Reviews;

public static class ReviewEndpoints
{
    public static IEndpointRouteBuilder MapReviewEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/reviews", async (CreateReviewRequest? request, ReviewService reviews, CancellationToken ct) =>
        {
            if (request is null)
                return Results.BadRequest(new { message = "Заповніть форму відгуку." });

            var validationResults = new List<ValidationResult>();
            if (!Validator.TryValidateObject(request, new ValidationContext(request), validationResults,
                    validateAllProperties: true))
            {
                var errors = validationResults
                    .SelectMany(result => result.MemberNames.DefaultIfEmpty(string.Empty)
                        .Select(member => new { member, message = result.ErrorMessage ?? "Некоректне значення." }))
                    .GroupBy(error => error.member)
                    .ToDictionary(group => group.Key, group => group.Select(error => error.message).ToArray());
                return Results.ValidationProblem(errors);
            }

            try
            {
                var review = await reviews.CreateAsync(request.AuthorName!, request.AuthorEmail!, request.Content!, ct);
                return Results.Created($"/api/reviews/{review.Id}", new { review.Id });
            }
            catch (ArgumentException)
            {
                return Results.BadRequest(new { message = "Перевірте введені дані відгуку." });
            }
        });

        return app;
    }
}
