using Microsoft.CodeAnalysis;
using System.Reflection;
using WrapGenerator;
using WrapGeneratorTest.TestClasses;

namespace WrapGeneratorTest;


public class TestSourceGeneratorContext : ISourceGeneratorContext
{
    public IEnumerable<AdditionalText> AdditionalFiles { get; }
    public CancellationToken CancellationToken => new(false);
    public MetadataLoadContext Domain { get; }

    public TestSourceGeneratorContext(MetadataLoadContext domain, IEnumerable<AdditionalText> additionalFiles)
    {
        Domain = domain;
        AdditionalFiles = additionalFiles;
    }

    public Dictionary<string, string> SourceFiles = new();

    public void AddSource(string hintName, string source) => SourceFiles[hintName] = source;
}

[TestClass]
public sealed class SingleClassSourceGeneratorTest
{
    string[] assemblyFiles;
    MetadataLoadContext loadContext;
    TestSourceGeneratorContext testContext;
    GenRegistrar registrar;

    public SingleClassSourceGeneratorTest()
    {
        assemblyFiles = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.Location).Where(s => !string.IsNullOrEmpty(s)).ToArray();
        loadContext = new(new PathAssemblyResolver(assemblyFiles));
        testContext = new(loadContext, []);
        registrar = new(testContext);
    }


    private static readonly WrapNamespace wrapNs = new()
    {
        Namespace = "System.IO",
        TargetNamespaceFormat = "Wrapped.TestClasses"
    };

    private void DoTest(Type toWrap, Type[] otherToRegister, string? expected)
    {
        ClassToWrap wrap = new(toWrap, wrapNs);
        registrar.Register(wrap);

        foreach (Type other in otherToRegister)
        {
            ClassToWrap otherWrap = new(other, wrapNs);
            registrar.Register(otherWrap);
        }

        TypeFactory factory = new(registrar);
        SingleClassSourceGenerator generator = new(factory, wrap);
        string code = generator.GeneratorSource();
        Console.WriteLine(code);
        if (expected != null)
        {
            CodeAsserts.AreEqual(code, expected);
        }
    }

    private void DoTest(string className, string? expected)
    {
        ClassToWrap wrap = new(Type.GetType("WrapGeneratorTest.TestClasses." + className)!, wrapNs);
        registrar.Register(wrap);
        TypeFactory factory = new(registrar);
        SingleClassSourceGenerator generator = new(factory, wrap);
        string code = generator.GeneratorSource();
        Console.WriteLine(code);
        if (expected != null)
        {
            CodeAsserts.AreEqual(code, expected);
        }
    }

    [TestMethod]
    public void PropertyTest()
    {
        DoTest("PropertyExample", @"
using SystemWrapper2;
using WrapGeneratorTest.TestClasses;
using System;
namespace Wrapped.TestClasses {
public interface IPropertyExampleWrap {
	PropertyExample WrappedPropertyExample { get; }
    public int Foo { get; set; }
}

public class PropertyExampleWrap : IPropertyExampleWrap {
	private readonly PropertyExample inner;
	public PropertyExample WrappedPropertyExample => inner;
	public PropertyExampleWrap(PropertyExample inner) {
		this.inner = inner;
	}
	public PropertyExampleWrap()
		: this(new PropertyExample()) { }

	public int Foo { 
		get => inner.Foo; 
		set => inner.Foo = value; 
	}

	public override bool Equals(Object obj) {
		return inner.Equals(obj);
	}
	public override int GetHashCode() {
		return inner.GetHashCode();
	}
	public override string ToString() {
		return inner.ToString();
	}
}

}");
    }

    [TestMethod]
    public void ListTest()
    {
        DoTest("ListExample", @"
using SystemWrapper2;
using WrapGeneratorTest.TestClasses;
using System.Collections.Generic;
using System;
namespace Wrapped.TestClasses {
public interface IListExampleWrap {
	ListExample WrappedListExample { get; }
	public List<string> Method(List<int> foo);
}

public class ListExampleWrap : IListExampleWrap {
	private readonly ListExample inner;
	public ListExample WrappedListExample => inner;
	public ListExampleWrap(ListExample inner) {
		this.inner = inner;
	}
	public ListExampleWrap()
		: this(new ListExample()) { }


	public List<string> Method(List<int> foo) {
		return inner.Method(foo);
	}
	public override bool Equals(Object obj) {
		return inner.Equals(obj);
	}
	public override int GetHashCode() {
		return inner.GetHashCode();
	}
	public override string ToString() {
		return inner.ToString();
	}
}

}");
    }

    [TestMethod]
    public void ArrayTest()
    {
        DoTest("ArrayExample", @"
using SystemWrapper2;
using WrapGeneratorTest.TestClasses;
using System;
namespace Wrapped.TestClasses {
public interface IArrayExampleWrap {
	ArrayExample WrappedArrayExample { get; }
	public String[] Method(Int32[] foo);
}

public class ArrayExampleWrap : IArrayExampleWrap {
	private readonly ArrayExample inner;
	public ArrayExample WrappedArrayExample => inner;
	public ArrayExampleWrap(ArrayExample inner) {
		this.inner = inner;
	}
	public ArrayExampleWrap()
		: this(new ArrayExample()) { }


	public String[] Method(Int32[] foo) {
		return inner.Method(foo);
	}
	public override bool Equals(Object obj) {
		return inner.Equals(obj);
	}
	public override int GetHashCode() {
		return inner.GetHashCode();
	}
	public override string ToString() {
		return inner.ToString();
	}
}

}");
    }

    [TestMethod]
    public void NullableTest()
    {
        DoTest("NullableExample", @"
using SystemWrapper2;
using WrapGeneratorTest.TestClasses;
using System;
namespace Wrapped.TestClasses {
public interface INullableExampleWrap {
	NullableExample WrappedNullableExample { get; }
	public Nullable<byte> Method(Nullable<int> foo);
}

public class NullableExampleWrap : INullableExampleWrap {
	private readonly NullableExample inner;
	public NullableExample WrappedNullableExample => inner;
	public NullableExampleWrap(NullableExample inner) {
		this.inner = inner;
	}
	public NullableExampleWrap()
		: this(new NullableExample()) { }


	public Nullable<byte> Method(Nullable<int> foo) {
		return inner.Method(foo);
	}
	public override bool Equals(Object obj) {
		return inner.Equals(obj);
	}
	public override int GetHashCode() {
		return inner.GetHashCode();
	}
	public override string ToString() {
		return inner.ToString();
	}
}

}");
    }

    [TestMethod]
    public void GenericTest()
    {
        Console.WriteLine(typeof(GenericExample<>).Name);
        DoTest(typeof(GenericExample<>), [], @"
using SystemWrapper2;
using WrapGeneratorTest.TestClasses;
using System;
namespace Wrapped.TestClasses {
public interface IGenericExample_T1_Wrap<T1> {
	GenericExample<T1> WrappedGenericExample<T1> { get; }
	public T1 Method<T2>(T2 foo);
}

public class GenericExample_T1_Wrap<T1> : IGenericExample_T1_Wrap<T1> {
	private readonly GenericExample<T1> inner;
	public GenericExample<T1> WrappedGenericExample<T1> => inner;
	public GenericExample_T1_Wrap(GenericExample<T1> inner) {
		this.inner = inner;
	}
	public GenericExample_T1_Wrap()
		: this(new GenericExample<T1>()) { }


	public T1 Method<T2>(T2 foo) {
		return inner.Method(foo);
	}
	public override bool Equals(Object obj) {
		return inner.Equals(obj);
	}
	public override int GetHashCode() {
		return inner.GetHashCode();
	}
	public override string ToString() {
		return inner.ToString();
	}
}

}");
    }

    [TestMethod]
    public void InterfaceTest()
    {
        DoTest("InterfaceExample", @"
using SystemWrapper2;
using WrapGeneratorTest.TestClasses;
using System;
namespace Wrapped.TestClasses {
public interface IInterfaceExampleWrap : IFoo, IBar {
	InterfaceExample WrappedInterfaceExample { get; }
	public int Mork { get; set; }
}

public class InterfaceExampleWrap : IInterfaceExampleWrap {
	private readonly InterfaceExample inner;
	public InterfaceExample WrappedInterfaceExample => inner;
	public InterfaceExampleWrap(InterfaceExample inner) {
		this.inner = inner;
	}
	public InterfaceExampleWrap()
		: this(new InterfaceExample()) { }

	public int Mork { 
		get => inner.Mork; 
		set => inner.Mork = value; 
	}

	public void Bar() {
		inner.Bar();
	}
	public void Foo() {
		inner.Foo();
	}
	public override bool Equals(Object obj) {
		return inner.Equals(obj);
	}
	public override int GetHashCode() {
		return inner.GetHashCode();
	}
	public override string ToString() {
		return inner.ToString();
	}
}

}");
    }

    [TestMethod]
    public void DisposableTest()
    {
        DoTest("DisposableExample", @"
using SystemWrapper2;
using WrapGeneratorTest.TestClasses;
using System;
namespace Wrapped.TestClasses {
public interface IDisposableExampleWrap : IDisposable {
	DisposableExample WrappedDisposableExample { get; }
}

public class DisposableExampleWrap : IDisposableExampleWrap {
	private readonly DisposableExample inner;
	public DisposableExample WrappedDisposableExample => inner;
	public DisposableExampleWrap(DisposableExample inner) {
		this.inner = inner;
	}
	public DisposableExampleWrap()
		: this(new DisposableExample()) { }


	public void Dispose() {
		inner.Dispose();
	}
	public override bool Equals(Object obj) {
		return inner.Equals(obj);
	}
	public override int GetHashCode() {
		return inner.GetHashCode();
	}
	public override string ToString() {
		return inner.ToString();
	}
}

}");
    }

    [TestMethod]
    public void WrappingTest()
    {
        DoTest("WrappingExample", @"
using SystemWrapper2;
using WrapGeneratorTest.TestClasses;
using System;
namespace Wrapped.TestClasses {
public interface IWrappingExampleWrap {
	WrappingExample WrappedWrappingExample { get; }
	public PropertyExample Prop { get; set; }
	public PropertyExample[] MakeProperties(PropertyExample[] props);
}

public class WrappingExampleWrap : IWrappingExampleWrap {
	private readonly WrappingExample inner;
	public WrappingExample WrappedWrappingExample => inner;
	public WrappingExampleWrap(WrappingExample inner) {
		this.inner = inner;
	}
	public WrappingExampleWrap()
		: this(new WrappingExample()) { }

	public PropertyExample Prop { 
		get => inner.Prop; 
		set => inner.Prop = value; 
	}

	public PropertyExample[] MakeProperties(PropertyExample[] props) {
		return inner.MakeProperties(props);
	}
	public override bool Equals(Object obj) {
		return inner.Equals(obj);
	}
	public override int GetHashCode() {
		return inner.GetHashCode();
	}
	public override string ToString() {
		return inner.ToString();
	}
}

}");
    }

    [TestMethod]
    public void ArraySegmentTest()
    {
        DoTest("ArraySegmentExample", @"
using SystemWrapper2;
using WrapGeneratorTest.TestClasses;
using System;
namespace Wrapped.TestClasses {
public interface IArraySegmentExampleWrap {
	ArraySegmentExample WrappedArraySegmentExample { get; }
	public void Foo(out ArraySegment<byte> foo);
}

public class ArraySegmentExampleWrap : IArraySegmentExampleWrap {
	private readonly ArraySegmentExample inner;
	public ArraySegmentExample WrappedArraySegmentExample => inner;
	public ArraySegmentExampleWrap(ArraySegmentExample inner) {
		this.inner = inner;
	}
	public ArraySegmentExampleWrap()
		: this(new ArraySegmentExample()) { }


	public void Foo(out ArraySegment<byte> foo) {
		inner.Foo(out foo);
	}
	public override bool Equals(Object obj) {
		return inner.Equals(obj);
	}
	public override int GetHashCode() {
		return inner.GetHashCode();
	}
	public override string ToString() {
		return inner.ToString();
	}
}

}");
    }

    [TestMethod]
    public void OutTest() // TODO, shouldn't out parameter be wrapped?
    {
        DoTest("OutExample", @"
using SystemWrapper2;
using WrapGeneratorTest.TestClasses;
using System;
namespace Wrapped.TestClasses {
public interface IOutExampleWrap {
	OutExample WrappedOutExample { get; }
	public void Mork(out int a, out PropertyExample p);
}

public class OutExampleWrap : IOutExampleWrap {
	private readonly OutExample inner;
	public OutExample WrappedOutExample => inner;
	public OutExampleWrap(OutExample inner) {
		this.inner = inner;
	}
	public OutExampleWrap()
		: this(new OutExample()) { }


	public void Mork(out int a, out PropertyExample p) {
		inner.Mork(out a, out p);
	}
	public override bool Equals(Object obj) {
		return inner.Equals(obj);
	}
	public override int GetHashCode() {
		return inner.GetHashCode();
	}
	public override string ToString() {
		return inner.ToString();
	}
}

}");
    }

    [TestMethod]
    public void CtorTest() // TODO: shouldn't ctor parameter be wrapped?
    {
        DoTest("CtorExample", @"
using SystemWrapper2;
using WrapGeneratorTest.TestClasses;
using System;
namespace Wrapped.TestClasses {
public interface ICtorExampleWrap {
	CtorExample WrappedCtorExample { get; }
}

public class CtorExampleWrap : ICtorExampleWrap {
	private readonly CtorExample inner;
	public CtorExample WrappedCtorExample => inner;
	public CtorExampleWrap(CtorExample inner) {
		this.inner = inner;
	}
	public CtorExampleWrap(string foo)
		: this(new CtorExample(foo)) { }
	public CtorExampleWrap(int a, string b, OutExample c)
		: this(new CtorExample(a, b, c)) { }


	public override bool Equals(Object obj) {
		return inner.Equals(obj);
	}
	public override int GetHashCode() {
		return inner.GetHashCode();
	}
	public override string ToString() {
		return inner.ToString();
	}
}

}");
    }

    [TestMethod]
    public void AttributesTest()
    {
        DoTest("AttributesExample", @"
using SystemWrapper2;
using WrapGeneratorTest.TestClasses;
using System.Runtime.Versioning;
using System;
namespace Wrapped.TestClasses {
public interface IAttributesExampleWrap {
	AttributesExample WrappedAttributesExample { get; }
	[SupportedOSPlatform(platformName: ""windows"")]
	[UnsupportedOSPlatform(platformName: ""linux"", message: null)]
	[Obsolete(message: null, error: false)]
	[Example(boolean: true, anEnum: ExampleEnum.Bar, integer: 42, String1 = ""foo"", AnotherString = ""Foo"")]
	public void Double();
	[Obsolete(message: ""This API supports obsolete formatter-based serialization. It should not be called or extended by application code."", error: false, DiagnosticId = ""SYSLIB0051"", UrlFormat = ""https://aka.ms/dotnet-warnings/{0}"")]
	public void OldMethod();
	[Obsolete(message: null, error: false)]
	[Example(boolean: true, anEnum: ExampleEnum.Buz, String1 = ""foo"", Integer = 1)]
	public void Single();
}

public class AttributesExampleWrap : IAttributesExampleWrap {
	private readonly AttributesExample inner;
	public AttributesExample WrappedAttributesExample => inner;
	public AttributesExampleWrap(AttributesExample inner) {
		this.inner = inner;
	}
	public AttributesExampleWrap()
		: this(new AttributesExample()) { }


	[SupportedOSPlatform(platformName: ""windows"")]
	[UnsupportedOSPlatform(platformName: ""linux"", message: null)]
	[Obsolete(message: null, error: false)]
	[Example(boolean: true, anEnum: ExampleEnum.Bar, integer: 42, String1 = ""foo"", AnotherString = ""Foo"")]
	public void Double() {
		inner.Double();
	}
	[Obsolete(message: ""This API supports obsolete formatter-based serialization. It should not be called or extended by application code."", error: false, DiagnosticId = ""SYSLIB0051"", UrlFormat = ""https://aka.ms/dotnet-warnings/{0}"")]
	public void OldMethod() {
		inner.OldMethod();
	}
	[Obsolete(message: null, error: false)]
	[Example(boolean: true, anEnum: ExampleEnum.Buz, String1 = ""foo"", Integer = 1)]
	public void Single() {
		inner.Single();
	}
	public override bool Equals(Object obj) {
		return inner.Equals(obj);
	}
	public override int GetHashCode() {
		return inner.GetHashCode();
	}
	public override string ToString() {
		return inner.ToString();
	}
}

}");
    }

    [TestMethod]
    public void EventTest()
    {
        DoTest(typeof(EventExample), [typeof(ExampleEventArgs)], @"
using SystemWrapper2;
using WrapGeneratorTest.TestClasses;
using System;
namespace Wrapped.TestClasses {
public interface IEventExampleWrap {
	EventExample WrappedEventExample { get; }
	public event EventHandler SimpleEvent; 
	public event EventHandler<EventArgs> WithArgs; 
	public event EventHandler<IExampleEventArgsWrap> WithCustomArgs; 
}

public class EventExampleWrap : IEventExampleWrap {
	private readonly EventExample inner;
	public EventExample WrappedEventExample => inner;
	public EventExampleWrap(EventExample inner) {
		this.inner = inner;
	}
	public EventExampleWrap()
		: this(new EventExample()) { }


	public event EventHandler SimpleEvent { 
		add { inner.SimpleEvent += value; }
		remove { inner.SimpleEvent -= value; }
	}
	public event EventHandler<EventArgs> WithArgs { 
		add { inner.WithArgs += value; }
		remove { inner.WithArgs -= value; }
	}
	public event EventHandler<IExampleEventArgsWrap> WithCustomArgs { 
		add { inner.WithCustomArgs += value; }
		remove { inner.WithCustomArgs -= value; }
	}
	public override bool Equals(Object obj) {
		return inner.Equals(obj);
	}
	public override int GetHashCode() {
		return inner.GetHashCode();
	}
	public override string ToString() {
		return inner.ToString();
	}
}

}");
    }


    [TestMethod]
    public void StreamTest()
    {
        ClassToWrap wrap = new(typeof(FileStream), wrapNs);
        registrar.Register(wrap);
        registrar.Register(new ClassToWrap(typeof(Stream), wrapNs));
        TypeFactory factory = new(registrar);
        SingleClassSourceGenerator generator = new(factory, wrap);
        string code = generator.GeneratorSource();
        Console.WriteLine(code);
    }

    [TestMethod]
    public void FileSystemWatcherTest()
    {
        ClassToWrap wrap = new(typeof(FileSystemWatcher), wrapNs);
        registrar.Register(wrap);
        TypeFactory factory = new(registrar);
        SingleClassSourceGenerator generator = new(factory, wrap);
        string code = generator.GeneratorSource();
        Console.WriteLine(code);
    }
}