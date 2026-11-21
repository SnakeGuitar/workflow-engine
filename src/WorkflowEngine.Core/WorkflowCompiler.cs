using System.Collections.Immutable;
using System.Text.Json;

namespace WorkflowEngine.Core;

public sealed record CompiledNode(string Id, string Type, JsonElement Parameters,
    string? Operation, string? OperationVersion, string? AdapterDigest, string? ConnectionId);

public sealed record CompiledDag(ImmutableArray<CompiledNode> Nodes, ImmutableArray<Edge> Edges,
    string StartNode, ImmutableArray<string> TopologicalOrder);

public sealed record Compilation(CompiledDag? Document, IReadOnlyList<string> Errors);
public sealed record WorkflowVersion(int Number, CompiledDag Document);
public sealed record Activation(WorkflowVersion? Version, IReadOnlyList<string> Errors);

public sealed class WorkflowCompiler(OperationCatalog catalog)
{
    private readonly WorkflowValidator validator = new(catalog);

    public Compilation Compile(WorkflowDiagram diagram)
    {
        var errors = validator.Validate(diagram);
        if (errors.Count > 0) return new(null, errors);

        var edges = diagram.Edges.OrderBy(e => e.Source, StringComparer.Ordinal)
            .ThenBy(e => e.Output, StringComparer.Ordinal).ThenBy(e => e.Target, StringComparer.Ordinal).ToImmutableArray();
        var nodes = diagram.Nodes.OrderBy(n => n.Id, StringComparer.Ordinal).Select(n =>
        {
            var operation = n.Operation == null ? null : catalog.Find(n.Operation);
            return new CompiledNode(n.Id, n.Type, n.Parameters.Clone(), n.Operation, operation?.Version,
                operation?.AdapterDigest, n.ConnectionId);
        }).ToImmutableArray();

        var start = nodes.Single(n => n.Type == "Event").Id;
        var outgoing = edges.ToLookup(e => e.Source);
        var incoming = nodes.ToDictionary(n => n.Id, _ => 0);
        foreach (var edge in edges) incoming[edge.Target]++;
        var ready = new SortedSet<string>(StringComparer.Ordinal) { start };
        var order = ImmutableArray.CreateBuilder<string>(nodes.Length);
        while (ready.Count > 0)
        {
            var id = ready.Min!;
            ready.Remove(id);
            order.Add(id);
            foreach (var edge in outgoing[id])
                if (--incoming[edge.Target] == 0) ready.Add(edge.Target);
        }
        return new(new(nodes, edges, start, order.MoveToImmutable()), []);
    }

    public Activation Activate(WorkflowDiagram diagram, int lastVersion)
    {
        var compiled = Compile(diagram);
        return new(compiled.Document == null ? null : new(checked(lastVersion + 1), compiled.Document), compiled.Errors);
    }
}
