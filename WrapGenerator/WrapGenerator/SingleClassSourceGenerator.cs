using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace WrapGenerator;

//TODO - do events properly
public class SingleClassSourceGenerator
{
    private readonly ClassToWrap wrap;
    private readonly HashSet<string> usings = new();
    private readonly StringBuilder interfaceStr = new();
    private readonly StringBuilder classStr = new();
    private readonly string wrappedProperty;
    private readonly StandardizedType standardizedType;
    private readonly TypeFactory factory;

    public SingleClassSourceGenerator(TypeFactory factory, ClassToWrap wrap)
    {
        this.factory = factory;
        this.wrap = wrap;
        standardizedType = factory.GetStandardizedType(wrap.Type);
        wrappedProperty = $"Wrapped{standardizedType.UseType(true)}";
    }

    public string GeneratorSource()
    {
        usings.Add("SystemWrapper2");
        usings.Add(wrap.Type.Namespace);
        string topLevelGenerics = wrap.ClassLevelGenericParameters == null ? "" : $"<{string.Join(", ", wrap.ClassLevelGenericParameters)}>";
        interfaceStr.Append($"public interface {wrap.InterfaceNameToGenerate}{topLevelGenerics}");
        List<StandardizedType> implementedInterfaces = wrap.Type.GetInterfaces().Select(factory.GetStandardizedType).ToList();

        // we might have wrapped the base class (for example FileStream extends Stream which is wrapped), so check if we did and add it
        Type? baseClass = wrap.Type.BaseType;
        if (baseClass != null)
        {
            StandardizedType st = factory.GetStandardizedType(baseClass);
            if (st.IsWrapped && st.Interface != null)
            {
                implementedInterfaces.Add(st);
            }
        }

        if (implementedInterfaces.Count != 0)
        {
            // TODO: handle generic interfaces
            interfaceStr.Append(" : " + string.Join(", ", implementedInterfaces.Select(st => st.Interface ?? st.Name)));
            foreach (StandardizedType t in implementedInterfaces)
            {
                if (t.Namespace != null) usings.Add(t.Namespace);
            }
        }

        interfaceStr.AppendLine(" {");
        string wrappedTypeName = standardizedType.UseType(true);

        classStr.AppendLine($"public class {wrap.ClassNameToGenerate}{topLevelGenerics} : {wrap.InterfaceNameToGenerate}{topLevelGenerics} {{");
        if (!wrap.IsStatic)
        {
            interfaceStr.AppendLine($"\t{wrappedTypeName} {wrappedProperty} {{ get; }}");

            classStr.AppendLine($"\tprivate readonly {wrappedTypeName} inner;");
            classStr.AppendLine($"\tpublic {wrappedTypeName} {wrappedProperty} => inner;");

            Type? walkUp = wrap.Type.BaseType;
            while (walkUp != null)
            {
                StandardizedType baseType = factory.GetStandardizedType(walkUp);
                if (baseType.IsWrapped)
                {
                    classStr.AppendLine($"\tpublic {baseType.UseType(true)} Wrapped{baseType.UseType(true)} => inner;");
                }
                walkUp = walkUp.BaseType;
            }

            GenerateConstructors();
            classStr.AppendLine();
            GenerateProperties(implementedInterfaces);
            classStr.AppendLine();
            GenerateEvents(implementedInterfaces);
        }

        GenerateMethods(implementedInterfaces);
        StringBuilder s = new();
        s.AppendLine("// from " + wrap.Type.Assembly.CodeBase);
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

    private void GenerateConstructors()
    {
        // add the wrap constructor.  all other constructors will call this
        string innerTypeStr = standardizedType.UseType(true);
        classStr.AppendLine($@"	public {wrap.ClassNameToGenerate}({innerTypeStr} inner) {{
		this.inner = inner;
	}}");

        ConstructorInfo[] ctors = wrap.Type.GetConstructors(BindingFlags.Public | BindingFlags.Instance);
        Array.Sort(ctors, MethodComparer);
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

    private int DistanceToRoot(Type? t)
    {
        if (t == null || t == typeof(object) || t == typeof(void) || t.BaseType == null) return 0;
        return 1 + DistanceToRoot(t.BaseType);
    }

    private int MethodComparer(MethodBase x, MethodBase y)
    {
        int xd = DistanceToRoot(x.DeclaringType);
        int yd = DistanceToRoot(y.DeclaringType);
        if (xd != yd) return yd - xd;
        int r = StringComparer.Ordinal.Compare(x.Name, y.Name);
        if (r != 0) return r;
        ParameterInfo[] xp = x.GetParameters();
        ParameterInfo[] yp = y.GetParameters();
        if (xp.Length < yp.Length) return -1;
        if (xp.Length > yp.Length) return 1;
        for (int i = 0; i < xp.Length; i++)
        {
            string xpn = xp[i].ParameterType.ToString();
            string ypn = yp[i].ParameterType.ToString();
            r = StringComparer.Ordinal.Compare(xpn, ypn);
            if (r != 0) return r;
        }

        return 0;
    }

    private static readonly List<MethodInfo> methodsToSkip =
    [
        typeof(object).GetMethod("GetType")
    ];

    private void GenerateMethods(List<StandardizedType> implementedInterfaces)
    {
        BindingFlags staticOrInstance = wrap.IsStatic ? BindingFlags.Static : BindingFlags.Instance;
        MethodInfo[] methods = wrap.Type.GetMethods(staticOrInstance | BindingFlags.Public);
        Array.Sort(methods, MethodComparer);
        var props = wrap.Type.GetProperties();
        var events = wrap.Type.GetEvents();

        foreach (MethodInfo method in methods)
        {
            if (methodsToSkip.Any(m => m.DeclaringType == method.DeclaringType && m.Name == method.Name)) 
                continue;

            // skip methods that are part of properties or events
            if (props.Any(p => p.GetMethod == method | p.SetMethod == method)) continue;
            if (events.Any(e => e.AddMethod == method || e.RemoveMethod == method)) continue;

            // use a dummy StringBuilder if the declaring type is not this type so we don't redeclare methods
            // on the interface
            MethodInfo baseMethod = method.GetBaseDefinition();
            StringBuilder interStr = interfaceStr;
            if (//method.DeclaringType != wrap.Type ||
                //baseMethod != null && baseMethod.DeclaringType != method.DeclaringType ||
                IsFromTypes(method, implementedInterfaces) ||
                (baseMethod ?? method).DeclaringType == typeof(object))
            {
                interStr = new StringBuilder();
            }

            string? attrStr = BuildAttributes(method);
            if (attrStr != null)
            {
                interStr.Append(attrStr);
                classStr.Append(attrStr);
            }
            StandardizedType retType = factory.GetStandardizedType(method.ReturnType);
            AddUsing(retType);
            string overrideStr = IsFromTypes(method, [factory.GetStandardizedType(typeof(object))]) ? "override " : "";
            string methodGenericParamString = GenerateMethodGenericParamString(method);
            string retTypeStr = retType.UseType();
            interStr.Append($"\tpublic {retTypeStr} {method.Name}{methodGenericParamString}(");
            classStr.Append($"\tpublic {overrideStr}{retTypeStr} {method.Name}{methodGenericParamString}(");
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

            string inner = wrap.IsStatic ? wrap.Type.Name : "inner";
            string rValue = retType.WrapRValueCode($"{inner}.{method.Name}({argList})");
            string formattedCall = string.Format(retFormat, rValue);
            classStr.AppendLine($"\t\t{formattedCall};");
            classStr.AppendLine("\t}");
        }
    }

    private string? BuildAttributes(MethodInfo method)
    {
        Attribute[] attrs = method.GetCustomAttributes().ToArray();
        if (attrs.Length == 0) return null;
        StringBuilder ret = new();
        foreach (Attribute a in attrs)
        {
            string? attrCode = BuildSingleAttributeCode(method, a);
            if (attrCode != null) ret.Append("\t").AppendLine(attrCode);
        }

        return ret.ToString();
    }

    private string? BuildSingleAttributeCode(MethodInfo method, Attribute a)
    {
        string name = a.GetType().Name;
        if (a.GetType().Namespace == "System.Runtime.CompilerServices") return null;
        if (name == "__DynamicallyInvokableAttribute") return null;
        AddUsing(factory.GetStandardizedType(a.GetType()));
        if (name.EndsWith("Attribute")) name = name.Substring(0, name.Length - "Attribute".Length);
        Dictionary<string, AttributePropertyInfo> attrProps = GetAttributePropertiesWithNonDefaultValues(a);

        ConstructorInfo? ctor = GetValidConstructor(a);
        if (ctor == null)
        {
            Console.Error.WriteLine($"WARNING: cannot find ctor for [{name}] on {wrap.Type.Name}.{method.Name}");
            return $"[{name}]";
        }

        List<String> attrParts = new();
        foreach (ParameterInfo param in ctor.GetParameters())
        {
            bool found = false;
            foreach (KeyValuePair<string, AttributePropertyInfo> prop in attrProps.ToArray())
            {
                if (prop.Value.Property.PropertyType != param.ParameterType ||
                    !prop.Key.Equals(param.Name, StringComparison.InvariantCultureIgnoreCase)) continue;
                found = true;
                object? val = prop.Value.Value;
                object? defaultValue =
                    param.HasDefaultValue ? param.DefaultValue : DefaultValueOf(param.ParameterType);
                if (Equals(val, defaultValue)) break;

                var strVal = GetCodeOfAttributeValue(val, prop.Value.Property.PropertyType);
                attrParts.Add($"{param.Name}: {strVal}");
                attrProps.Remove(prop.Key);
                break;
            }

            if (!found && !param.IsOptional)
            {
                object? defaultValue = DefaultValueOf(param.ParameterType);
                attrParts.Add($"{param.Name}: {GetCodeOfAttributeValue(defaultValue, param.ParameterType)}");
            }
        }

        foreach (KeyValuePair<string, AttributePropertyInfo> prop in attrProps.ToArray())
        {
            attrParts.Add($"{prop.Key} = {GetCodeOfAttributeValue(prop.Value.Value, prop.Value.Property.PropertyType)}");
        }

        return $"[{name}({string.Join(", ", attrParts)})]";
    }

    string GetCodeOfAttributeValue(object? val, Type valType)
    {
        string strVal = val?.ToString() ?? "null";
        if (val != null)
        {
            if (valType == typeof(string))
            {
                strVal = "\"" + strVal + "\"";
            }
            else if (valType == typeof(bool))
            {
                strVal = strVal.ToLower();
            }
            else if (valType.IsEnum)
            {
                AddUsing(factory.GetStandardizedType(valType));
                strVal = $"{valType.Name}.{strVal}";
            }
        }

        return strVal;
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
    }

    private class AttributePropertyInfo
    {
        public PropertyInfo Property { get; }
        public object? Value { get; }

        public AttributePropertyInfo(PropertyInfo property, object? value)
        {
            Property = property;
            Value    = value;
        }
    }

    private object? DefaultValueOf(Type t) => t.IsValueType ? Activator.CreateInstance(t) : null;

    private Dictionary<string, AttributePropertyInfo> GetAttributePropertiesWithNonDefaultValues(Attribute a)
    {
        Dictionary<string, AttributePropertyInfo> ret = new();
        foreach (PropertyInfo p in a.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (p.DeclaringType == typeof(object) || p.DeclaringType == typeof(Attribute)) continue;
            object? defaultValue = DefaultValueOf(p.PropertyType);
            object? value = p.GetValue(a);
            if (!Equals(defaultValue, value))
            {
                ret[p.Name] = new(p, value);
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
        StandardizedType paramType = factory.GetStandardizedType(paramT);
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
        if (paramType.IsWrapped) arg += "." + $"Wrapped{paramType.UseType(true)}";
        return (declaration, arg);
    }

    private static bool IsFromTypes(MethodInfo method, List<StandardizedType> types)
    {
        foreach (StandardizedType type in types)
        {
            foreach (MethodInfo m in type.Original.GetMethods())
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
    private static bool IsFromTypes(PropertyInfo property, List<StandardizedType> types)
    {
        foreach (StandardizedType type in types)
        {
            PropertyInfo? otherProperty = type.Original.GetProperty(property.Name);
            if (otherProperty != null) return true;
        }
        return false;
    }
    private static bool IsFromTypes(EventInfo ev, List<StandardizedType> types)
    {
        foreach (StandardizedType type in types)
        {
            EventInfo? otherEvent = type.Original.GetEvent(ev.Name);
            if (otherEvent != null) return true;
        }
        return false;
    }

    private string GenerateMethodGenericParamString(MethodInfo method)
    {
        Type[] genericArgs = method.GetGenericArguments();
        if (genericArgs.Length == 0) return "";
        return $"<{string.Join(", ", genericArgs.Select(a => a.Name))}>";
    }

    private void GenerateProperties(List<StandardizedType> implementedInterfaces)
    {
        PropertyInfo[] props = wrap.Type.GetProperties(BindingFlags.Instance | BindingFlags.Public);
        foreach (PropertyInfo property in props)
        {
            StringBuilder interStr = interfaceStr;
            if (IsFromTypes(property, implementedInterfaces)) interStr = new StringBuilder();

            StandardizedType propType = factory.GetStandardizedType(property.PropertyType);
            bool needsWrap = propType.IsWrapped;
            AddUsing(propType);

            string typeStr = propType.UseType();

            interStr.Append($"\tpublic {typeStr} {property.Name} {{ ");
            classStr.AppendLine($"\tpublic {typeStr} {property.Name} {{ ");

            if (property.GetMethod != null && property.GetMethod.IsPublic)
            {
                interStr.Append("get; ");
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
                string rValue = needsWrap ? $"value.Wrapped{propType.UseType(true)}" : "value";
                interStr.Append("set; ");
                classStr.AppendLine($"\t\tset => inner.{property.Name} = {rValue}; ");
            }

            interStr.AppendLine("}");
            classStr.AppendLine("\t}");
        }
    }

    private void GenerateEvents(List<StandardizedType> implementedInterfaces)
    {
        EventInfo[] events = wrap.Type.GetEvents();
        foreach (EventInfo ev in events)
        {
            StringBuilder interStr = interfaceStr;
            if (IsFromTypes(ev, implementedInterfaces)) interStr = new StringBuilder();

            StandardizedType eventHandlerType = factory.GetStandardizedType(ev.EventHandlerType);
            bool needsWrap = eventHandlerType.IsWrapped;
            AddUsing(eventHandlerType);

            string typeStr = eventHandlerType.UseType();

            interStr.AppendLine($"\tpublic event {typeStr} {ev.Name}; ");
            classStr.AppendLine($"\tpublic event {typeStr} {ev.Name} {{ ");

            if (ev.AddMethod != null && ev.AddMethod.IsPublic)
            {
                classStr.AppendLine($"\t\tadd {{ inner.{ev.Name} += value; }}");
            }

            if (ev.RemoveMethod != null && ev.RemoveMethod.IsPublic)
            {
                classStr.AppendLine($"\t\tremove {{ inner.{ev.Name} -= value; }}");
            }

            classStr.AppendLine("\t}");
        }
    }

    private void AddUsing(StandardizedType type)
    {
        if (type.Namespace != null && type.Namespace != wrap.TargetNamespace) usings.Add(type.Namespace);
    }
}