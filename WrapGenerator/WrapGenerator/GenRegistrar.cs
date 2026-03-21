using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace WrapGenerator;

public class GenRegistrar
{
    private readonly List<ClassToWrap> classesToWrap = new();

    public void Register(ClassToWrap toWrap)
    {
        classesToWrap.Add(toWrap);
    }

    public IEnumerable<ClassToWrap> AllClassesToWrap => classesToWrap;

    public ClassToWrap? GetWrap(Type sourceType)
    {
        return AllClassesToWrap.FirstOrDefault(c => c.Type == sourceType);
    }

    public void Register(WrapNamespace nsWrap)
    {
        try
        {
            foreach (Assembly a in AppDomain.CurrentDomain.GetAssemblies())
            {
                LoadAssembly(nsWrap, a);
            }
        }
        catch (ReflectionTypeLoadException)
        {
            foreach (Assembly a in AppDomain.CurrentDomain.ReflectionOnlyGetAssemblies())
            {
                LoadAssembly(nsWrap, a);
            }
        }
        
    }

    private void LoadAssembly(WrapNamespace nsWrap, Assembly a)
    {
        Regex? exclude = null, include = null;
        if (nsWrap.ExcludeFilter != null)
        {
            exclude = new Regex(nsWrap.ExcludeFilter);
        }

        if (nsWrap.OnlyIncludeFilter != null)
        {
            include = new Regex(nsWrap.OnlyIncludeFilter);
        }
        foreach (Type t in a.GetTypes())
        {
            if (t == typeof(FileSystemWatcher))
            {
                Console.WriteLine("foo");
            }
            if (t.IsNotPublic) continue;
            if (!t.IsClass) continue;
            if (t.IsNested) continue;
            // static classes
            //if (t.IsAbstract && !t.IsSealed) continue; // don't wrap abstract classes, but do wrap static classes.
            if (typeof(Exception).IsAssignableFrom(t)) continue;
            if (t.Namespace == nsWrap.Namespace)
            {
                if (exclude != null && exclude.IsMatch(t.Name)) continue;
                if (include != null && !include.IsMatch(t.Name)) continue;
                ClassToWrap wrap = new(t, nsWrap);
                Register(wrap);
            }
        }
    }
}