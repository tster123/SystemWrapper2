using System.Threading;

namespace WrapGenerator;

public interface ISourceGeneratorContext
{
    public CancellationToken CancellationToken { get; }
    public void AddSource(string hintPath, string source);
}