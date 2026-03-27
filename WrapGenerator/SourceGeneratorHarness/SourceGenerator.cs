using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Xml.Linq;
using System.Xml.XPath;
using WrapGenerator;

namespace SourceGeneratorHarness;

public class SourceGeneratorContext : ISourceGeneratorContext
{
    private readonly GeneratorExecutionContext inner;

    public IEnumerable<AdditionalText> AdditionalFiles => inner.AdditionalFiles;
    public CancellationToken CancellationToken => inner.CancellationToken;
    public MetadataLoadContext Domain { get; }

    public SourceGeneratorContext(GeneratorExecutionContext context, MetadataLoadContext domain)
    {
        inner = context;
        Domain = domain;
    }

    public void AddSource(string hintName, string source) => inner.AddSource(hintName, source);
}

#pragma warning disable RS1042, RS1036 // Implementations of this interface are not allowed
[Generator]
public class SourceGenerator : ISourceGenerator
#pragma warning restore RS1042, RS1036 // Implementations of this interface are not allowed
{
    public void Initialize(GeneratorInitializationContext context)
    {
    }

    public void Execute(GeneratorExecutionContext context)
    {
        string s = "";
        string[] allAssemblies = context.Compilation.References.Where(a => a != null).Select(a => a.Display!).ToArray();
        var resolver = new PathAssemblyResolver(allAssemblies);
        using (var loadContext = new MetadataLoadContext(resolver))
        {
            foreach (var assemblyFile in allAssemblies)
            {
                loadContext.LoadFromAssemblyPath(assemblyFile);
            }
            //Execute(new SourceGeneratorContext(context, loadContext));
        }

        /*foreach (MetadataReference a in context.Compilation.References)
        {
            Assembly.ReflectionOnlyLoadFrom(a.Display!);
            s += a.Display + "\n";
        }*/
        context.AddSource("CompilationInfo.cs", $"/* {s} */");
    }

    public void Execute(ISourceGeneratorContext context)
    {
        /*
        GenRegistrar registrar = new(context);
        foreach (AdditionalText file in context.AdditionalFiles) 
        {
            SourceText? text = file.GetText(context.CancellationToken);
            if (text == null) continue;
            XDocument xml = XDocument.Parse(text.ToString());
            XElement? gen = xml.XPathSelectElement("/Generate");
            if (gen == null) continue;
            foreach (XElement nsElem in gen.Descendants("Namespace"))
            {
                string ns = nsElem.Attribute("Namespace").Value;
                string targetNsFormat = nsElem.Attribute("TargetNamespaceFormat").Value;
                string? excludeFilter = nsElem.Attribute("Exclude")?.Value;
                string? includeFilter = nsElem.Attribute("IncludeOnly")?.Value;
                registrar.Register(new WrapNamespace
                {
                    Namespace = ns,
                    TargetNamespaceFormat = targetNsFormat,
                    ExcludeFilter = excludeFilter,
                    OnlyIncludeFilter = includeFilter,
                });
            }
        }

        TypeFactory factory = new(registrar);
        foreach (ClassToWrap wrap in registrar.AllClassesToWrap)
        {
            SingleClassSourceGenerator generator = new(factory, wrap);
            string code = generator.GeneratorSource();
            context.AddSource(wrap.TargetNamespace.Replace('.', '/') + "/" + wrap.ClassNameToGenerate + ".cs", code);
        }
        */
        
    }
}