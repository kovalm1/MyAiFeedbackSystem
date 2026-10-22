using System.Security.Claims;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;

namespace MyAiFeedbackSystem.WebApp;

public static class AdminAuthEndpoints
{
    public static IEndpointRouteBuilder MapAdminAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var auth = app.MapGroup("/api/auth");

        auth.MapGet("/session", (HttpContext context, IAntiforgery antiforgery) =>
        {
            var token = antiforgery.GetAndStoreTokens(context).RequestToken;
            return Results.Ok(new AdminSession(
                context.User.HasClaim(ClaimTypes.Role, AdminAuthentication.Policy), token));
        });

        auth.MapPost("/login", async (AdminLoginRequest request, HttpContext context,
            IAntiforgery antiforgery, IConfiguration configuration, IPasswordHasher<string> hasher) =>
        {
            if (await AdminAuthentication.ValidateCsrfAsync(context, antiforgery) is { } invalid)
                return invalid;

            const string username = "admin";
            var hash = configuration["Admin:PasswordHash"];
            if (string.IsNullOrWhiteSpace(hash))
                return Results.Problem("Administrator password is not configured.", statusCode: 503);

            PasswordVerificationResult verified;
            try
            {
                verified = hasher.VerifyHashedPassword(username, hash, request.Password ?? "");
            }
            catch (FormatException)
            {
                return Results.Problem("Administrator password hash is invalid.", statusCode: 503);
            }

            if (!string.Equals(request.Username, username, StringComparison.Ordinal)
                || verified == PasswordVerificationResult.Failed)
                return Results.Json(new { message = "Invalid credentials." }, statusCode: 401);

            var identity = new ClaimsIdentity(
                [new Claim(ClaimTypes.Name, username), new Claim(ClaimTypes.Role, AdminAuthentication.Policy)],
                CookieAuthenticationDefaults.AuthenticationScheme);
            await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identity));
            return Results.Ok();
        }).RequireRateLimiting("admin-login");

        auth.MapPost("/logout", async (HttpContext context, IAntiforgery antiforgery) =>
        {
            if (await AdminAuthentication.ValidateCsrfAsync(context, antiforgery) is { } invalid)
                return invalid;

            await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Results.Ok();
        }).RequireAuthorization(AdminAuthentication.Policy);

        return app;
    }
}

public sealed record AdminLoginRequest(string? Username, string? Password);
public sealed record AdminSession(bool IsAuthenticated, string? CsrfToken);
