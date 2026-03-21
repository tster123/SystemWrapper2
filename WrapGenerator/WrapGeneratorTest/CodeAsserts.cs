using System.Text;

namespace WrapGeneratorTest;

internal class CodeAsserts
{
    public static void AreEqual(string actual, string expected)
    {
        Assert.AreEqual(CleanString(expected), CleanString(actual));
    }

    private static string CleanString(string v)
    {
        StringBuilder b = new();
        foreach (char c in v)
        {
            if (c == '\r') continue;
            else if (c == '\t') b.Append("    ");
            else b.Append(c);
        }

        return b.ToString().Trim('\n', '\r', '\t', ' ');
    }
}
