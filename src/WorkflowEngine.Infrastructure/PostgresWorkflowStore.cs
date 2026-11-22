using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using WorkflowEngine.Core;

namespace WorkflowEngine.Infrastructure;

public sealed class PostgresWorkflowStore(WorkflowDatabase db, WorkflowCompiler compiler) : IWorkflowStore
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private static SavedWorkflow View(StoredWorkflow row) => new(row.Id, row.OwnerId, row.State,
        row.Version, JsonSerializer.Deserialize<WorkflowDiagram>(row.Diagram, Json)!);

    public async Task<IReadOnlyList<SavedWorkflow>> ListAsync(CancellationToken cancellation) =>
        (await db.Set<StoredWorkflow>().AsNoTracking().OrderBy(row => row.Id).ToListAsync(cancellation))
        .Select(View).ToArray();

    public async Task<SavedWorkflow?> FindAsync(Guid id, CancellationToken cancellation)
    {
        var row = await db.Set<StoredWorkflow>().AsNoTracking().SingleOrDefaultAsync(w => w.Id == id, cancellation);
        return row == null ? null : View(row);
    }

    public async Task<SavedWorkflow> CreateAsync(string ownerId, WorkflowDiagram diagram, CancellationToken cancellation)
    {
        var row = new StoredWorkflow { Id = Guid.NewGuid(), OwnerId = ownerId, Diagram = JsonSerializer.Serialize(diagram, Json) };
        db.Add(row);
        await db.SaveChangesAsync(cancellation);
        return View(row);
    }

    public async Task<WorkflowChange> ChangeAsync(Guid id, WorkflowDiagram? replacement,
        WorkflowCommand command, CancellationToken cancellation)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellation);
        var row = await db.Set<StoredWorkflow>().FromSqlInterpolated(
            $"""SELECT * FROM "Workflows" WHERE "Id" = {id} FOR UPDATE""").SingleOrDefaultAsync(cancellation);
        if (row == null) return new(null, []);
        switch (command)
        {
            case WorkflowCommand.Save:
                row.Diagram = JsonSerializer.Serialize(replacement, Json);
                break;
            case WorkflowCommand.Activate:
                var compiled = compiler.Activate(View(row).Diagram, row.Version);
                if (compiled.Version == null) return new(null, compiled.Errors);
                row.State = "Active";
                row.Version = compiled.Version.Number;
                db.Add(new StoredVersion { WorkflowId = id, Number = row.Version,
                    Document = JsonSerializer.Serialize(compiled.Version.Document, Json) });
                break;
            case WorkflowCommand.Deactivate:
                if (row.State != "Active") return new(null, ["Only active workflows can be deactivated."]);
                row.State = "Inactive";
                break;
        }
        await db.SaveChangesAsync(cancellation);
        await transaction.CommitAsync(cancellation);
        return new(View(row), []);
    }
}
