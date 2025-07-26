// See https://aka.ms/new-console-template for more information

Console.Write("Path: ");
var path = Console.ReadLine();

if (!Directory.Exists(path))
{
    Console.WriteLine("Invalid path");
    return;
}

var directory = new DirectoryInfo(path);

await GetFileCount(directory);
async Task<int> GetFileCount(DirectoryInfo directoryInfo)
{
    var count = directoryInfo.GetFiles().Length;

    foreach (var dirInfo in directoryInfo.GetDirectories())
    {
        try
        {
            count += await GetFileCount(dirInfo);
        }
        catch (UnauthorizedAccessException e)
        {
            Console.WriteLine($"Access denied for dir: {Path.GetRelativePath(directory.FullName, dirInfo.FullName)}");
        }
    }

    if (count == 0)
    {
        Directory.Delete(directoryInfo.FullName);
        Console.WriteLine($"Deleted {directoryInfo.FullName}");
    }
    
    return count;
}
