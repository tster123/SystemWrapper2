using System;
using System.Linq;

namespace WrapGenerator;

public record StandardizedType
{
    public StandardizedType(
        Type original,
        string? namespaceName,
        string name,
        string? interfaceName,
        StandardizedType[]? parameterizedTypes,
        ITypeFactory? factory,
        bool isWrapped = false,
        bool isArray = false)
    {
        Original = original;
        Namespace = namespaceName;
        Name = name;
        Interface = interfaceName;
        ParameterizedTypes = parameterizedTypes;
        Factory = factory ?? new DummyTypeFactory();
        IsWrapped = isWrapped;
        IsArray = isArray;
    }

    public Type Original { get; }
    public string? Namespace { get; }
    public string Name { get; }
    public string? Interface { get; }
    public StandardizedType[]? ParameterizedTypes { get; }
    public bool IsWrapped { get; }
    public bool IsArray { get; }
    public ITypeFactory Factory { get; }

    public string UseType(bool useInner = false, bool forceClass = false)
    {
        string? iface = forceClass ? null : Interface;
        string name = useInner ? Original.Name.Split('`').First() : iface ?? Name;
        if (ParameterizedTypes != null)
        {
            name += "<";
            name += string.Join(", ", ParameterizedTypes.Select(pt => pt.UseType(useInner)));
            name += ">";
        }

        if (IsArray && IsWrapped)
        {
            name += "[]";
        }
        return name;
    }

    //public string WrapMethodCode()

    //private bool needsWrapMethod = false;

    public string WrapRValueCode(string rValue)
    {
        if (Original == typeof(void)) return rValue;
        if (IsWrapped)
        {
            if (IsArray)
            {
                StandardizedType elemType = Factory.GetStandardizedType(Original.GetElementType()!);
                return $"WrapHelpers.WrapArray({rValue}, e => new {elemType.UseType(forceClass: true)}(e))";
            }
            return $"{rValue} is {Original.Name} _r ? new {UseType(forceClass: true)}(_r) : null";
        }

        Type[] paramTypes = Original.GetGenericArguments();
        if (Name.StartsWith("IEnumerable"))
        {
            StandardizedType iEnumType = Factory!.GetStandardizedType(paramTypes[0]);
            if (iEnumType.IsWrapped)
            {
                return $"WrapHelpers.WrapEnumerable({rValue}, e => new {iEnumType.UseType(forceClass: true)}(e))";
            }
        }

        return rValue;
    }
}