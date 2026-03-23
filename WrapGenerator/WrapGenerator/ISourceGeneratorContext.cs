
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;

namespace WrapGenerator;

public interface ISourceGeneratorContext
{
    public CancellationToken CancellationToken { get; }
    public IEnumerable<AdditionalText> AdditionalFiles { get; }
    public void AddSource(string hintPath, string source);

    public MetadataLoadContext Domain { get; }
}