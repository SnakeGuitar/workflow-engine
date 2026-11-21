using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Time.Testing;
using WorkflowEngine.Api;

namespace WorkflowEngine.Tests.Support;

public sealed class ApplicationFactory : WebApplicationFactory<Program>
{
    public FakeTimeProvider Clock { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder) =>
        builder.UseEnvironment("Testing").ConfigureServices(services => services.Replace(
            ServiceDescriptor.Singleton<TimeProvider>(Clock)));
}
