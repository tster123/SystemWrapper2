using System;
using System.Linq;

namespace WrapGenerator;

public record ClassToWrap
{
    public readonly Type Type;
    public readonly string TargetNamespace;
    public readonly string ClassNameToGenerate;
    public readonly string InterfaceNameToGenerate;
    public readonly string[]? ClassLevelGenericParameters;
    public bool IsStatic => Type.IsSealed && Type.IsAbstract;
    public ClassToWrap(Type type,
        WrapNamespace wrapSettings)
    {
        Type = type;
        Type[] generics = type.GetGenericArguments();
        string name = type.Name.Split('`').First();
        if (generics.Length == 0)
        {
            ClassLevelGenericParameters = null;
        }
        else
        {
            ClassLevelGenericParameters = generics.Select(a => a.Name).ToArray();
            name += "_" + string.Join("_", ClassLevelGenericParameters) + "_";
        }

        string wrapperClassname = string.Format(wrapSettings.WrapperClassNameFormat, name);
        TargetNamespace = string.Format(wrapSettings.TargetNamespaceFormat, type.Namespace);
        ClassNameToGenerate = wrapperClassname;
        InterfaceNameToGenerate = "I" + wrapperClassname;
        
    }

    public override string ToString()
    {
        return
            $"{nameof(Type)}: {Type}, {nameof(TargetNamespace)}: {TargetNamespace}, {nameof(ClassNameToGenerate)}: {ClassNameToGenerate}, {nameof(InterfaceNameToGenerate)}: {InterfaceNameToGenerate}";
    }
}