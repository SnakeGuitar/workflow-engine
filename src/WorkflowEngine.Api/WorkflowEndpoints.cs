using System.Security.Claims;
using System.Text.Json;
using WorkflowEngine.Core;

namespace WorkflowEngine.Api;

internal static class WorkflowEndpoints
{
    internal static void MapWorkflows(this WebApplication app)
    {
        var group = app.MapGroup("/api/workflows");
        group.MapGet("/", (IWorkflowStore store, CancellationToken ct) => store.ListAsync(ct)).RequireAuthorization("Read");
        group.MapGet("/{id:guid}", async (Guid id, IWorkflowStore store, CancellationToken ct) =>
            await store.FindAsync(id, ct) is { } row ? Results.Ok(row) : Results.NotFound()).RequireAuthorization("Read");

        group.MapPost("/", async (WorkflowDiagram diagram, ClaimsPrincipal user, IWorkflowStore store, CancellationToken ct) =>
        {
            if (CheckShape(diagram) is { } invalid) return invalid;
            var row = await store.CreateAsync(user.FindFirstValue(ClaimTypes.NameIdentifier)!, diagram, ct);
            return Results.Created($"/api/workflows/{row.Id}", row);
        }).RequireAuthorization("Edit");
        group.MapPut("/{id:guid}", async (Guid id, WorkflowDiagram diagram, IWorkflowStore store, CancellationToken ct) =>
            CheckShape(diagram) ?? Respond(await store.ChangeAsync(id, diagram, WorkflowCommand.Save, ct))).RequireAuthorization("Edit");
        group.MapPost("/{id:guid}/validate", async (Guid id, IWorkflowStore store, WorkflowCompiler compiler, CancellationToken ct) =>
            await store.FindAsync(id, ct) is { } row ? Results.Ok(compiler.Compile(row.Diagram)) : Results.NotFound())
            .RequireAuthorization("Edit");
        group.MapPost("/{id:guid}/activate", async (Guid id, IWorkflowStore store, CancellationToken ct) =>
            Respond(await store.ChangeAsync(id, null, WorkflowCommand.Activate, ct))).RequireAuthorization("Edit");
        group.MapPost("/{id:guid}/deactivate", async (Guid id, IWorkflowStore store, CancellationToken ct) =>
            Respond(await store.ChangeAsync(id, null, WorkflowCommand.Deactivate, ct))).RequireAuthorization("Edit");
    }

    private static IResult? CheckShape(WorkflowDiagram diagram)
    {
        if (diagram.Nodes == null || diagram.Edges == null || diagram.Edges.Any(e => e == null))
            return Results.BadRequest(new { error = "Provide arrays of nodes and edges." });
        foreach (var node in diagram.Nodes)
        {
            if (node == null || !WorkflowValidator.Supports(node.Type))
                return Results.BadRequest(new { error = $"{node?.Id}: unsupported node type" });
            if (node.Parameters.ValueKind != JsonValueKind.Object)
                return Results.BadRequest(new { error = $"{node.Id}: expected a parameters object" });
            if (node.ConnectionId != null)
                return Results.BadRequest(new { error = $"{node.Id}: unknown connection" });
        }
        return null;
    }

    private static IResult Respond(WorkflowChange change) => change.Errors.Count != 0
        ? Results.BadRequest(new { change.Errors }) : change.Workflow == null ? Results.NotFound() : Results.Ok(change.Workflow);
}
