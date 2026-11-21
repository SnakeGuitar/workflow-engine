using System.Reflection;
using NetArchTest.Rules;
using Xunit;

namespace WorkflowEngine.Tests.Architecture;

public class DependencyTests
{
    [Fact]
    public void CoreIsIndependent()
    {
        var assembly = Assembly.Load("WorkflowEngine.Core");
        string[] forbidden = ["WorkflowEngine.Infrastructure", "WorkflowEngine.Api",
            "Microsoft.EntityFrameworkCore", "Microsoft.AspNetCore", "System.Net.Http"];
        var result = Types.InAssembly(assembly).ShouldNot().HaveDependencyOnAny(forbidden).GetResult();
        Assert.True(result.IsSuccessful, "AR-01: Move database, HTTP and hosting dependencies outside Core.");
    }
}
