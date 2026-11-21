using Microsoft.Extensions.DependencyInjection;
using WorkflowEngine.Tests.Support;
using Xunit;

namespace WorkflowEngine.Tests.Unit;

public class ApplicationFactoryTests
{
    [Fact]
    public void HostUsesTheControllableClock()
    {
        using var factory = new ApplicationFactory();
        var clock = factory.Services.GetRequiredService<TimeProvider>();
        var start = clock.GetUtcNow();
        factory.Clock.Advance(TimeSpan.FromHours(2));
        Assert.Equal(start.AddHours(2), clock.GetUtcNow());
    }
}
