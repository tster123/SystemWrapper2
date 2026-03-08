namespace WrapGenerator;

internal class WrapNamespace
{
    public string TargetNamespaceFormat { get; set; } = "{0}";
    public string WrapperClassNameFormat { get; set; } = "{0}Wrap";
    public string? Namespace { get; set; }

    public override string ToString()
    {
        return $"{nameof(TargetNamespaceFormat)}: {TargetNamespaceFormat}, {nameof(WrapperClassNameFormat)}: {WrapperClassNameFormat}, {nameof(Namespace)}: {Namespace}";
    }
}