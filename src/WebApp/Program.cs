using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using MudBlazor.Services;
using MyAiFeedbackSystem.Application.Abstractions;
using MyAiFeedbackSystem.Application.Services;
using MyAiFeedbackSystem.Infrastructure.AI;
using MyAiFeedbackSystem.Infrastructure.Data;
using MyAiFeedbackSystem.WebApp;
using MyAiFeedbackSystem.WebApp.Components;
using MyAiFeedbackSystem.WebApp.Client.SessionState;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveWebAssemblyComponents();
builder.Services.AddMudServices();
builder.Services.AddHttpClient();
builder.Services.AddScoped(services => services.GetRequiredService<IHttpClientFactory>().CreateClient());

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
    throw new InvalidOperationException("ConnectionStrings:DefaultConnection is required.");

builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlite(connectionString));
builder.Services.AddScoped<IReviewRepository, EfReviewRepository>();
builder.Services.AddScoped<ReviewService>();
builder.Services.AddScoped<IClientSessionStateStore, BrowserSessionStateStore>();

var aiOptions = builder.Configuration.GetSection(AIOptions.SectionName).Get<AIOptions>() ?? new AIOptions();
AIConfiguration.Validate(aiOptions);
builder.Services.AddSingleton(aiOptions);

if (aiOptions.Provider == "Stub")
{
    builder.Services.AddScoped<IAIReviewAnalyzer, StubAIReviewAnalyzer>();
    builder.Services.AddScoped<IAIResponseGenerator, StubAIResponseGenerator>();
}
else
{
    builder.Services.AddHttpClient<OpenAIReviewAnalyzer>();
    builder.Services.AddHttpClient<OpenAIResponseGenerator>();
    builder.Services.AddTransient<IAIReviewAnalyzer>(services => services.GetRequiredService<OpenAIReviewAnalyzer>());
    builder.Services.AddTransient<IAIResponseGenerator>(services => services.GetRequiredService<OpenAIResponseGenerator>());
}

var app = builder.Build();

using (var startupScope = app.Services.CreateScope())
{
    _ = startupScope.ServiceProvider.GetRequiredService<ReviewService>();
    _ = startupScope.ServiceProvider.GetRequiredService<IAIReviewAnalyzer>();
    _ = startupScope.ServiceProvider.GetRequiredService<IAIResponseGenerator>();
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
else
{
    app.UseWebAssemblyDebugging();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapPost("/api/reviews", async (CreateReviewRequest? request, ReviewService reviews, CancellationToken ct) =>
{
    if (request is null)
        return Results.BadRequest(new { message = "Заповніть форму відгуку." });

    var validationResults = new List<ValidationResult>();
    if (!Validator.TryValidateObject(request, new ValidationContext(request), validationResults, validateAllProperties: true))
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
app.MapRazorComponents<App>()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(MyAiFeedbackSystem.WebApp.Client._Imports).Assembly);

app.Run();
