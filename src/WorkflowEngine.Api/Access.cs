using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using WorkflowEngine.Infrastructure;

namespace WorkflowEngine.Api;

internal static class Access
{
    private const string Scheme = CookieAuthenticationDefaults.AuthenticationScheme;

    internal static void AddAccess(this IServiceCollection services)
    {
        services.AddMemoryCache();
        services.AddSingleton<SessionTickets>();
        services.AddScoped<AccountAuthentication>();
        services.AddAuthentication(Scheme).AddCookie(options =>
        {
            options.Cookie.HttpOnly = true;
            options.Cookie.SameSite = SameSiteMode.Strict;
            options.SlidingExpiration = false;
            options.Events.OnRedirectToLogin = context => Status(context, 401);
            options.Events.OnRedirectToAccessDenied = context => Status(context, 403);
        });
        services.AddOptions<CookieAuthenticationOptions>(Scheme)
            .Configure<SessionTickets, TimeProvider>((options, tickets, clock) =>
            {
                options.TimeProvider = clock;
                options.SessionStore = tickets;
            });
        services.AddAuthorizationBuilder()
            .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build())
            .AddPolicy("Read", policy => policy.RequireRole("Reader", "Editor"))
            .AddPolicy("Edit", policy => policy.RequireRole("Editor"));
    }

    private static Task Status(RedirectContext<CookieAuthenticationOptions> context, int status)
    {
        context.Response.StatusCode = status;
        return Task.CompletedTask;
    }

    internal sealed class Login
    {
        public required string Username { get; init; }
        public required string Password { get; init; }
    }

    internal static void MapSession(this WebApplication app)
    {
        app.MapPost("/api/session", async (Login login, AccountAuthentication accounts, HttpContext context) =>
        {
            if (string.IsNullOrWhiteSpace(login.Username) || string.IsNullOrEmpty(login.Password))
                return Results.Unauthorized();
            var user = await accounts.VerifyAsync(login.Username, login.Password, context.RequestAborted);
            if (user == null) return Results.Unauthorized();
            var identity = new ClaimsIdentity([
                new Claim(ClaimTypes.NameIdentifier, user.Id), new Claim(ClaimTypes.Name, user.UserName!),
                new Claim(ClaimTypes.Role, user.Role)], Scheme);
            await context.SignInAsync(Scheme, new ClaimsPrincipal(identity));
            return Results.Ok(new { username = user.UserName, role = user.Role });
        }).AllowAnonymous();
        app.MapGet("/api/session", (ClaimsPrincipal user) => new
        {
            username = user.Identity!.Name, role = user.FindFirstValue(ClaimTypes.Role)
        }).RequireAuthorization("Read");
        app.MapDelete("/api/session", async (HttpContext context) =>
        {
            await context.SignOutAsync(Scheme);
            return Results.NoContent();
        }).RequireAuthorization("Read");
    }

    internal static bool ValidIntegrationKey(HttpRequest request, IConfiguration configuration)
    {
        var expected = configuration["IntegrationKey"];
        if (string.IsNullOrEmpty(expected) || !request.Headers.TryGetValue("X-Integration-Key", out var supplied)
            || supplied.Count != 1) return false;
        return CryptographicOperations.FixedTimeEquals(SHA256.HashData(Encoding.UTF8.GetBytes(expected)),
            SHA256.HashData(Encoding.UTF8.GetBytes(supplied.ToString())));
    }
}
