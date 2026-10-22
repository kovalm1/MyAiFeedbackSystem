# Local SQLite database

Run EF commands from the solution root. The design-time `AppDbContextFactory` uses `Data Source=reviews.db`. With `src/Infrastructure` as the startup project, `dotnet ef database update --project src/Infrastructure/MyAiFeedbackSystem.Infrastructure.csproj --startup-project src/Infrastructure/MyAiFeedbackSystem.Infrastructure.csproj` creates `src/Infrastructure/reviews.db`. To migrate the WebApp database, use `--connection "Data Source=../WebApp/reviews.db"` as shown in the root README.

`reviews.db` and its SQLite sidecar files are excluded by the root `.gitignore`; they may contain local review data.
