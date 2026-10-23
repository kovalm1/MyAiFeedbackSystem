using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Identity;
using System.Threading.RateLimiting;

namespace MyAiFeedbackSystem.WebApp.Auth;

public static class AdminAuthentication
{
    public const string Policy = "Administrator";
    public const string CsrfHeader = "X-CSRF-TOKEN";

    public static void Configure(WebApplicationBuilder builder)
    {
        builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                options.Cookie.Name = "MyAiFeedbackSystem.Admin";
                options.Cookie.HttpOnly = true;
                options.Cookie.SameSite = SameSiteMode.Strict;
                options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
                options.LoginPath = "/login";
                options.ExpireTimeSpan = TimeSpan.FromHours(8);
                options.Events.OnRedirectToLogin = context =>
                {
                    if (context.Request.Path.StartsWithSegments("/api"))
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    else
                        context.Response.Redirect(context.RedirectUri);
                    return Task.CompletedTask;
                };
                options.Events.OnRedirectToAccessDenied = context =>
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    return Task.CompletedTask;
                };
            });
        builder.Services.AddAuthorizationBuilder()
            .AddPolicy(Policy, policy => policy.RequireClaim(ClaimTypes.Role, Policy));
        builder.Services.AddAntiforgery(options => options.HeaderName = CsrfHeader);
        builder.Services.AddSingleton<IPasswordHasher<string>, PasswordHasher<string>>();
        builder.Services.AddRateLimiter(options =>
            options.AddPolicy("admin-login", context => RateLimitPartition.GetFixedWindowLimiter(
                context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 5,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0
                })));
    }

    public static async Task<IResult?> ValidateCsrfAsync(HttpContext context, IAntiforgery antiforgery)
    {
        try
        {
            await antiforgery.ValidateRequestAsync(context);
            return null;
        }
        catch (AntiforgeryValidationException)
        {
            return Results.BadRequest(new { message = "Invalid CSRF token." });
        }
    }
}
