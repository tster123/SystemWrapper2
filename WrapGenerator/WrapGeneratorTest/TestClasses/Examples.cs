using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;

namespace WrapGeneratorTest.TestClasses;

public class ArraySegmentExample
{
    public void Foo(out ArraySegment<byte> foo)
    {
        foo = new ArraySegment<byte>();
    }
}

public class WrappingExample
{
    public PropertyExample Prop { get; set; }

    public PropertyExample MakeProperty(PropertyExample p1)
    {
        return new PropertyExample();
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

public interface IFoo
{
}

public class InterfaceExample : IFoo
{
    public int Mork { get; set; }
}