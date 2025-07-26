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

var entries = new List<Entry>();
var taskQueue = new Queue<Task>();


await GetEntries(directory);

var collisions = new Dictionary<string, string[]>();

foreach (var entry in entries)
{
    if (!collisions.TryAdd(entry.Hash, [entry.Path]))
    {
        var c = collisions[entry.Hash];
        collisions[entry.Hash] = [..c, entry.Path];
    }
}

Console.WriteLine("");

if (collisions.All(c => c.Value.Length == 1))
{
    Console.WriteLine("No collisions!");
    return;
}

foreach (var (_, songs) in collisions)
{
    if (songs.Length > 1)
    {
        AutoDelete(songs);
        //PromptDelete(songs);
    }
}

return;

void PromptDelete(string[] songs)
{
    Console.WriteLine("COLLISION DETECTED");
        
    for (var i = 0; i < songs.Length; i++)
        Console.WriteLine($"{i}: {Path.GetRelativePath(directory.FullName, songs[i])}");
        
    Console.Write("Select a number to keep or press s to skip: ");
        
    var key = Console.ReadKey();
    if (key.Key == ConsoleKey.S) ;
        
    if (int.TryParse(key.KeyChar.ToString(), out var keepIndex) && keepIndex >= 0 && keepIndex < songs.Length)
    {
        for (var i = 0; i < songs.Length; i++)
        {
            if (i != keepIndex)
                File.Delete(songs[i]);
        }
    }
        
    Console.WriteLine("");
}

void AutoDelete(string[] songs)
{
    Console.WriteLine("COLLISION DETECTED");
        
    for (var i = 0; i < songs.Length; i++)
        Console.WriteLine($"{i}: {Path.GetRelativePath(directory.FullName, songs[i])}");

    for (var i = 1; i < songs.Length; i++)
        File.Delete(songs[i]);
    
    Console.WriteLine($"Kept {songs[0]}...\r\n");
}
async Task GetEntries(DirectoryInfo directoryInfo)
{
    // Breadth first search
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
                entries.Add(new Entry(fileInfo.FullName, Convert.ToBase64String(hash)));
                Console.WriteLine($"{p}: {Convert.ToBase64String(hash)}");
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
            await GetEntries(dirInfo);
        }
        catch (UnauthorizedAccessException e)
        {
            Console.WriteLine($"Access denied for dir: {Path.GetRelativePath(directory.FullName, dirInfo.FullName)}");
        }
    }
    
    while (taskQueue.Count > 0)
        await taskQueue.Dequeue().ConfigureAwait(false);
}
file sealed record Entry(string Path, string Hash);