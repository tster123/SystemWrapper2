using WrapGenerator;

namespace WrapCli;

public class Program
{
    static Program()
    {
#pragma warning disable CS0168 // Variable is declared but never used
        DriveInfo i;
#pragma warning restore CS0168 // Variable is declared but never used
    }

    private readonly GenerationContext context;
    private readonly List<WrapNamespace> toWrap;
    public Program(GenerationContext context, List<WrapNamespace> toWrap)
    {
        this.context = context;
        this.toWrap = toWrap;
    }

    public static void Main(string[] args)
    {
        string outDir = args[0];
        List<WrapNamespace> toWrap =
        [
            new()
            {
                Namespace = "System.IO",
                TargetNamespaceFormat = "Wrapped.System.IO",
                ExcludeFilter = "Unmanaged.*|BufferedStream|.*Options.*"
            },
            new()
            {
                Namespace = "System.Diagnostics",
                TargetNamespaceFormat = "Wrapped.System.Diagnostics",
                OnlyIncludeFilter = "^((Process)|(ProcessStartInfo))$"
            }
        ];
        Program p = new(new GenerationContext(outDir), toWrap);
        p.GenerateCode();
    }

    public void GenerateCode()
    {
        GenRegistrar registrar = new();
        foreach (WrapNamespace ns in toWrap)
        {
            registrar.Register(ns);
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