using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace WorkflowEngine.Infrastructure;

public sealed class Account : IdentityUser
{
    public required string Role { get; set; }
}

public sealed class WorkflowDatabase(DbContextOptions<WorkflowDatabase> options)
    : IdentityUserContext<Account>(options)
{
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.Entity<Account>().Property(a => a.Role).HasMaxLength(6);
    }
}

public sealed class DesignFactory : IDesignTimeDbContextFactory<WorkflowDatabase>
{
    public WorkflowDatabase CreateDbContext(string[] args) => new(new DbContextOptionsBuilder<WorkflowDatabase>()
        .UseNpgsql().Options);
}
