using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace WorkflowEngine.Infrastructure;

public static class Accounts
{
    public static void AddAccounts(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<WorkflowDatabase>(options =>
            options.UseNpgsql(configuration.GetConnectionString("WorkflowDatabase")));
        services.AddIdentityCore<Account>(options =>
        {
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
        }).AddEntityFrameworkStores<WorkflowDatabase>();
    }

    public static async Task InitializeAsync(IServiceProvider services, IConfiguration configuration)
    {
        using var scope = services.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<WorkflowDatabase>();
        await database.Database.MigrateAsync();
        var manager = scope.ServiceProvider.GetRequiredService<UserManager<Account>>();
        foreach (var section in configuration.GetSection("Accounts").GetChildren())
        {
            var name = section["Username"] ?? throw new InvalidOperationException("Account username required.");
            var role = section["Role"];
            if (role is not ("Editor" or "Reader")) throw new InvalidOperationException("Invalid installation role.");
            if (await manager.FindByNameAsync(name) != null) continue;
            var account = new Account { UserName = name, Role = role };
            var result = await manager.CreateAsync(account, section["Password"]
                ?? throw new InvalidOperationException("Initial account password required."));
            if (!result.Succeeded) throw new InvalidOperationException("Account creation failed: " +
                string.Join(", ", result.Errors.Select(error => error.Code)));
        }
    }
}
