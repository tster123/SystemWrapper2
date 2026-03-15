using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Text;
using Microsoft.Win32;
using WrapGenerator;

namespace WrapGenratorTest;

public class TestAdditionalText(string path, string text) : AdditionalText
{
    public override string Path { get; } = path;
    public override SourceText? GetText(CancellationToken cancellationToken = new CancellationToken())
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
public sealed class Test1
{
    private static WrapNamespace wrapNs = new()
    {
        Namespace = "System.IO",
        TargetNamespaceFormat = "Wrapped.TestClasses"
    };

    [TestMethod]
    public void TestMethod1()
    {
        FileSystemWatcher w = new FileSystemWatcher();
        SourceGenerator gen = new();
        TestSourceGeneratorContext context = new TestSourceGeneratorContext(new()
        {
            ["Namespaces.xml"] = @"<?xml version=""1.0"" encoding=""utf-8"" ?>
<Generate>
	<Namespace Namespace=""System.IO"" TargetNamespaceFormat=""Wrapped.System.IO"" />
</Generate>"
        });
        gen.Execute(context);
        Console.WriteLine(context.SourcesAdded["Wrapped/System/IO/FileSystemWatcherWrap.cs"]);
        Assert.AreEqual("foo", context.SourcesAdded["Wrapped/System/IO/FileSystemWatcherWrap.cs"]);
    }

    private void DoTestWithMultiple(string className)
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
        Console.WriteLine(context.SourcesAdded[$"Wrapped/System/IO/{className}Wrap.cs"]);
    }

    private void DoTest(string className)
    {
        GenRegistrar registrar = new();
        ClassToWrap wrap = new(Type.GetType("WrapGeneratorTest.TestClasses." + className)!, wrapNs);
        registrar.Register(wrap);
        SingleClassSourceGenerator generator = new(registrar, wrap);
        string code = generator.GeneratorSource();
        Console.WriteLine(code);
    }

    [TestMethod]
    public void PropertyTest()
    {
        DoTest("PropertyExample");
    }

    [TestMethod]
    public void ListTest()
    {
        DoTest("ListExample");
    }

    [TestMethod]
    public void ArrayTest()
    {
        DoTest("ArrayExample");
    }

    [TestMethod]
    public void NullableTest()
    {
        DoTest("NullableExample");
    }

    [TestMethod]
    public void GenericTest()
    {
        DoTestWithMultiple("GenericExample_T1_");
    }

    [TestMethod]
    public void InterfaceTest()
    {
        DoTest("InterfaceExample");
    }

    [TestMethod]
    public void DisposableTest()
    {
        DoTest("DisposableExample");
    }

    [TestMethod]
    public void WrappingTest()
    {
        DoTest("WrappingExample");
    }

    [TestMethod]
    public void ArraySegmentTest()
    {
        DoTest("ArraySegmentExample");
    }

    [TestMethod]
    public void OutTest()
    {
        DoTest("OutExample");
    }

    [TestMethod]
    public void CtorTest()
    {
        DoTest("CtorExample");
    }

    [TestMethod]
    public void AttributesTest()
    {
        DoTest("AttributesExample");
    }

    [TestMethod]
    public void StreamTest()
    {
        GenRegistrar registrar = new();
        ClassToWrap wrap = new(typeof(FileStream), wrapNs);
        registrar.Register(wrap);
        registrar.Register(new ClassToWrap(typeof(Stream), wrapNs));
        SingleClassSourceGenerator generator = new(registrar, wrap);
        string code = generator.GeneratorSource();
        Console.WriteLine(code);
    }

    [TestMethod]
    public void FileSystemWatcherTest()
    {
        GenRegistrar registrar = new();
        ClassToWrap wrap = new(typeof(FileSystemWatcher), wrapNs);
        registrar.Register(wrap);
        SingleClassSourceGenerator generator = new(registrar, wrap);
        string code = generator.GeneratorSource();
        Console.WriteLine(code);
    }
}