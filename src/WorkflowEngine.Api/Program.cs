using WorkflowEngine.Infrastructure;

namespace WorkflowEngine.Api;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Services.AddSingleton(TimeProvider.System);
        builder.Services.AddAccounts(builder.Configuration);
        builder.Services.AddAccess();
        var app = builder.Build();
        if (!app.Environment.IsEnvironment("Testing") || app.Configuration.GetConnectionString("WorkflowDatabase") != null)
            await Accounts.InitializeAsync(app.Services, app.Configuration);
        app.Use(async (context, next) =>
        {
            if (context.Request.Path.Equals("/api/events", StringComparison.OrdinalIgnoreCase)
                && HttpMethods.IsPost(context.Request.Method) && !Access.ValidIntegrationKey(context.Request, app.Configuration))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return;
            }
            await next();
        });
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapSession();
        await app.RunAsync();
    }
}
