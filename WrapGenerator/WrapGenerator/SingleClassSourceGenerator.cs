using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace WrapGenerator;

//TODO - provide option to wrap static classes like Path and File.
internal class SingleClassSourceGenerator
{
    private readonly GenRegistrar registrar;
    private readonly ClassToWrap wrap;
    private readonly HashSet<string> usings = new();
    private readonly StringBuilder interfaceStr = new();
    private readonly StringBuilder classStr = new();

    public SingleClassSourceGenerator(GenRegistrar registrar, ClassToWrap wrap)
    {
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
");

        GenerateConstructors(innerType);
        classStr.AppendLine();
        GenerateProperties();
        classStr.AppendLine();
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

    private void GenerateConstructors(StandardizedType innerType)
    {
        // add the wrap constructor.  all other constructors will call this
        string innerTypeStr = innerType.UseType(true);
        classStr.AppendLine($@"	public {wrap.ClassNameToGenerate}({innerTypeStr} inner) {{
		this.inner = inner;
	}}");

        ConstructorInfo[] ctors = wrap.Type.GetConstructors(BindingFlags.Public | BindingFlags.Instance);
        foreach (ConstructorInfo ctor in ctors)
        {
            ParameterInfo[] parameters = ctor.GetParameters();
            classStr.Append($"\tpublic {wrap.ClassNameToGenerate}(");
            bool first = true;
            StringBuilder argList = new();
            foreach (ParameterInfo param in parameters)
            {
                (string declaration, string arg) = BuildParameterDeclarationAndArgumentRValue(param);
                if (!first)
                {
                    classStr.Append(", ");
                    argList.Append(", ");
                }
                first = false;
                classStr.Append(declaration);
                argList.Append(arg);
            }
            classStr.AppendLine(")");
            
            // call this(<wrapper class>)
            classStr.AppendLine($"\t\t: this(new {innerTypeStr}({argList})) {{ }}");
        }
    }

    private static readonly List<MethodInfo> methodsToSkip =
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

            // skip methods that are part of properties
            if (props.Any(p => p.GetMethod == method | p.SetMethod == method)) continue;

            // use a dummy StringBuilder if the declaring type is not this type so we don't redeclare methods
            // on the interface
            MethodInfo baseMethod = method.GetBaseDefinition();
            StringBuilder interStr = interfaceStr;
            if (method.DeclaringType != wrap.Type ||
                baseMethod != null && baseMethod.DeclaringType != method.DeclaringType ||
                IsFromTypes(method, implementedInterfaces))
            {
                interStr = new StringBuilder();
            }

            string? attrStr = BuildAttributes(method);
            if (attrStr != null)
            {
                interStr.Append(attrStr);
                classStr.Append(attrStr);
            }
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
                (string declaration, string arg) = BuildParameterDeclarationAndArgumentRValue(param);
                if (!first)
                {
                    interStr.Append(", ");
                    classStr.Append(", ");
                    argList.Append(", ");
                }
                first = false;
                interStr.Append(declaration);
                classStr.Append(declaration);
                argList.Append(arg);
            }

            interStr.AppendLine(");");
            classStr.AppendLine(") {");
            string retFormat = "{0}";
            if (method.ReturnType != typeof(void))
            {
                retFormat = "return {0}";
            }

            string rValue = ConvertRValueToWrapped($"inner.{method.Name}({argList})", retType);
            string formattedCall = string.Format(retFormat, rValue);
            classStr.AppendLine($"\t\t{formattedCall};");
            classStr.AppendLine("\t}");
        }
    }

    private string? BuildAttributes(MethodInfo method)
    {
        string GetCodeOfAttributeValue(object val, KeyValuePair<string, (PropertyInfo, object)> prop)
        {
            string strVal = val?.ToString() ?? "null";
            if (val != null)
            {
                if (prop.Value.Item1.PropertyType == typeof(string))
                {
                    strVal = "\"" + strVal + "\"";
                }
                else if (prop.Value.Item1.PropertyType == typeof(bool))
                {
                    strVal = strVal.ToLower();
                }
            }

            return strVal;
        }

        Attribute[] attrs = method.GetCustomAttributes().ToArray();
        if (attrs.Length == 0) return null;
        StringBuilder ret = new();
        foreach (Attribute a in attrs)
        {
            string name = a.GetType().Name;
            if (a.GetType().Namespace == "System.Runtime.CompilerServices") continue;
            AddUsing(GetStandardizedType(a.GetType()));
            if (name.EndsWith("Attribute")) name = name.Substring(0, name.Length - "Attribute".Length);
            Dictionary<string, (PropertyInfo, object)> attrProps = GetAttributePropertiesWithNonDefaultValues(a);

            if (attrProps.Count == 0)
            {
                ret.AppendLine($"[{name}]");
            }
            else
            {
                ConstructorInfo? ctor = GetValidConstructor(a);
                if (ctor == null)
                {
                    Console.Error.WriteLine($"WARNING: cannot find ctor for [{name}] on {wrap.Type.Name}.{method.Name}");
                    ret.AppendLine($"[{name}]");
                    continue;
                }
                ret.Append($"[{name}(");
                StringBuilder propSection = new();
                bool first = true;
                foreach (ParameterInfo param in ctor.GetParameters())
                {
                    if (!first) ret.Append(", ");
                    first = false;
                    foreach (KeyValuePair<string, (PropertyInfo, object)> prop in attrProps.ToArray())
                    {
                        if (prop.Value.Item1.PropertyType == param.ParameterType &&
                            prop.Key.Equals(param.Name, StringComparison.InvariantCultureIgnoreCase))
                        {
                            object val = prop.Value.Item2;
                            var strVal = GetCodeOfAttributeValue(val, prop);

                            ret.Append(param.Name).Append(": ").Append(strVal);
                            attrProps.Remove(prop.Key);
                        }
                    }
                }

                foreach (KeyValuePair<string, (PropertyInfo, object)> prop in attrProps.ToArray())
                {
                    if (!first) ret.Append(", ");
                    first = false;
                    ret.Append($"{prop.Key} = {GetCodeOfAttributeValue(prop.Value.Item2, prop)}");
                }
                ret.AppendLine(")]");
            }
            
        }

        return ret.ToString();
    }

    private ConstructorInfo? GetValidConstructor(Attribute a)
    {
        ConstructorInfo? toRet = null;
        int max = int.MinValue;
        foreach (ConstructorInfo ctor in a.GetType().GetConstructors(BindingFlags.Instance | BindingFlags.Public))
        {
            ParameterInfo[] ctorParams = ctor.GetParameters();
            if (ctorParams.Length > max)
            {
                max = ctorParams.Length;
                toRet = ctor;
            }
        }

        return toRet;
        /*foreach (ConstructorInfo ctor in a.GetType().GetConstructors(BindingFlags.Instance | BindingFlags.Public))
        {
            bool thisCtorWorks = true;
            ParameterInfo[] ctorParams = ctor.GetParameters();
            foreach (PropertyInfo prop in attrProps)
            {
                if (prop.DeclaringType == typeof(Attribute) || prop.DeclaringType == typeof(object)) continue;
                object? defaultValue = prop.PropertyType.IsValueType ? Activator.CreateInstance(prop.PropertyType) : null;
                object? value = prop.GetValue(a);
                if (Equals(defaultValue, value)) continue;  // value doesn't need to be given in ctor
                thisCtorWorks = false; // this won't work unless we find a valid parameter
                foreach (ParameterInfo ctorParam in ctorParams)
                {
                    if (ctorParam.ParameterType == prop.PropertyType &&
                        ctorParam.Name.Equals(prop.Name, StringComparison.InvariantCultureIgnoreCase))
                    {
                        thisCtorWorks = true; // found a valid one!
                        break;
                    }
                }

                if (!thisCtorWorks) break;
            }

            if (thisCtorWorks) return ctor;
        }

        return null;
        */
    }

    private Dictionary<string, (PropertyInfo, object)> GetAttributePropertiesWithNonDefaultValues(Attribute a)
    {
        Dictionary<string, (PropertyInfo, object)> ret = new();
        foreach (PropertyInfo p in a.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (p.DeclaringType == typeof(object) || p.DeclaringType == typeof(Attribute)) continue;
            object? defaultValue = p.PropertyType.IsValueType ? Activator.CreateInstance(p.PropertyType) : null;
            object? value = p.GetValue(a);
            if (!Equals(defaultValue, value))
            {
                ret[p.Name] = (p, value);
            }
        }

        return ret;
    }

    private (string, string) BuildParameterDeclarationAndArgumentRValue(ParameterInfo param)
    {
        Type paramT = param.ParameterType;
        if (paramT.IsByRef)
        {
            paramT = paramT.GetElementType()!;
        }
        StandardizedType paramType = GetStandardizedType(paramT);
        AddUsing(paramType);
        string paramTypeStr = paramType.UseType();
        string declaration = "";
        if (param.IsOut)
        {
            declaration = "out ";
            paramTypeStr = paramTypeStr.Replace("&", "");
        }
        declaration += $"{paramTypeStr} {param.Name}";
        
        //TODO handle out parameters that are wrapped.
        string arg = "";
        if (param.IsOut) arg = "out ";
        arg += param.Name;
        if (paramType.IsWrapped) arg += ".WrappedInstance";
        return (declaration, arg);
    }

    private string ConvertRValueToWrapped(string rValue, StandardizedType type)
    {
        if (type.Original == typeof(void)) return rValue;
        if (type.IsWrapped)
        {
            if (type.IsArray)
            {
                StandardizedType itemType = GetStandardizedType(type.Original.GetElementType()!);
                return $"{rValue}.Select(e => new {itemType.UseType(forceClass: true)}(e)).ToArray()";
            }
            return $"new {type.UseType(forceClass: true)}({rValue})";
        }

        Type[] paramTypes = type.Original.GetGenericArguments();
        if (type.Name.StartsWith("IEnumerable"))
        {
            StandardizedType iEnumType = GetStandardizedType(paramTypes[0]);
            if (iEnumType.IsWrapped)
            {
                return $"{rValue}.Select(e => new {iEnumType.UseType(forceClass: true)}(e))";
            }
        }

        return rValue;
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
        Type[] genericArgs = method.GetGenericArguments();
        if (genericArgs.Length == 0) return "";
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
            cName = eWrap == null ? type.Name : eWrap.ClassNameToGenerate;
            iName = eWrap?.InterfaceNameToGenerate;
        }
        else
        {
            parameterized = generics.Select(GetStandardizedType).ToArray();
            cName = (eWrap == null ? type.Name : eWrap.ClassNameToGenerate).Split('`').First();
            iName = eWrap?.InterfaceNameToGenerate.Split('`').First();
        }

        return new StandardizedType(type, ns, cName, iName, parameterized, eWrap != null, type.IsArray);
    }
}