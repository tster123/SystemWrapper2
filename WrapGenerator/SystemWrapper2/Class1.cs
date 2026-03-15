
using Wrapped.System.IO;

namespace SystemWrapper2;

public class Class1
{
    public void Main()
    {
        FileSystemWatcher w = new FileSystemWatcher();
        FileInfoWrap wrap = new FileInfoWrap(new FileInfo("foo"));
    }
}

