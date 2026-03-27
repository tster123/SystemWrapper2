using WrapGenerator;

namespace WrapCli;

public class GenerationContext : ISourceGeneratorContext
{
    public CancellationToken CancellationToken { get; } = new CancellationToken(false);

    private string BasePath { get; }

    public GenerationContext(string basePath)
    {
        BasePath = basePath;
    }

    public void AddSource(string hintPath, string source)
    {
        string path = Path.Combine(BasePath, hintPath);
        string dir = Path.GetDirectoryName(path);
        Directory.CreateDirectory(dir);
        File.WriteAllText(path, source);
    }
}