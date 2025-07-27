// See https://aka.ms/new-console-template for more information

using System.Security.Cryptography;
using TagLib;
using File = System.IO.File;

Console.Write("Path: ");
var path = Console.ReadLine();

if (!Directory.Exists(path))
{
    Console.WriteLine("Invalid path");
    return;
}

var directory = new DirectoryInfo(path);

await Organize(directory);

return;

async Task MoveSong(TagLib.File file, string path)
{
    var title = file.Tag.Title;
    if (string.IsNullOrWhiteSpace(title) || title.Contains('/'))
        title = Path.GetFileNameWithoutExtension(path);
    
    var artists = ((IEnumerable<string>)[..file.Tag.AlbumArtists,..file.Tag.Performers]).Distinct().ToArray();
    var album = file.Tag.Album;

    if (artists.Length <= 0) return;
    
    var artist = artists[0];
    var artistDir = directory.CreateSubdirectory(artist);

    var dest = string.Empty;
    
    if (album != null)
    {
        var albumDir = artistDir.CreateSubdirectory(album);
        dest = $"{directory.FullName}/{artist}/{album}/{title}.mp3";
    }
    else
        dest = $"{directory.FullName}/{artist}/{title}.mp3";
    
    await MoveItem(path, dest);
}

async Task<bool> MoveItem(string sourcePath, string destPath)
{
    if (Path.GetDirectoryName(sourcePath) == Path.GetDirectoryName(destPath)) return false;

    try
    {
        if (File.Exists(destPath))
        {
            await using var src = File.OpenRead(sourcePath);
            await using var dst = File.OpenRead(destPath);
            var srcHash = await MD5.HashDataAsync(src);
            var dstHash = await MD5.HashDataAsync(dst);

            if (srcHash.SequenceEqual(dstHash))
                File.Delete(sourcePath);
            else
            {
                Console.WriteLine($"File {sourcePath} conflicts with {destPath}\r\n");
                var name = Path.GetFileNameWithoutExtension(destPath);
                var dir = Path.GetDirectoryName(destPath);
                var ext = Path.GetExtension(destPath);
                
                await MoveItem(sourcePath, $"{dir}/{name} - alt{ext}");
            }
        }
        else
            File.Move(sourcePath, destPath);
        return true;
    }
    catch (IOException exception)
    {
        Console.WriteLine(exception.Message);
    }
    return false;
}

async Task Organize(DirectoryInfo directoryInfo)
{
    foreach (var fileInfo in directoryInfo.GetFiles())
    {
        if (fileInfo.Extension != ".mp3") continue;

        try
        {
            var tf = TagLib.File.Create(fileInfo.FullName);
        
            await MoveSong(tf, fileInfo.FullName);
        }
        catch (CorruptFileException e)
        {
            
        }
    }

    foreach (var dirInfo in directoryInfo.GetDirectories())
    {
        try
        {
            await Organize(dirInfo);
        }
        catch (UnauthorizedAccessException e)
        {
            Console.WriteLine($"Access denied for dir: {Path.GetRelativePath(directory.FullName, dirInfo.FullName)}");
        }
    }
}