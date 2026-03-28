using System.Runtime.Versioning;
// ReSharper disable UnusedMember.Global
// unused because it's all used with reflection
// ReSharper disable UnusedParameter.Local

#pragma warning disable CS0067 // Event is never used

namespace WrapGeneratorTest.TestClasses;

public enum ExampleEnum
{
    Foo, Bar, Baz, Buz
}
public class ExampleAttribute : Attribute
{
    public ExampleEnum AnEnum { get; set; }
    public bool Boolean { get; set; }
    public string String1 { get; set; }
    public int Integer { get; set; }
    public int? AnotherInt { get; set; }
    public string? AnotherString { get; set; }

    public ExampleAttribute(bool boolean, ExampleEnum anEnum, string string1 = "foo", int integer = 1)
    {
        Boolean = boolean;
        AnEnum  = anEnum;
        String1 = string1;
        Integer = integer;
    }
}

public class AttributesExample
{
    [Obsolete]
    [Example(true, ExampleEnum.Buz)]
    public void Single()
    {

    }

    [SupportedOSPlatform(platformName: "windows")]
    [UnsupportedOSPlatform(platformName: "linux")]
    [Obsolete]
    [Example(true, ExampleEnum.Bar, integer: 42, AnotherString = "Foo")]
    public void Double()
    {

    }

    [Obsolete("This API supports obsolete formatter-based serialization. It should not be called or extended by application code.", DiagnosticId = "SYSLIB0051", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
    public void OldMethod()
    {

    }
}

public class CtorExample
{
    public CtorExample(string foo)
    {

    }

    public CtorExample(int a, string b, OutExample c)
    {

    }
}

public class OutExample
{
    public void Mork(out int a, out PropertyExample p)
    {
        a = 1;
        p = new PropertyExample();
    }
}

public class WrappingExample
{
    public PropertyExample? Prop { get; set; }

    public PropertyExample[] MakeProperties(PropertyExample[] props)
    {
        return [];
    }
}

public class ArraySegmentExample
{
    public void Foo(out ArraySegment<byte> foo)
    {
        foo = new ArraySegment<byte>();
    }
}

public class DisposableExample : IDisposable
{
    public void Dispose()
    {
    }
}

public class PropertyExample
{
    public int Foo { get; set; }
}

public class ListExample
{
    public List<string> Method(List<int> foo)
    {
        return new List<string>();
    }
}


public class ArrayExample
{
    public string[] Method(int[] foo)
    {
        return ["foo"];
    }
}

public class NullableExample
{
    public byte? Method(int? foo)
    {
        return null;
    }
}

public class GenericExample<T1>
{
    public T1? Method<T2>(T2 foo)
    {
        return default(T1);
    }
}

public interface IFoo : IBar
{
    void Foo();
}

public interface IBar
{
    void Bar();
}

public class InterfaceExample : IFoo
{
    public int Mork { get; set; }

    public void Bar()
    {
    }

    public void Foo()
    {
    }
}

public class EventExample
{
    public event EventHandler? SimpleEvent;

    public event EventHandler<EventArgs>? WithArgs;

    public event EventHandler<ExampleEventArgs>? WithCustomArgs;
}

public class ExampleEventArgs : EventArgs
{
    public int Foo { get; }
    public ExampleEventArgs(int foo)
    {
        Foo = foo;
    }
}