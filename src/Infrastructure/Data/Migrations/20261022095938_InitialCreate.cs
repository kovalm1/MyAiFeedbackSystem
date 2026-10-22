using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyAiFeedbackSystem.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Reviews",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    AuthorName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    AuthorEmail = table.Column<string>(type: "TEXT", maxLength: 320, nullable: false),
                    Content = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAtUtc = table.Column<long>(type: "INTEGER", nullable: false),
                    Sentiment = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    Priority = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    Category = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    NeedUrgentResponse = table.Column<bool>(type: "INTEGER", nullable: true),
                    Summary = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    DraftResponse = table.Column<string>(type: "TEXT", maxLength: 10000, nullable: true),
                    AIProviderName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    AIModelName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    AnalyzedAtUtc = table.Column<long>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reviews", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_CreatedAtUtc",
                table: "Reviews",
                column: "CreatedAtUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Reviews");
        }
    }
}
