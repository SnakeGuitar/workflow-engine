using System.Collections.Immutable;
using System.Text.Json;

namespace WorkflowEngine.Core;

public sealed record Position(double X, double Y);
public sealed record DiagramNode(string Id, string Type, JsonElement Parameters,
    Position? Position = null, string? Operation = null, string? ConnectionId = null);
public sealed record Edge(string Source, string Target, string? Output = null);
public sealed record WorkflowDiagram(DiagramNode[] Nodes, Edge[] Edges);

public sealed record Operation(string Name, string Version, string AdapterDigest, ImmutableArray<string> RequiredInputs);

public sealed class OperationCatalog(IEnumerable<Operation> operations)
{
    private readonly ImmutableDictionary<string, Operation> entries =
        operations.ToImmutableDictionary(op => op.Name, StringComparer.Ordinal);

    public Operation? Find(string name) => entries.GetValueOrDefault(name);

    public static OperationCatalog BuiltIn { get; } = new([
        new("Send email", "1", "", ["recipient", "subject", "body"]),
        new("Send SMS", "1", "", ["phone", "text"]),
        new("Call webhook", "1", "", ["url"])
    ]);
}
