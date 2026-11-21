namespace WorkflowEngine.Api;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Services.AddSingleton(TimeProvider.System);
        var app = builder.Build();
        app.Run();
    }
}
