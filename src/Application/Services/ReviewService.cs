using System.ComponentModel.DataAnnotations;
using MyAiFeedbackSystem.Application.Abstractions;
using MyAiFeedbackSystem.Core.Entities;
using MyAiFeedbackSystem.Core.Models;

namespace MyAiFeedbackSystem.Application.Services;

public sealed class ReviewService(IReviewRepository repository, TimeProvider? timeProvider = null)
{
    private readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;

    public async Task<Review> CreateAsync(string authorName, string authorEmail, string content,
        CancellationToken ct = default)
    {
        var review = new Review
        {
            Id = Guid.NewGuid(),
            AuthorName = ValidateRequired(authorName, ReviewFieldLimits.AuthorName, nameof(authorName)),
            AuthorEmail = ValidateEmail(authorEmail),
            Content = ValidateRequired(content, null, nameof(content)),
            CreatedAtUtc = _timeProvider.GetUtcNow()
        };

        await repository.AddAsync(review, ct);
        return review;
    }

    public Task<IReadOnlyList<Review>> GetAllAsync(CancellationToken ct = default) =>
        repository.GetAllAsync(ct);

    public Task<Review?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        repository.GetByIdAsync(ValidateId(id), ct);

    public async Task UpdateAsync(Guid id, string authorName, string authorEmail, string content,
        CancellationToken ct = default)
    {
        var name = ValidateRequired(authorName, ReviewFieldLimits.AuthorName, nameof(authorName));
        var email = ValidateEmail(authorEmail);
        var text = ValidateRequired(content, null, nameof(content));

        if (!await repository.UpdateAsync(ValidateId(id), name, email, text, ct))
            throw new KeyNotFoundException($"Review {id} was not found.");
    }

    public async Task SaveAnalysisAsync(Guid id, AIReviewAnalysisResult analysis,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(analysis);
        var validated = analysis with
        {
            Category = ValidateRequired(analysis.Category, ReviewFieldLimits.Category, nameof(analysis.Category)),
            Summary = ValidateRequired(analysis.Summary, ReviewFieldLimits.Summary, nameof(analysis.Summary)),
            ProviderName = ValidateRequired(analysis.ProviderName, ReviewFieldLimits.AIProviderName, nameof(analysis.ProviderName)),
            ModelName = ValidateRequired(analysis.ModelName, ReviewFieldLimits.AIModelName, nameof(analysis.ModelName))
        };

        if (!await repository.SaveAnalysisAsync(ValidateId(id), validated, _timeProvider.GetUtcNow(), ct))
            throw new KeyNotFoundException($"Review {id} was not found.");
    }

    public async Task SaveDraftResponseAsync(Guid id, string? draftResponse,
        CancellationToken ct = default)
    {
        if (draftResponse?.Length > ReviewFieldLimits.DraftResponse)
            throw new ArgumentException($"Maximum length is {ReviewFieldLimits.DraftResponse} characters.", nameof(draftResponse));

        if (!await repository.SaveDraftResponseAsync(ValidateId(id), draftResponse, ct))
            throw new KeyNotFoundException($"Review {id} was not found.");
    }

    private static Guid ValidateId(Guid id) =>
        id == Guid.Empty ? throw new ArgumentException("Review ID cannot be empty.", nameof(id)) : id;

    private static string ValidateEmail(string? value)
    {
        var email = ValidateRequired(value, ReviewFieldLimits.AuthorEmail, "authorEmail");
        if (!new EmailAddressAttribute().IsValid(email))
            throw new ArgumentException("A valid email address is required.", "authorEmail");
        return email;
    }

    private static string ValidateRequired(string? value, int? maxLength, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Value is required.", parameterName);

        var trimmed = value.Trim();
        if (maxLength is int limit && trimmed.Length > limit)
            throw new ArgumentException($"Maximum length is {limit} characters.", parameterName);

        return trimmed;
    }
}
