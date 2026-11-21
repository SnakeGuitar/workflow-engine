using System.Text.Json;
using WorkflowEngine.Core;
using Xunit;

namespace WorkflowEngine.Tests.Unit;

public class CompilationTests
{
    [Fact]
    public void ActivationPinsDeterministicExecutableDocument()
    {
        var start = new DiagramNode("start", "Event", JsonSerializer.SerializeToElement(new { eventType = "created" }));
        var action = new DiagramNode("send", "Action", JsonSerializer.SerializeToElement(new { phone = "123", text = "hello" }),
            new Position(5, 8), "Send SMS");
        WorkflowDiagram diagram = new([action, start], [new("start", "send")]);
        var compiler = new WorkflowCompiler(OperationCatalog.BuiltIn);
        var first = compiler.Activate(diagram, 0).Version!;
        var next = compiler.Activate(diagram, first.Number).Version!;
        Assert.Equal(2, next.Number);
        Assert.Equal("start", first.Document.StartNode);
        Assert.Equal<string>(["start", "send"], first.Document.TopologicalOrder);
        Assert.Equal("1", first.Document.Nodes.Single(n => n.Id == "send").OperationVersion);
        Assert.Equal(JsonSerializer.SerializeToUtf8Bytes(first.Document), JsonSerializer.SerializeToUtf8Bytes(next.Document));
        Assert.Null(compiler.Activate(diagram with { Edges = [] }, next.Number).Version);
    }
}
