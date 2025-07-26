using System.Security.Cryptography;
using System.Text;

Console.Write("Path: ");
var path = Console.ReadLine();

if (!Directory.Exists(path))
{
    Console.WriteLine("Invalid path");
    return;
}

var directory = new DirectoryInfo(path);

var taskQueue = new Queue<Task>();

var collisions = new HashSet<string>();

await Deduplicate(directory);

return;

async Task Deduplicate(DirectoryInfo directoryInfo)
{
    foreach (var fileInfo in directoryInfo.GetFiles())
    {
        if (Environment.ProcessorCount == taskQueue.Count)
            await taskQueue.Dequeue().ConfigureAwait(false);
        
        taskQueue.Enqueue(Task.Run(async () =>
        {
            var p = Path.GetRelativePath(directory.FullName, fileInfo.FullName);
            try
            {
                await using var file = fileInfo.OpenRead();
                var hash = await MD5.HashDataAsync(file);
                
                if (!collisions.Add(Convert.ToBase64String(hash)))
                {
                    File.Delete(fileInfo.FullName);
                    Console.WriteLine($"Automatically removed {p}");
                }
            }
            catch (UnauthorizedAccessException e)
            {
                Console.WriteLine($"Access denied for file: {p}");
            }
        }));
    }

    foreach (var dirInfo in directoryInfo.GetDirectories())
    {
        try
        {
            await Deduplicate(dirInfo);
        }
        catch (UnauthorizedAccessException e)
        {
            Console.WriteLine($"Access denied for dir: {Path.GetRelativePath(directory.FullName, dirInfo.FullName)}");
        }
    }
    
    while (taskQueue.Count > 0)
        await taskQueue.Dequeue().ConfigureAwait(false);
}