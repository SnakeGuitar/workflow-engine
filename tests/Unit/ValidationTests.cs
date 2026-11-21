using System.Text.Json;
using WorkflowEngine.Core;
using Xunit;

namespace WorkflowEngine.Tests.Unit;

public class ValidationTests
{
    [Fact]
    public void ReportsMissingStartCycleAndInvalidDurationTogether()
    {
        var parameters = JsonSerializer.SerializeToElement(new { durationSeconds = 0 });
        WorkflowDiagram diagram = new([new("A", "Timer", parameters), new("B", "Timer", parameters)],
            [new("A", "B"), new("B", "A")]);
        var errors = new WorkflowValidator(OperationCatalog.BuiltIn).Validate(diagram);
        Assert.Contains("missing start event", errors);
        Assert.Contains("cycle: A -> B -> A", errors);
        Assert.Contains(errors, error => error.Contains("timer must be positive"));
        Assert.Contains("B: unreachable node", errors);
    }
}
