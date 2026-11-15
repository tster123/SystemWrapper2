using System.Threading;

namespace WrapGenerator.CodeGeneration;

public interface ISourceGeneratorContext
{
    public CancellationToken CancellationToken { get; }
    public void AddSource(string hintPath, string source);
}