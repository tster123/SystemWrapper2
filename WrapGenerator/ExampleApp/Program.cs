using Wrapped.System.IO;

namespace ExampleApp;

internal class Program
{
    static void Main(string[] args)
    {
        if (args.Length != 1)
        {
            Console.WriteLine("ExampleApp.exe <example folder>");
        }
        DirectoryWrap dir = new DirectoryWrap();
        Console.WriteLine("Dir created at : " + dir.GetCreationTime(args[0]));

        DirectoryInfoWrap di = new DirectoryInfoWrap(args[0]);
        foreach (IFileSystemInfoWrap fi in di.EnumerateFileSystemInfos())
        {
            if (fi is IDirectoryInfoWrap d)
            {
                Console.WriteLine("Found dir: " + d.FullName);
            }
            else if (fi is IFileInfoWrap f)
            {
                Console.WriteLine("Found file: " + f.Name);
            }
            else
            {
                Console.WriteLine($"Shouldn't have found {fi.GetType()} at {fi.FullName}");
            }
        }
    }
}
