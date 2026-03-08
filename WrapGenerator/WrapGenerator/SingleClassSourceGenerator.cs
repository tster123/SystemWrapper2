using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace WrapGenerator;

internal class SingleClassSourceGenerator
{
    private readonly ISourceGeneratorContext context;
    private readonly GenRegistrar registrar;
    private int _test;
    private ClassToWrap wrap;
    private readonly HashSet<string> usings = new();
    private readonly StringBuilder interStr = new();
    private readonly StringBuilder classStr = new();

    private int Test
    {
        get => _test;
        set => _test = value;
    }
    public SingleClassSourceGenerator(ISourceGeneratorContext context, GenRegistrar registrar, ClassToWrap wrap)
    {
        this.context = context;
        this.registrar = registrar;
        this.wrap = wrap;
    }

    public string GeneratorSource()
    {
        usings.Add(wrap.Type.Namespace);
        string topLevelGenerics = wrap.ClassLevelGenericParameters == null ? "" : $"<{string.Join(", ", wrap.ClassLevelGenericParameters)}>";
        interStr.Append($"public interface {wrap.InterfaceNameToGenerate}{topLevelGenerics}");
        Type[] implementedInterfaces = wrap.Type.GetInterfaces();
        if (implementedInterfaces.Length != 0)
        {
            interStr.Append(" : " + string.Join(", ", implementedInterfaces.Select(i => i.Name))); // TODO: handle generic interfaces
            foreach (Type i in implementedInterfaces)
            {
                if (i.Namespace != null) usings.Add(i.Namespace);
            }
        }

        interStr.AppendLine(" {");
        // TODO: have it implement interfaces that the source class implemented

        StandardizedType innerType = GetStandardizedType(wrap.Type);

        classStr.Append(@$"public class {wrap.ClassNameToGenerate}{topLevelGenerics} : {wrap.InterfaceNameToGenerate}{topLevelGenerics} {{
    private readonly {innerType.UseType(true)} inner;
    public {wrap.ClassNameToGenerate}({innerType.UseType(true)} inner) {{
        this.inner = inner;
    }}
");
        // TODO: add constructors that match the original type's constructors and build inner.
        GenerateProperties();
        GenerateMethods();
        StringBuilder s = new();
        foreach (string u in usings)
        {
            s.AppendLine($"using {u};");
        }

        s.AppendLine($"namespace {wrap.TargetNamespace} {{");

        interStr.AppendLine("}");
        classStr.AppendLine("}");
        s.AppendLine(interStr.ToString());
        s.AppendLine(classStr.ToString());
        s.AppendLine("}"); // close namespace
        return s.ToString();
    }

    private void GenerateMethods()
    {
        MethodInfo[] methods = wrap.Type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
        var props = wrap.Type.GetProperties();
        foreach (MethodInfo method in methods)
        {
            // skip methods that are part of properties
            if (props.Any(p => p.GetMethod == method | p.SetMethod == method)) continue;

            StandardizedType retType = GetStandardizedType(method.ReturnType);
            AddUsing(retType);
            string methodGenericParamString = GenerateMethodGenericParamString(method);
            interStr.Append($"    public {retType.UseType()} {method.Name}{methodGenericParamString}(");
            classStr.Append($"    public {retType.UseType()} {method.Name}{methodGenericParamString}(");
            bool first = true;
            StringBuilder argList = new();
            foreach (ParameterInfo param in method.GetParameters())
            {
                StandardizedType paramType = GetStandardizedType(param.ParameterType);
                AddUsing(paramType);
                if (!first)
                {
                    interStr.Append(", ");
                    classStr.Append(", ");
                    argList.Append(", ");
                }
                first = false;
                interStr.Append($"{paramType.UseType()} {param.Name}");
                classStr.Append($"{paramType.UseType()} {param.Name}"); // TODO: support default values
                argList.Append(param.Name);
            }

            interStr.AppendLine(");");
            classStr.AppendLine(") {");
            string retStr = method.ReturnType == typeof(void) ? "" : "return ";
            classStr.AppendLine($"        {retStr}inner.{method.Name}({argList});");
            classStr.AppendLine("    }");
        }
    }

    private string GenerateMethodGenericParamString(MethodInfo method)
    {
        Type[]? genericArgs = method.GetGenericArguments();
        if (genericArgs == null || genericArgs.Length == 0) return "";
        return $"<{string.Join(", ", genericArgs.Select(a => a.Name))}>";
    }

    private void GenerateProperties()
    {
        PropertyInfo[] props = wrap.Type.GetProperties(BindingFlags.Instance | BindingFlags.Public);
        foreach (PropertyInfo property in props)
        {
            StandardizedType propType = GetStandardizedType(property.PropertyType);
            AddUsing(propType);

            interStr.Append($"    public {propType.UseType()} {property.Name} {{ get; ");
            classStr.Append($"    public {propType.UseType()} {property.Name} {{ get => inner.{property.Name}; ");

            if (property.SetMethod != null && property.SetMethod.IsPublic)
            {
                interStr.Append("set; ");
                classStr.Append($"set => inner.{property.Name} = value; ");
            }

            interStr.AppendLine("}");
            classStr.AppendLine("}");
        }
    }

    private void AddUsing(StandardizedType type)
    {
        if (type.Namespace != null && type.Namespace != wrap.TargetNamespace) usings.Add(type.Namespace);
    }

    private StandardizedType GetStandardizedType(Type type)
    {
        if (StandardizedType.Common.TryGetValue(type, out StandardizedType r)) return r;
        ClassToWrap? wrap = registrar.GetWrap(type);

        StandardizedType[]? parameterized;
        string cName;
        string? iName;
        string ns = wrap == null ? type.Namespace : wrap.TargetNamespace;
        Type[]? generics = type.GenericTypeArguments;
        if (generics == null || generics.Length == 0 && type.IsGenericType)
        {
            var t1 = type.GetGenericTypeDefinition();
            generics = t1.GetGenericArguments();
        }
        if (generics == null || generics.Length == 0)
        {
            parameterized = null;
            cName = wrap == null ? type.Name : wrap.ClassNameToGenerate;
            iName = wrap == null ? null : wrap.InterfaceNameToGenerate;
        }
        else
        {
            parameterized = generics.Select(GetStandardizedType).ToArray();
            cName = (wrap == null ? type.Name : wrap.ClassNameToGenerate).Split('`').First();
            iName = wrap == null ? null : wrap.InterfaceNameToGenerate.Split('`').First();
        }
        return new StandardizedType(type, ns, cName, iName, parameterized);
    }
}

internal record StandardizedType(Type Original, string? Namespace, string Name, string? Interface, StandardizedType[]? ParameterizedTypes)
{
    public Type Original { get; } = Original;
    public string? Namespace { get; } = Namespace;
    public string Name { get; } = Name;
    public string? Interface { get; } = Interface;
    public StandardizedType[]? ParameterizedTypes { get; } = ParameterizedTypes;

    public string UseType(bool useInner = false)
    {
        string name = useInner ? Original.Name.Split('`').First() : Interface ?? Name;
        if (ParameterizedTypes != null)
        {
            name += "<";
            name += string.Join(", ", ParameterizedTypes.Select(pt => pt.UseType(useInner)));
            name += ">";
        }

        return name;
    }

    public static Dictionary<Type, StandardizedType> Common = new()
    {
        [typeof(void)] = new StandardizedType(typeof(void), null, "void", null, null),
        [typeof(int)] = new StandardizedType(typeof(int), null, "int", null, null),
        [typeof(long)] = new StandardizedType(typeof(long), null, "long", null, null),
        [typeof(short)] = new StandardizedType(typeof(short), null, "short", null, null),
        [typeof(double)] = new StandardizedType(typeof(double), null, "double", null, null),
        [typeof(float)] = new StandardizedType(typeof(float), null, "float", null, null),
        [typeof(string)] = new StandardizedType(typeof(string), null, "string", null, null),
        [typeof(byte)] = new StandardizedType(typeof(byte), null, "byte", null, null),
        [typeof(bool)] = new StandardizedType(typeof(bool), null, "bool", null, null),
    };
}

