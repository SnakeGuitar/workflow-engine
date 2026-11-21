namespace WorkflowEngine.Infrastructure;

internal sealed class StoredWorkflow
{
    public Guid Id { get; set; }
    public required string OwnerId { get; set; }
    public required string Diagram { get; set; }
    public string State { get; set; } = "Draft";
    public int Version { get; set; }
}

internal sealed class StoredVersion
{
    public Guid WorkflowId { get; set; }
    public int Number { get; set; }
    public required string Document { get; set; }
}
