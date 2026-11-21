using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace WorkflowEngine.Infrastructure;

public sealed class AccountAuthentication(WorkflowDatabase database, UserManager<Account> manager, TimeProvider clock)
{
    public async Task<Account?> VerifyAsync(string username, string password, CancellationToken cancellation)
    {
        var normalized = manager.NormalizeName(username);
        await using var transaction = await database.Database.BeginTransactionAsync(cancellation);
        var account = await database.Users.FromSqlInterpolated(
            $"""SELECT * FROM "AspNetUsers" WHERE "NormalizedUserName" = {normalized} FOR UPDATE""")
            .SingleOrDefaultAsync(cancellation);
        if (account == null || account.LockoutEnd > clock.GetUtcNow()) return null;

        var valid = await manager.CheckPasswordAsync(account, password);
        var options = manager.Options.Lockout;
        account.AccessFailedCount = valid ? 0 : account.AccessFailedCount + 1;
        account.LockoutEnd = null;
        if (account.AccessFailedCount >= options.MaxFailedAccessAttempts)
        {
            account.LockoutEnd = clock.GetUtcNow() + options.DefaultLockoutTimeSpan;
            account.AccessFailedCount = 0;
        }
        await database.SaveChangesAsync(cancellation);
        await transaction.CommitAsync(cancellation);
        return valid ? account : null;
    }
}
