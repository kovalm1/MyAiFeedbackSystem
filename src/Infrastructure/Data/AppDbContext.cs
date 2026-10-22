using Microsoft.EntityFrameworkCore;
using MyAiFeedbackSystem.Core.Entities;

namespace MyAiFeedbackSystem.Infrastructure.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Review> Reviews => Set<Review>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var review = modelBuilder.Entity<Review>();
        review.HasKey(r => r.Id);
        review.Property(r => r.AuthorName).IsRequired().HasMaxLength(ReviewFieldLimits.AuthorName);
        review.Property(r => r.AuthorEmail).IsRequired().HasMaxLength(ReviewFieldLimits.AuthorEmail);
        review.Property(r => r.Content).IsRequired();
        review.Property(r => r.CreatedAtUtc).IsRequired().HasConversion<long>();
        review.HasIndex(r => r.CreatedAtUtc);
        review.Property(r => r.Sentiment).HasConversion<string>().HasMaxLength(20);
        review.Property(r => r.Priority).HasConversion<string>().HasMaxLength(20);
        review.Property(r => r.Category).HasMaxLength(ReviewFieldLimits.Category);
        review.Property(r => r.Summary).HasMaxLength(ReviewFieldLimits.Summary);
        review.Property(r => r.DraftResponse).HasMaxLength(ReviewFieldLimits.DraftResponse);
        review.Property(r => r.AIProviderName).HasMaxLength(ReviewFieldLimits.AIProviderName);
        review.Property(r => r.AIModelName).HasMaxLength(ReviewFieldLimits.AIModelName);
        review.Property(r => r.AnalyzedAtUtc).HasConversion<long>();
    }
}
