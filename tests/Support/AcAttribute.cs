namespace WorkflowEngine.Tests.Support;

[AttributeUsage(AttributeTargets.Method)]
public sealed class AcAttribute(string id) : Attribute
{
    public string Id { get; } = id;
}
