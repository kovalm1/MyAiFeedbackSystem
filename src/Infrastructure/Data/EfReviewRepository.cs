using Microsoft.EntityFrameworkCore;
using MyAiFeedbackSystem.Application.Abstractions;
using MyAiFeedbackSystem.Core.Entities;
using MyAiFeedbackSystem.Core.Enums;
using MyAiFeedbackSystem.Core.Models;

namespace MyAiFeedbackSystem.Infrastructure.Data;

public sealed class EfReviewRepository(AppDbContext db) : IReviewRepository
{
    public async Task AddAsync(Review review, CancellationToken ct = default)
    {
        db.Reviews.Add(review);
        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<Review>> GetAllAsync(CancellationToken ct = default) =>
        await db.Reviews.AsNoTracking().OrderByDescending(r => r.CreatedAtUtc).ToListAsync(ct);

    public Task<Review?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        db.Reviews.AsNoTracking().FirstOrDefaultAsync(r => r.Id == id, ct);

    public async Task<bool> UpdateAsync(Guid id, string authorName, string authorEmail, string content,
        CancellationToken ct = default) =>
        await db.Reviews.Where(r => r.Id == id).ExecuteUpdateAsync(setters => setters
            .SetProperty(r => r.AuthorName, authorName)
            .SetProperty(r => r.AuthorEmail, authorEmail)
            .SetProperty(r => r.Content, content), ct) > 0;

    public async Task<bool> SaveAnalysisAsync(Guid id, AIReviewAnalysisResult analysis,
        DateTimeOffset analyzedAtUtc, CancellationToken ct = default)
    {
        ReviewSentiment? sentiment = analysis.Sentiment;
        ReviewPriority? priority = analysis.Priority;
        return await db.Reviews.Where(r => r.Id == id).ExecuteUpdateAsync(setters => setters
            .SetProperty(r => r.Sentiment, sentiment)
            .SetProperty(r => r.Priority, priority)
            .SetProperty(r => r.Category, analysis.Category)
            .SetProperty(r => r.NeedUrgentResponse, analysis.NeedUrgentResponse)
            .SetProperty(r => r.Summary, analysis.Summary)
            .SetProperty(r => r.AIProviderName, analysis.ProviderName)
            .SetProperty(r => r.AIModelName, analysis.ModelName)
            .SetProperty(r => r.AnalyzedAtUtc, analyzedAtUtc), ct) > 0;
    }

    public async Task<bool> SaveDraftResponseAsync(Guid id, string? draftResponse,
        CancellationToken ct = default) =>
        await db.Reviews.Where(r => r.Id == id).ExecuteUpdateAsync(setters => setters
            .SetProperty(r => r.DraftResponse, draftResponse), ct) > 0;
}
