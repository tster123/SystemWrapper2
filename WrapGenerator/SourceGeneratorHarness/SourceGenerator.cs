using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Xml.Linq;
using System.Xml.XPath;
using WrapGenerator;

namespace SourceGeneratorHarness;

public interface ISourceGeneratorContext
{
    public CancellationToken CancellationToken { get; }
    public IEnumerable<AdditionalText> AdditionalFiles { get; }
    public void AddSource(string hintPath, string source);
}

public class SourceGeneratorContext : ISourceGeneratorContext
{
    private GeneratorExecutionContext inner;

    public IEnumerable<AdditionalText> AdditionalFiles => inner.AdditionalFiles;
    public CancellationToken CancellationToken => inner.CancellationToken;

    public SourceGeneratorContext(GeneratorExecutionContext context)
    {
        inner = context;
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
        Execute(new SourceGeneratorContext(context));
    }

    public void Execute(ISourceGeneratorContext context)
    {
        GenRegistrar registrar = new();
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
    }
}