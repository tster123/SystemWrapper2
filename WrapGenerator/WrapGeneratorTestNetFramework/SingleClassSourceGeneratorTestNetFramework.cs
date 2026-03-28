using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using WrapGenerator;

namespace WrapGeneratorTestNetFramework
{

    public class TestSourceGeneratorContext : ISourceGeneratorContext
    {
        public CancellationToken CancellationToken => new(false);

        public TestSourceGeneratorContext()
        {
        }

        public Dictionary<string, string> SourceFiles = new();

        public void AddSource(string hintName, string source) => SourceFiles[hintName] = source;
    }

    [TestClass]
    public class SingleClassSourceGeneratorTestNetFramework
    {
        private static readonly WrapNamespace wrapNs = new()
        {
            Namespace = "System.IO",
            TargetNamespaceFormat = "Wrapped.TestClasses"
        };

        private readonly GenRegistrar registrar = new();

        [TestMethod]
        public void StreamTest()
        {
            ClassToWrap wrap = new(typeof(Stream), wrapNs);
            registrar.Register(wrap);
            TypeFactory factory = new(registrar);
            SingleClassSourceGenerator generator = new(factory, wrap);
            string code = generator.GeneratorSource();
            Console.WriteLine(code);
        }
    }
}
