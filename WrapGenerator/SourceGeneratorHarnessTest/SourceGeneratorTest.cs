using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using SourceGeneratorHarness;
using System.Text;

namespace SourceGeneratorHarnessTest;


public class TestAdditionalText(string path, string text) : AdditionalText
{
    public override string Path { get; } = path;
    public override SourceText GetText(CancellationToken cancellationToken = new())
    {
        return SourceText.From(text);
    }
}

public class TestSourceGeneratorContext : ISourceGeneratorContext
{
    public CancellationToken CancellationToken { get; }
    public IEnumerable<AdditionalText> AdditionalFiles { get; }

    public Dictionary<string, string> SourcesAdded = new();
    public void AddSource(string hintPath, string source)
    {
        if (SourcesAdded.ContainsKey(hintPath))
        {
            Assert.Fail($"Adding source a second time: [{hintPath}]");
        }
        SourcesAdded[hintPath] = source;
    }

    public TestSourceGeneratorContext(Dictionary<string, string> sourceFiles)
    {
        CancellationToken = CancellationToken.None;
        AdditionalFiles = sourceFiles.Select(kvp => new TestAdditionalText(kvp.Key, kvp.Value));
    }
}

[TestClass]
public sealed class SourceGeneratorTest
{
    [TestMethod]
    public void SystemIoTest()
    {
        // need to force FileSystemWatcher to be loaded.
        // ReSharper disable once UnusedVariable
        FileSystemWatcher w = new();
        SourceGenerator gen = new();
        TestSourceGeneratorContext context = new TestSourceGeneratorContext(new()
        {
            ["Namespaces.xml"] = @"<?xml version=""1.0"" encoding=""utf-8"" ?>
<Generate>
	<Namespace Namespace=""System.IO"" TargetNamespaceFormat=""Wrapped.System.IO"" />
<Namespace Namespace=""System.Diagnostics"" TargetNamespaceFormat=""Wrapped.System.Diagnostics""
		   IncludeOnly=""^(Process)|(ProcessStartInfo)$"" />
</Generate>"
        });
        gen.Execute(context);
        Console.WriteLine(context.SourcesAdded["Wrapped/System/IO/FileSystemWatcherWrap.cs"]);
    }

    [TestMethod]
    public void SystemDiagnosticsTest()
    {
        SourceGenerator gen = new();
        TestSourceGeneratorContext context = new TestSourceGeneratorContext(new()
        {
            ["Namespaces.xml"] = @"<?xml version=""1.0"" encoding=""utf-8"" ?>
<Generate>
	<Namespace Namespace=""System.Diagnostics"" TargetNamespaceFormat=""Wrapped.System.Diagnostics"" />
</Generate>"
        });
        gen.Execute(context);
        Console.WriteLine(context.SourcesAdded["Wrapped/System/Diagnostics/ProcessWrap.cs"]);
    }

    private void DoTestWithMultiple(string className, string? expected)
    {
        SourceGenerator gen = new();
        TestSourceGeneratorContext context = new TestSourceGeneratorContext(new()
        {
            ["Namespaces.xml"] = @"<?xml version=""1.0"" encoding=""utf-8"" ?>
<Generate>
	<Namespace Namespace=""WrapGeneratorTest.TestClasses"" TargetNamespaceFormat=""Wrapped.System.IO"" />
</Generate>"
        });
        gen.Execute(context);
        string code = context.SourcesAdded[$"Wrapped/System/IO/{className}Wrap.cs"];
        Console.WriteLine(code);
        if (expected != null)
        {
            CodeAsserts.AreEqual(code, expected);
        }
    }
}

//TODO: move this to a common place
internal class CodeAsserts
{
    public static void AreEqual(string actual, string expected)
    {
        Assert.AreEqual(CleanString(expected), CleanString(actual));
    }

    private static string CleanString(string v)
    {
        StringBuilder b = new();
        foreach (char c in v)
        {
            if (c == '\r') continue;
            else if (c == '\t') b.Append("    ");
            else b.Append(c);
        }

        return b.ToString().Trim('\n', '\r', '\t', ' ');
    }
}
