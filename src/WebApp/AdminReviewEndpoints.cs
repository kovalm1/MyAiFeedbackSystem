using MyAiFeedbackSystem.Application.Abstractions;
using MyAiFeedbackSystem.Application.Services;
using MyAiFeedbackSystem.Core.Entities;
using MyAiFeedbackSystem.Infrastructure.AI;
using MyAiFeedbackSystem.WebApp.Client.Pages;
using Microsoft.AspNetCore.Antiforgery;

namespace MyAiFeedbackSystem.WebApp;

public static class AdminReviewEndpoints
{
    public static IEndpointRouteBuilder MapAdminReviewEndpoints(this IEndpointRouteBuilder app)
    {
        var reviews = app.MapGroup("/api/admin/reviews")
            .RequireAuthorization(AdminAuthentication.Policy)
            .AddEndpointFilter(async (context, next) =>
            {
                if (HttpMethods.IsPost(context.HttpContext.Request.Method))
                {
                    var antiforgery = context.HttpContext.RequestServices.GetRequiredService<IAntiforgery>();
                    if (await AdminAuthentication.ValidateCsrfAsync(context.HttpContext, antiforgery) is { } invalid)
                        return invalid;
                }

                return await next(context);
            });

        reviews.MapGet("/", async (ReviewService service, CancellationToken ct) =>
        {
            var items = await service.GetAllAsync(ct);
            return Results.Ok(items.Select(ToItem));
        });

        reviews.MapPost("/{id:guid}/analyze", async (Guid id, ReviewService service,
            IAIReviewAnalyzer analyzer, ILoggerFactory loggerFactory, CancellationToken ct) =>
        {
            var review = await service.GetByIdAsync(id, ct);
            if (review is null)
                return Results.NotFound();

            try
            {
                var result = await analyzer.AnalyzeAsync(review.Content, ct);
                await service.SaveAnalysisAsync(id, result, ct);
                var updated = await service.GetByIdAsync(id, ct);
                return updated is null ? Results.NotFound() : Results.Ok(ToItem(updated));
            }
            catch (AIProviderException ex)
            {
                loggerFactory.CreateLogger("AdminReviewEndpoints").LogWarning(ex,
                    "AI analysis failed for review {ReviewId}", id);
                return Results.Problem("AI-аналіз зараз недоступний. Спробуйте пізніше.", statusCode: 502);
            }
            catch (KeyNotFoundException)
            {
                return Results.NotFound();
            }
        });

        reviews.MapPost("/{id:guid}/draft", async (Guid id, ReviewService service,
            IAIResponseGenerator generator, ILoggerFactory loggerFactory, CancellationToken ct) =>
        {
            var review = await service.GetByIdAsync(id, ct);
            if (review is null)
                return Results.NotFound();

            try
            {
                var draft = await generator.GenerateResponseAsync(review.Content, review.AuthorName, ct);
                return Results.Ok(new GeneratedReviewDraft(draft));
            }
            catch (AIProviderException ex)
            {
                loggerFactory.CreateLogger("AdminReviewEndpoints").LogWarning(ex,
                    "AI draft generation failed for review {ReviewId}", id);
                return Results.Problem("Не вдалося згенерувати чернетку. Спробуйте пізніше.", statusCode: 502);
            }
        });

        return app;
    }

    private static AdminReviewItem ToItem(Review review) => new(
        review.Id, review.CreatedAtUtc, review.AuthorName, review.Content,
        review.Sentiment?.ToString(), review.Priority?.ToString(), review.Category,
        review.NeedUrgentResponse, review.Summary, review.AIProviderName,
        review.AIModelName, review.AnalyzedAtUtc);
}
