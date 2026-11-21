using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;

namespace WorkflowEngine.Tests.Architecture;

public class SourceRules
{
    private static readonly DirectoryInfo Root = FindRoot();

    private static DirectoryInfo FindRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "WorkflowEngine.sln")))
            directory = directory.Parent;
        return directory ?? throw new DirectoryNotFoundException("Solution root missing.");
    }

    private static IEnumerable<FileInfo> Sources(DirectoryInfo folder)
    {
        foreach (var file in folder.EnumerateFiles()) yield return file;
        foreach (var child in folder.EnumerateDirectories())
        {
            if (child.Name is "bin" or "obj" or "dist" or "node_modules" or "Acceptance") continue;
            foreach (var file in Sources(child)) yield return file;
        }
    }

    [Fact]
    public void CodeFitsLineLimit()
    {
        foreach (var dir in new[] { "src", "tests", "tools", "harness" })
        {
            var folder = new DirectoryInfo(Path.Combine(Root.FullName, dir));
            if (!folder.Exists) continue;
            foreach (var file in Sources(folder).Where(f => f.Extension is ".cs" or ".ts" or ".tsx" or ".js" or ".py" or ".css" or ".sql"))
                Assert.True(File.ReadLines(file.FullName).Take(401).Count() <= 400,
                    $"AR-07: Split {file.Name} into files of at most 400 lines.");
        }
    }

    [Fact]
    public void ProductProjectsHaveOnlyAllowedReferences()
    {
        var allowed = new Dictionary<string, string[]>
        {
            ["WorkflowEngine.Api"] = ["WorkflowEngine.Infrastructure", "WorkflowEngine.Core"],
            ["WorkflowEngine.Infrastructure"] = ["WorkflowEngine.Core"],
            ["WorkflowEngine.Core"] = [],
            ["WorkflowEngine.Simulator"] = []
        };
        foreach (var dir in new[] { "src", "tests", "tools" })
        {
            var folder = new DirectoryInfo(Path.Combine(Root.FullName, dir));
            if (!folder.Exists) continue;
            foreach (var project in Sources(folder).Where(f => f.Extension == ".csproj"))
            {
                var name = Path.GetFileNameWithoutExtension(project.Name);
                foreach (var reference in XDocument.Load(project.FullName).Descendants("ProjectReference"))
                {
                    var target = Path.GetFileNameWithoutExtension(reference.Attribute("Include")!.Value.Replace('\\', '/'));
                    Assert.True(target != "WorkflowEngine.Simulator", "AR-16: Access Simulator over HTTP.");
                    Assert.True(!allowed.TryGetValue(name, out var list) || list.Contains(target),
                        $"AR-01: Remove the {name} to {target} project reference.");
                }
            }
        }
    }

    private static bool Sensitive(string name) =>
        new[] { "password", "credential", "integrationkey", "secret", "accesstoken", "refreshtoken" }
            .Any(word => name.Replace("_", "").Contains(word, StringComparison.OrdinalIgnoreCase));

    [Fact]
    public void ProductSourcesRespectBoundaries()
    {
        foreach (var file in Sources(new DirectoryInfo(Path.Combine(Root.FullName, "src"))).Where(f => f.Extension == ".cs"))
        {
            var root = CSharpSyntaxTree.ParseText(File.ReadAllText(file.FullName)).GetRoot();
            foreach (var type in root.DescendantNodes().OfType<TypeDeclarationSyntax>())
            {
                foreach (var method in type.Members.OfType<MethodDeclarationSyntax>())
                {
                    if (type.Identifier.Text.EndsWith("Interpreter") && method.Modifiers.Any(SyntaxKind.PublicKeyword))
                        Assert.True(method.ParameterList.Parameters.All(p => p.Type?.ToString().Contains("Diagram") != true)
                            && method.ParameterList.Parameters.Any(p => p.Type?.ToString().EndsWith("CompiledDag") == true),
                            "AR-17: Accept the compiled DAG in interpreter entry points.");
                    if (method.AttributeLists.ToString().Contains("LoggerMessage"))
                        Assert.False(method.ParameterList.Parameters.Any(p => Sensitive(p.ToString())),
                            "AR-05: Remove credentials from the logging signature.");
                }
                foreach (var call in type.Members.SelectMany(m => m.DescendantNodes()).OfType<InvocationExpressionSyntax>())
                    if (call.Expression is MemberAccessExpressionSyntax access && access.Name.Identifier.Text.StartsWith("Log"))
                        Assert.False(call.ArgumentList.Arguments.Any(a => Sensitive(a.ToString())),
                            "AR-05: Remove secret values from technical log arguments.");

                if (type.Identifier.Text == "ExecutionStateMachine") continue;
                foreach (var write in type.DescendantNodes().OfType<AssignmentExpressionSyntax>())
                    if (write.Right.DescendantNodesAndSelf().OfType<IdentifierNameSyntax>().Any(n => n.Identifier.Text == "ExecutionState"))
                        Assert.False(write.Left.ToString().Split('.').Last() is "State" or "ExecutionState",
                            "AR-18: Apply execution transitions through ExecutionStateMachine.");
            }
        }
    }
}
