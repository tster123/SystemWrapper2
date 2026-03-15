using System;
using System.Linq;

namespace WrapGenerator;

internal record ClassToWrap
{
    internal readonly Type Type;
    internal readonly string TargetNamespace;
    internal readonly string ClassNameToGenerate;
    internal readonly string InterfaceNameToGenerate;
    internal readonly string[]? ClassLevelGenericParameters;
    internal bool IsStatic => Type.IsSealed && Type.IsAbstract;
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