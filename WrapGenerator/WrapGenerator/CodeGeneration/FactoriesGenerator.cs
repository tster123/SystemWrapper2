using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WrapGenerator.CodeGeneration;

public class FactoriesGenerator(TypeFactory factory)
{

    public string? GenerateSource()
    {
        if (factory.FactoryMethods.Count == 0) return null;

        
        HashSet<string> usings = new();
        StringBuilder methods = new();
        foreach (KeyValuePair<string, FactoryMethodDemand> method in factory.FactoryMethods)
        {
            AppendFactoryMethod(method.Key, method.Value.Wrap, method.Value.StType, methods, usings);
        }

        StringBuilder ret = new();
        foreach (string u in usings)
        {
            ret.AppendLine($"using {u};");
        }
        ret.AppendLine("internal static class Factories {");
        ret.Append(methods);
        ret.AppendLine("}");
        return ret.ToString();
    }

    private void AppendFactoryMethod(string methodName, ClassToWrap wrap, StandardizedType st, StringBuilder ret, HashSet<string> usings)
    {
        if (st.Namespace != null) usings.Add(st.Namespace);
        if (wrap.Type.Namespace != null) usings.Add(wrap.Type.Namespace);
        // force the factory method to not be an array
        st = new StandardizedType(st.Original, st.Namespace, st.Name, st.Interface, st.ParameterizedTypes, st.Factory,
            st.IsWrapped, isArray: false);
        ret.AppendLine($"    internal static {st.UseType()} {methodName}({wrap.Type.Name} inner) {{");
        List<StandardizedType> implementers = wrap.Implementers.Select(i => factory.GetStandardizedType(i.Type)).ToList();
        int i = 0;
        foreach (StandardizedType type in implementers)
        {
            string tName = "r" + i;
            if (type.Namespace != null) usings.Add(type.Namespace);
            if (type.Original.Namespace != null) usings.Add(type.Original.Namespace);
            ret.AppendLine($"        if (inner is {type.Original.Name} {tName}) return {type.FactoryMethodCall(tName)};");
            i++;
        }

        ret.AppendLine($"        return new {st.UseType(forceClass: true)}(inner);");
        ret.AppendLine("    }");
    }
}
