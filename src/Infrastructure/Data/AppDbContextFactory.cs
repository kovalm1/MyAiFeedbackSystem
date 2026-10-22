using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace MyAiFeedbackSystem.Infrastructure.Data;

public sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        // Design-time only. Run dotnet ef from the solution root; WebApp configuration comes in step 5.
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite("Data Source=reviews.db")
            .Options;

        return new AppDbContext(options);
    }
}
