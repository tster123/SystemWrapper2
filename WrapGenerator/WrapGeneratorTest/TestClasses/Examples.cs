using System;
using System.Collections.Generic;
using System.Text;

namespace WrapGeneratorTest.TestClasses;

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