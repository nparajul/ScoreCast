using System.Reflection;
using FastEndpoints;
using ScoreCast.Ws.Application.V1.Interfaces;
using ScoreCast.Ws.Endpoints;
using ScoreCast.Ws.Infrastructure;
using Xunit;

namespace ScoreCast.ArchTests;

public class ConventionTests
{
    private static readonly Assembly _applicationAssembly = typeof(IQuery).Assembly;
    private static readonly Assembly _infrastructureAssembly = typeof(InfrastructureGroup).Assembly;
    private static readonly Assembly _endpointsAssembly = typeof(EndpointsGroup).Assembly;

    private static readonly string _srcRoot = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src"));

    // ── Query / Command interface rules ──

    [Fact]
    public void Queries_Must_Implement_IQuery()
    {
        var violations = _applicationAssembly.GetTypes()
            .Where(t => t.Name.EndsWith("Query") && t is { IsClass: true, IsAbstract: false })
            .Where(t => !ImplementsOpenGeneric(t, typeof(IQuery<>)) && !typeof(IQuery).IsAssignableFrom(t))
            .Select(t => t.Name)
            .ToList();

        Assert.True(violations.Count == 0,
            $"These Query types don't implement IQuery or IQuery<T>: {string.Join(", ", violations)}");
    }

    [Fact]
    public void Commands_Must_Implement_ICommand()
    {
        var violations = _applicationAssembly.GetTypes()
            .Where(t => t.Name.EndsWith("Command") && t is { IsClass: true, IsAbstract: false })
            .Where(t => !ImplementsOpenGeneric(t, typeof(ICommand<>)) && !typeof(ICommand).IsAssignableFrom(t))
            .Select(t => t.Name)
            .ToList();

        Assert.True(violations.Count == 0,
            $"These Command types don't implement ICommand or ICommand<T>: {string.Join(", ", violations)}");
    }

    [Fact]
    public void QueryHandlers_Must_Implement_IQueryHandler()
    {
        var violations = _infrastructureAssembly.GetTypes()
            .Where(t => t.Name.EndsWith("QueryHandler") && t is { IsClass: true, IsAbstract: false })
            .Where(t => !ImplementsOpenGeneric(t, typeof(IQueryHandler<,>)))
            .Select(t => t.Name)
            .ToList();

        Assert.True(violations.Count == 0,
            $"These QueryHandler types don't implement IQueryHandler<,>: {string.Join(", ", violations)}");
    }

    [Fact]
    public void CommandHandlers_Must_Implement_ICommandHandler()
    {
        var violations = _infrastructureAssembly.GetTypes()
            .Where(t => t.Name.EndsWith("CommandHandler") && t is { IsClass: true, IsAbstract: false })
            .Where(t => !ImplementsOpenGeneric(t, typeof(ICommandHandler<,>)))
            .Select(t => t.Name)
            .ToList();

        Assert.True(violations.Count == 0,
            $"These CommandHandler types don't implement ICommandHandler<,>: {string.Join(", ", violations)}");
    }

    [Fact]
    public void QueryHandlers_Must_Not_Implement_ICommandHandler_Directly()
    {
        var violations = _infrastructureAssembly.GetTypes()
            .Where(t => t.Name.EndsWith("QueryHandler") && t is { IsClass: true, IsAbstract: false })
            .Where(t => ImplementsOpenGenericDirectly(t, typeof(ICommandHandler<,>))
                        && !ImplementsOpenGeneric(t, typeof(IQueryHandler<,>)))
            .Select(t => t.Name)
            .ToList();

        Assert.True(violations.Count == 0,
            $"These QueryHandlers implement ICommandHandler instead of IQueryHandler: {string.Join(", ", violations)}");
    }

    [Fact]
    public void CommandHandlers_Must_Not_Implement_IQueryHandler()
    {
        var violations = _infrastructureAssembly.GetTypes()
            .Where(t => t.Name.EndsWith("CommandHandler") && t is { IsClass: true, IsAbstract: false })
            .Where(t => ImplementsOpenGeneric(t, typeof(IQueryHandler<,>)))
            .Select(t => t.Name)
            .ToList();

        Assert.True(violations.Count == 0,
            $"These CommandHandlers implement IQueryHandler instead of ICommandHandler: {string.Join(", ", violations)}");
    }

    // ── Handlers must be internal sealed ──

    [Fact]
    public void Handlers_Must_Be_Internal_Sealed()
    {
        var violations = _infrastructureAssembly.GetTypes()
            .Where(t => (t.Name.EndsWith("QueryHandler") || t.Name.EndsWith("CommandHandler"))
                        && t is { IsClass: true, IsAbstract: false })
            .Where(t => t.IsPublic || !t.IsSealed)
            .Select(t => $"{t.Name} ({(t.IsPublic ? "public" : "not sealed")})")
            .ToList();

        Assert.True(violations.Count == 0,
            $"Handlers must be internal sealed: {string.Join(", ", violations)}");
    }

    // ── Endpoint rules ──

    [Fact]
    public void Endpoints_Must_Not_Have_AllowAnonymous()
    {
        var endpointFiles = Directory.GetFiles(
            Path.Combine(_srcRoot, "APIs", "ScoreCast.Ws.Endpoints"), "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains("/obj/") && !f.Contains("/bin/"))
            .Where(f => !f.Contains("/Health/"));

        var violations = endpointFiles
            .Where(f => File.ReadAllText(f).Contains("AllowAnonymous"))
            .Select(Path.GetFileNameWithoutExtension)
            .ToList();

        Assert.True(violations.Count == 0,
            $"Endpoints must not use AllowAnonymous() (except Health): {string.Join(", ", violations)}");
    }

    // ── No DateTime.Now / DateTime.UtcNow — use ScoreCastDateTime.Now ──

    [Fact]
    public void No_Raw_DateTime_Now()
    {
        var csFiles = Directory.GetFiles(_srcRoot, "*.cs", SearchOption.AllDirectories)
            .Concat(Directory.GetFiles(_srcRoot, "*.razor", SearchOption.AllDirectories))
            .Where(f => !f.Contains("/obj/") && !f.Contains("/bin/"))
            .Where(f => !Path.GetFileName(f).StartsWith("ScoreCastDateTime"));

        var violations = new List<string>();
        foreach (var file in csFiles)
        {
            var lines = File.ReadAllLines(file);
            for (var i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                // Skip lines that use ScoreCastDateTime (which contains "DateTime" as substring)
                if (line.Contains("ScoreCastDateTime")) continue;
                if (line.Contains("DateTime.Now") || line.Contains("DateTime.UtcNow"))
                    violations.Add($"{Path.GetFileName(file)}:{i + 1}");
            }
        }

        Assert.True(violations.Count == 0,
            $"Use ScoreCastDateTime.Now instead of DateTime.Now/UtcNow:\n  {string.Join("\n  ", violations)}");
    }

    // ── No @code blocks in Razor — code-behind only ──

    [Fact]
    public void Razor_Files_Must_Not_Have_Code_Blocks()
    {
        var razorFiles = Directory.GetFiles(
            Path.Combine(_srcRoot, "Web"), "*.razor", SearchOption.AllDirectories)
            .Where(f => !f.Contains("/obj/") && !f.Contains("_Imports.razor"));

        var violations = razorFiles
            .Where(f => File.ReadAllText(f).Contains("@code"))
            .Select(Path.GetFileNameWithoutExtension)
            .ToList();

        Assert.True(violations.Count == 0,
            $"Razor files must use code-behind (.razor.cs), not @code blocks: {string.Join(", ", violations)}");
    }

    // ── Helpers ──

    private static bool ImplementsOpenGeneric(Type type, Type openGeneric) =>
        type.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == openGeneric);

    private static bool ImplementsOpenGenericDirectly(Type type, Type openGeneric) =>
        type.GetInterfaces()
            .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == openGeneric)
            .Any(i => !type.GetInterfaces()
                .Where(other => other != i && other.IsGenericType)
                .Any(other => other.GetInterfaces()
                    .Any(oi => oi.IsGenericType && oi.GetGenericTypeDefinition() == openGeneric)));
}
