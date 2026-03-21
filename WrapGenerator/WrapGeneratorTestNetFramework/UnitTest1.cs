using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using WrapGenerator;

namespace WrapGeneratorTestNetFramework
{
    [TestClass]
    public class UnitTest1
    {
        private static readonly WrapNamespace wrapNs = new()
        {
            Namespace = "System.IO",
            TargetNamespaceFormat = "Wrapped.TestClasses"
        };

        [TestMethod]
        public void StreamTest()
        {
            GenRegistrar registrar = new();
            ClassToWrap wrap = new(typeof(Path), wrapNs);
            registrar.Register(wrap);
            TypeFactory factory = new(registrar);
            SingleClassSourceGenerator generator = new(factory, wrap);
            string code = generator.GeneratorSource();
            Console.WriteLine(code);
        }
    }
}
