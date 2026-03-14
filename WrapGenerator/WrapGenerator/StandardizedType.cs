using System;
using System.Collections.Generic;
using System.Linq;

namespace WrapGenerator;

internal record StandardizedType
{
    public StandardizedType(
        Type original,
        string? namespaceName,
        string name,
        string? interfaceName,
        StandardizedType[]? parameterizedTypes,
        bool isWrapped = false,
        bool isArray = false)
    {
        Original           = original;
        Namespace          = namespaceName;
        Name               = name;
        Interface          = interfaceName;
        ParameterizedTypes = parameterizedTypes;
        IsWrapped          = isWrapped;
        IsArray            = isArray;
    }

    public Type Original { get; }
    public string? Namespace { get; }
    public string Name { get; }
    public string? Interface { get; }
    public StandardizedType[]? ParameterizedTypes { get; }
    public bool IsWrapped { get; }
    public bool IsArray { get; }

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

    public static Dictionary<Type, StandardizedType> Common = new()
    {
        [typeof(void)]   = new StandardizedType(typeof(void), null, "void", null, null),
        [typeof(int)]    = new StandardizedType(typeof(int), null, "int", null, null),
        [typeof(long)]   = new StandardizedType(typeof(long), null, "long", null, null),
        [typeof(short)]  = new StandardizedType(typeof(short), null, "short", null, null),
        [typeof(double)] = new StandardizedType(typeof(double), null, "double", null, null),
        [typeof(float)]  = new StandardizedType(typeof(float), null, "float", null, null),
        [typeof(string)] = new StandardizedType(typeof(string), null, "string", null, null),
        [typeof(byte)]   = new StandardizedType(typeof(byte), null, "byte", null, null),
        [typeof(bool)]   = new StandardizedType(typeof(bool), null, "bool", null, null),
        [typeof(ushort)] = new StandardizedType(typeof(ushort), null, "ushort", null, null),
        [typeof(uint)]   = new StandardizedType(typeof(uint), null, "uint", null, null),
        [typeof(ulong)]  = new StandardizedType(typeof(ulong), null, "ulong", null, null),
    };

    public void Deconstruct(out Type Original, out string? Namespace, out string Name, out string? Interface, out StandardizedType[]? ParameterizedTypes)
    {
        Original           = this.Original;
        Namespace          = this.Namespace;
        Name               = this.Name;
        Interface          = this.Interface;
        ParameterizedTypes = this.ParameterizedTypes;
    }
}