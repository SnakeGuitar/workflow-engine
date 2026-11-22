using WorkflowEngine.Core;
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
        builder.Services.AddSingleton(OperationCatalog.BuiltIn);
        builder.Services.AddSingleton<WorkflowCompiler>();
        builder.Services.AddScoped<IWorkflowStore, PostgresWorkflowStore>();
        var app = builder.Build();
        if (!app.Environment.IsEnvironment("Testing") || app.Configuration.GetConnectionString("WorkflowDatabase") != null)
            await Accounts.InitializeAsync(app.Services, app.Configuration);
        app.Use(async (context, next) =>
        {
            if (string.Equals(context.Request.Path.Value?.TrimEnd('/'), "/api/events", StringComparison.OrdinalIgnoreCase)
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
        app.MapWorkflows();
        await app.RunAsync();
    }
}
