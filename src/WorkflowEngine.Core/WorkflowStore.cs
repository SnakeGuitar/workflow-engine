namespace WorkflowEngine.Core;

public sealed record SavedWorkflow(Guid Id, string OwnerId, string State, int Version, WorkflowDiagram Diagram);
public sealed record WorkflowChange(SavedWorkflow? Workflow, IReadOnlyList<string> Errors);

public interface IWorkflowStore
{
    Task<IReadOnlyList<SavedWorkflow>> ListAsync(CancellationToken cancellation);
    Task<SavedWorkflow?> FindAsync(Guid id, CancellationToken cancellation);
    Task<SavedWorkflow> CreateAsync(string ownerId, WorkflowDiagram diagram, CancellationToken cancellation);
    Task<WorkflowChange> ChangeAsync(Guid id, WorkflowDiagram? replacement, WorkflowCommand command, CancellationToken cancellation);
}

public enum WorkflowCommand { Save, Activate, Deactivate }
