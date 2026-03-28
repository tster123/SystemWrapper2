using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using WrapGenerator;

namespace WrapGeneratorTestNetFramework
{

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
            generator.IncludeDllLocation = true;
            string code = generator.GeneratorSource();
            Console.WriteLine(code);
        }
    }
}
