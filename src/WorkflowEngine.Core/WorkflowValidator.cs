using System.Text.Json;

namespace WorkflowEngine.Core;

public sealed class WorkflowValidator(OperationCatalog catalog)
{
    public static bool Supports(string type) => type is "Event" or "Condition" or "Timer" or "Action";

    public IReadOnlyList<string> Validate(WorkflowDiagram diagram)
    {
        var errors = new List<string>();
        var nodes = new Dictionary<string, DiagramNode>(StringComparer.Ordinal);
        foreach (var node in diagram.Nodes)
        {
            if (string.IsNullOrWhiteSpace(node.Id)) errors.Add("missing node identifier");
            else if (!nodes.TryAdd(node.Id, node)) errors.Add($"{node.Id}: duplicate identifier");
            ValidateNode(node, errors);
        }
        var starts = nodes.Values.Where(n => n.Type == "Event").ToArray();
        if (starts.Length == 0) errors.Add("missing start event");
        if (starts.Length > 1) errors.Add("multiple start events");
        var outgoing = nodes.Keys.ToDictionary(id => id, _ => new List<Edge>());
        foreach (var edge in diagram.Edges)
        {
            if (edge.Source == null || edge.Target == null || !nodes.ContainsKey(edge.Source) || !nodes.ContainsKey(edge.Target))
            {
                errors.Add($"unknown endpoint on edge {edge.Source} to {edge.Target}");
                continue;
            }
            outgoing[edge.Source].Add(edge);
        }
        foreach (var node in nodes.Values)
        {
            var edges = outgoing[node.Id];
            if (node.Type == "Condition")
            {
                if (edges.Any(e => e.Output is not ("yes" or "no")) || edges.GroupBy(e => e.Output).Any(g => g.Count() > 1))
                    errors.Add($"{node.Id}: expected at most one yes and one no output");
            }
            else if (edges.Count > 1) errors.Add($"{node.Id}: expected at most one output");
        }
        var reached = new HashSet<string>();
        var pending = new Stack<string>(starts.Select(n => n.Id));
        while (pending.TryPop(out var id))
            if (reached.Add(id))
                foreach (var edge in outgoing[id]) pending.Push(edge.Target);
        foreach (var id in nodes.Keys.Except(reached)) errors.Add($"{id}: unreachable node");
        DetectCycles(outgoing, errors);
        return errors;
    }

    private static void DetectCycles(Dictionary<string, List<Edge>> outgoing, List<string> errors)
    {
        var finished = new HashSet<string>();
        var path = new List<string>();
        var active = new Dictionary<string, int>();
        void Visit(string id)
        {
            if (finished.Contains(id)) return;
            if (active.TryGetValue(id, out var index))
            {
                errors.Add("cycle: " + string.Join(" -> ", path.Skip(index).Append(id)));
                return;
            }
            active.Add(id, path.Count);
            path.Add(id);
            foreach (var edge in outgoing[id]) Visit(edge.Target);
            path.RemoveAt(path.Count - 1);
            active.Remove(id);
            finished.Add(id);
        }
        foreach (var id in outgoing.Keys) Visit(id);
    }

    private void ValidateNode(DiagramNode node, List<string> errors)
    {
        var parameters = node.Parameters;
        string? Field(string name) =>
            parameters.ValueKind == JsonValueKind.Object && parameters.TryGetProperty(name, out var value)
            && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

        void Require(string name)
        {
            if (parameters.ValueKind != JsonValueKind.Object || !parameters.TryGetProperty(name, out var value)
                || value.ValueKind == JsonValueKind.Null || (value.ValueKind == JsonValueKind.String && string.IsNullOrWhiteSpace(value.GetString())))
                errors.Add($"{node.Id}: missing {name}");
        }

        switch (node.Type)
        {
            case "Event":
                Require("eventType");
                break;
            case "Timer":
                if (parameters.ValueKind != JsonValueKind.Object || !parameters.TryGetProperty("durationSeconds", out var duration)
                    || duration.ValueKind != JsonValueKind.Number || !duration.TryGetDouble(out var seconds)
                    || seconds <= 0 || seconds > TimeSpan.FromDays(90).TotalSeconds)
                    errors.Add($"{node.Id}: timer must be positive and at most 90 days");
                break;
            case "Condition":
                Require("path");
                if (Field("operator") is not ("=" or "≠" or ">" or "≥" or "<" or "≤" or "contains" or "exists"))
                    errors.Add($"{node.Id}: unknown condition operator");
                if (Field("operator") != "exists") Require("value");
                break;
            case "Action":
                var operation = catalog.Find(node.Operation ?? "");
                if (operation == null) errors.Add($"{node.Id}: unknown action {node.Operation}");
                else foreach (var input in operation.RequiredInputs) Require(input);
                if (node.Operation == "Call webhook" && (!Uri.TryCreate(Field("url"), UriKind.Absolute, out var uri)
                    || uri.Scheme is not ("http" or "https") || string.IsNullOrEmpty(uri.Host)))
                    errors.Add($"{node.Id}: invalid webhook URL");
                break;
            default:
                errors.Add($"{node.Id}: unsupported node type {node.Type}");
                break;
        }
    }
}
