using MyAiFeedbackSystem.Core.Entities;
using MyAiFeedbackSystem.Core.Models;

namespace MyAiFeedbackSystem.Application.Abstractions;

public interface IReviewRepository
{
    Task AddAsync(Review review, CancellationToken ct = default);
    Task<IReadOnlyList<Review>> GetAllAsync(CancellationToken ct = default);
    Task<Review?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<bool> UpdateAsync(Guid id, string authorName, string authorEmail, string content,
        CancellationToken ct = default);
    Task<bool> SaveAnalysisAsync(Guid id, AIReviewAnalysisResult analysis, DateTimeOffset analyzedAtUtc,
        CancellationToken ct = default);
    Task<bool> SaveDraftResponseAsync(Guid id, string? draftResponse, CancellationToken ct = default);
}
