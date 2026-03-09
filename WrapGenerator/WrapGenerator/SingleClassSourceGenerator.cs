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
    private readonly StringBuilder interfaceStr = new();
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
        interfaceStr.Append($"public interface {wrap.InterfaceNameToGenerate}{topLevelGenerics}");
        Type[] implementedInterfaces = wrap.Type.GetInterfaces();
        if (implementedInterfaces.Length != 0)
        {
            interfaceStr.Append(" : " + string.Join(", ", implementedInterfaces.Select(i => i.Name))); // TODO: handle generic interfaces
            foreach (Type i in implementedInterfaces)
            {
                if (i.Namespace != null) usings.Add(i.Namespace);
            }
        }

        interfaceStr.AppendLine(" {");

        StandardizedType innerType = GetStandardizedType(wrap.Type);

        interfaceStr.AppendLine($"\t{innerType.UseType(true)} WrappedInstance {{ get; }}");

        classStr.Append(@$"public class {wrap.ClassNameToGenerate}{topLevelGenerics} : {wrap.InterfaceNameToGenerate}{topLevelGenerics} {{
    private readonly {innerType.UseType(true)} inner;
    public {innerType.UseType(true)} WrappedInstance => inner;
    public {wrap.ClassNameToGenerate}({innerType.UseType(true)} inner) {{
        this.inner = inner;
    }}
");
        // TODO: add constructors that match the original type's constructors and build inner.
        GenerateProperties();
        GenerateMethods(implementedInterfaces);
        StringBuilder s = new();
        foreach (string u in usings)
        {
            s.AppendLine($"using {u};");
        }

        s.AppendLine($"namespace {wrap.TargetNamespace} {{");

        interfaceStr.AppendLine("}");
        classStr.AppendLine("}");
        s.AppendLine(interfaceStr.ToString());
        s.AppendLine(classStr.ToString());
        s.AppendLine("}"); // close namespace
        return s.ToString();
    }

    private static List<MethodInfo> methodsToSkip =
    [
        typeof(object).GetMethod("GetType")
    ];

    private void GenerateMethods(Type[] implementedInterfaces)
    {
        MethodInfo[] methods = wrap.Type.GetMethods(BindingFlags.Instance | BindingFlags.Public);
        var props = wrap.Type.GetProperties();

        foreach (MethodInfo method in methods)
        {
            if (methodsToSkip.Any(m => m.DeclaringType == method.DeclaringType && m.Name == method.Name)) 
                continue;

            // use a dummy stringbuilder if the declaring type is not this type so we don't redeclare methods
            // on the interface
            MethodInfo baseMethod = method.GetBaseDefinition();
            StringBuilder interStr = interfaceStr;
            if (method.DeclaringType != wrap.Type ||
                baseMethod != null && baseMethod.DeclaringType != method.DeclaringType ||
                IsFromTypes(method, implementedInterfaces))
            {
                interStr = new StringBuilder();
            }

            // skip methods that are part of properties
            if (props.Any(p => p.GetMethod == method | p.SetMethod == method)) continue;

            StandardizedType retType = GetStandardizedType(method.ReturnType);
            AddUsing(retType);
            string overrideStr = IsFromTypes(method, [typeof(object)]) ? "override " : "";
            string methodGenericParamString = GenerateMethodGenericParamString(method);
            string retTypeStr = retType.UseType();
            interStr.Append($"    public {retTypeStr} {method.Name}{methodGenericParamString}(");
            classStr.Append($"    public {overrideStr}{retTypeStr} {method.Name}{methodGenericParamString}(");
            bool first = true;
            StringBuilder argList = new();
            foreach (ParameterInfo param in method.GetParameters())
            {
                Type paramT = param.ParameterType;
                if (paramT.IsByRef)
                {
                    paramT = paramT.GetElementType()!;
                }
                StandardizedType paramType = GetStandardizedType(paramT);
                AddUsing(paramType);
                if (!first)
                {
                    interStr.Append(", ");
                    classStr.Append(", ");
                    argList.Append(", ");
                }
                first = false;
                string paramTypeStr = paramType.UseType();
                if (param.IsOut)
                {
                    interStr.Append("out ");
                    classStr.Append("out ");
                    paramTypeStr = paramTypeStr.Replace("&", "");
                }
                interStr.Append($"{paramTypeStr} {param.Name}");
                classStr.Append($"{paramTypeStr} {param.Name}"); // TODO: support default values
                if (paramType.IsWrapped)
                {
                    argList.Append(param.Name + ".WrappedInstance");
                }
                else
                {
                    argList.Append(param.Name);
                }
            }

            interStr.AppendLine(");");
            classStr.AppendLine(") {");
            string retFormat = "{0}";
            if (method.ReturnType != typeof(void))
            {
                retFormat = "return {0}";
                if (retType.IsWrapped)
                {
                    retFormat = $"return new {retType.UseType(forceClass: true)}({{0}})";
                }
            }
            string formattedCall = string.Format(retFormat, $"inner.{method.Name}({argList})");
            classStr.AppendLine($"\t\t{formattedCall};");
            classStr.AppendLine("\t}");
        }
    }

    private static bool IsFromTypes(MethodInfo method, Type[] types)
    {
        foreach (Type type in types)
        {
            foreach (MethodInfo m in type.GetMethods())
            {
                if (m.Name == method.Name)
                {
                    ParameterInfo[] p1 = method.GetParameters();
                    ParameterInfo[] p2 = m.GetParameters();
                    if (p1.Length != p2.Length) continue;
                    bool same = true;
                    for (int i = 0; i < p1.Length; i++)
                    {
                        if (p1[i].ParameterType != p2[i].ParameterType)
                        {
                            same = false;
                            break;
                        }
                    }

                    if (same) return true;
                }
            }
        }

        return false;
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
            bool needsWrap = propType.IsWrapped;
            AddUsing(propType);

            string typeStr = propType.UseType();

            interfaceStr.Append($"\tpublic {typeStr} {property.Name} {{ ");
            classStr.AppendLine($"\tpublic {typeStr} {property.Name} {{ ");

            if (property.GetMethod != null && property.GetMethod.IsPublic)
            {
                interfaceStr.Append("get; ");
                string wrapFormat = "{0}";
                if (needsWrap)
                {
                    wrapFormat = $"new {propType.UseType(forceClass: true)}({{0}})";
                }

                string rValue = string.Format(wrapFormat, $"inner.{property.Name}");
                classStr.AppendLine($"\t\tget => {rValue}; ");
            }

            if (property.SetMethod != null && property.SetMethod.IsPublic)
            {
                string rValue = needsWrap ? "value.WrappedInstance" : "value";
                interfaceStr.Append("set; ");
                classStr.AppendLine($"\t\tset => inner.{property.Name} = {rValue}; ");
            }

            interfaceStr.AppendLine("}");
            classStr.AppendLine("\t}");
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

        return new StandardizedType(type, ns, cName, iName, parameterized, wrap != null);
    }
}

internal record StandardizedType
{
    public StandardizedType(
        Type original,
        string? namespaceName,
        string name,
        string? interfaceName,
        StandardizedType[]? parameterizedTypes,
        bool isWrapped = false)
    {
        Original           = original;
        Namespace          = namespaceName;
        Name               = name;
        Interface          = interfaceName;
        ParameterizedTypes = parameterizedTypes;
        IsWrapped          = isWrapped;
    }

    public Type Original { get; }
    public string? Namespace { get; }
    public string Name { get; }
    public string? Interface { get; }
    public StandardizedType[]? ParameterizedTypes { get; }
    public bool IsWrapped { get; }

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

