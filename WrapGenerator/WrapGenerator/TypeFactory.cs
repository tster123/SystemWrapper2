using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace WrapGenerator;

public class TypeFactory
{
    private readonly GenRegistrar registrar;

    public TypeFactory(GenRegistrar registrar)
    {
        this.registrar = registrar;
    }

    private readonly Dictionary<Type, StandardizedType> types = new();

    internal StandardizedType GetStandardizedType(Type type)
    {
        if (Common.TryGetValue(type, out StandardizedType r)) return r;
        if (types.TryGetValue(type, out StandardizedType r2)) return r2;

        Type eType = type;
        if (type.IsArray)
        {
            Type? e = type.GetElementType();
            Debug.Assert(e != null);
            eType = e!;
        }
        ClassToWrap? eWrap = registrar.GetWrap(eType);

        StandardizedType[]? parameterized;
        string cName;
        string? iName;
        string? ns = type.Namespace;
        if (eWrap != null) ns = eWrap.TargetNamespace;

        Type[] generics = type.GenericTypeArguments;
        if (generics.Length == 0 && type.IsGenericType)
        {
            var t1 = type.GetGenericTypeDefinition();
            generics = t1.GetGenericArguments();
        }
        if (generics.Length == 0)
        {
            parameterized = null;
            cName         = eWrap == null ? type.Name : eWrap.ClassNameToGenerate;
            iName         = eWrap?.InterfaceNameToGenerate;
        }
        else
        {
            parameterized = generics.Select(GetStandardizedType).ToArray();
            cName         = (eWrap == null ? type.Name : eWrap.ClassNameToGenerate).Split('`').First();
            iName         = eWrap?.InterfaceNameToGenerate.Split('`').First();
        }

        StandardizedType ret = new(type, ns, cName, iName, parameterized, this, eWrap != null, type.IsArray);
        types[type] = ret;
        return ret;
    }

    private static readonly Dictionary<Type, StandardizedType> Common = new()
    {
        [typeof(void)]   = MakeCommon(typeof(void), "void"),
        [typeof(int)]    = MakeCommon(typeof(int), "int"),
        [typeof(long)]   = MakeCommon(typeof(long), "long"),
        [typeof(short)]  = MakeCommon(typeof(short), "short"),
        [typeof(double)] = MakeCommon(typeof(double), "double"),
        [typeof(float)]  = MakeCommon(typeof(float), "float"),
        [typeof(string)] = MakeCommon(typeof(string), "string"),
        [typeof(byte)]   = MakeCommon(typeof(byte), "byte"),
        [typeof(bool)]   = MakeCommon(typeof(bool), "bool"),
        [typeof(ushort)] = MakeCommon(typeof(ushort), "ushort"),
        [typeof(uint)]   = MakeCommon(typeof(uint), "uint"),
        [typeof(ulong)]  = MakeCommon(typeof(ulong), "ulong"),
    };

    private static StandardizedType MakeCommon(Type type, string shortName)
        => new (type, null, shortName, null, null, null);
}
