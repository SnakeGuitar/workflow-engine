using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Caching.Memory;

namespace WorkflowEngine.Api;

internal sealed class SessionTickets(IMemoryCache cache, TimeProvider clock) : ITicketStore
{
    public Task<string> StoreAsync(AuthenticationTicket ticket)
    {
        var key = Guid.NewGuid().ToString("N");
        cache.Set(key, ticket, ticket.Properties.ExpiresUtc!.Value - clock.GetUtcNow());
        return Task.FromResult(key);
    }

    public Task RenewAsync(string key, AuthenticationTicket ticket)
    {
        if (cache.TryGetValue(key, out _)) cache.Set(key, ticket, ticket.Properties.ExpiresUtc!.Value - clock.GetUtcNow());
        return Task.CompletedTask;
    }

    public Task<AuthenticationTicket?> RetrieveAsync(string key) =>
        Task.FromResult(cache.Get<AuthenticationTicket>(key));

    public Task RemoveAsync(string key)
    {
        cache.Remove(key);
        return Task.CompletedTask;
    }
}
