using System.ComponentModel.DataAnnotations;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using MyAiFeedbackSystem.Application.Services;
using MyAiFeedbackSystem.Infrastructure.AI;
using MyAiFeedbackSystem.Infrastructure.Data;
using MyAiFeedbackSystem.WebApp.Reviews;

namespace MyAiFeedbackSystem.Tests;

public sealed class ReviewServiceTests
{
    [Theory]
    [InlineData("", "a@example.com", "Text")]
    [InlineData("Name", "invalid", "Text")]
    [InlineData("Name", "a@example.com", "  ")]
    public async Task Create_rejects_invalid_input(string name, string email, string content)
    {
        await using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite("Data Source=:memory:").Options);
        var service = new ReviewService(new EfReviewRepository(db));
        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(name, email, content));
    }

    [Fact]
    public void Request_validation_rejects_invalid_email()
    {
        var request = new CreateReviewRequest
        {
            AuthorName = "Name", AuthorEmail = "invalid", Content = "Text"
        };
        var errors = new List<ValidationResult>();
        Assert.False(Validator.TryValidateObject(request, new ValidationContext(request), errors, true));
        Assert.Contains(errors, error => error.MemberNames.Contains(nameof(request.AuthorEmail)));
    }

    [Fact]
    public async Task Create_read_update_and_analysis_round_trip_in_sqlite()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;
        Guid id;
        await using (var db = new AppDbContext(options))
        {
            await db.Database.EnsureCreatedAsync();
            var service = new ReviewService(new EfReviewRepository(db));
            var review = await service.CreateAsync("  Olena  ", " olena@example.com ", " Good service ");
            id = review.Id;
            Assert.NotEqual(Guid.Empty, id);
            Assert.Equal("Olena", review.AuthorName);
            Assert.Equal(TimeSpan.Zero, review.CreatedAtUtc.Offset);
            await service.UpdateAsync(id, "Olena", "olena@example.com", "Updated");
        }

        await using (var db = new AppDbContext(options))
        {
            var service = new ReviewService(new EfReviewRepository(db));
            var read = await service.GetByIdAsync(id);
            Assert.NotNull(read);
            Assert.Equal("Updated", read.Content);
            Assert.Single(await service.GetAllAsync());
            var analysis = await new StubAIReviewAnalyzer().AnalyzeAsync(read.Content);
            await service.SaveAnalysisAsync(id, analysis);
            await service.SaveDraftResponseAsync(id, "Draft");
        }

        await using (var db = new AppDbContext(options))
        {
            var read = await new ReviewService(new EfReviewRepository(db)).GetByIdAsync(id);
            Assert.NotNull(read);
            Assert.Equal("General", read.Category);
            Assert.Equal("Stub", read.AIProviderName);
            Assert.NotNull(read.AnalyzedAtUtc);
            Assert.Equal("Draft", read.DraftResponse);
        }
    }

    [Fact]
    public async Task Missing_review_update_throws()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection).Options);
        await db.Database.EnsureCreatedAsync();
        var service = new ReviewService(new EfReviewRepository(db));
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            service.UpdateAsync(Guid.NewGuid(), "Name", "name@example.com", "Text"));
    }

    [Fact]
    public async Task Save_analysis_rejects_blank_summary()
    {
        await using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite("Data Source=:memory:").Options);
        var service = new ReviewService(new EfReviewRepository(db));
        var analysis = await new StubAIReviewAnalyzer().AnalyzeAsync("Text");
        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.SaveAnalysisAsync(Guid.NewGuid(), analysis with { Summary = "  " }));
    }
}
