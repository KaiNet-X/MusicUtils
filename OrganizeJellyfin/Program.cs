// See https://aka.ms/new-console-template for more information

using TagLib;
using File = TagLib.File;

Console.Write("Path: ");
var path = Console.ReadLine();

if (!Directory.Exists(path))
{
    Console.WriteLine("Invalid path");
    return;
}

var directory = new DirectoryInfo(path);

var songs = new List<SongInfo>();

await GetEntries(directory);

var artists = new Dictionary<string, Artist>();

foreach (var song in songs)
{
    if (song.Artists.Length == 0) continue;
    
    var firstArtist = song.Artists[0];
    if (!artists.TryGetValue(firstArtist, out var artist))
    {
        artist = new Artist([], []);
        artists.Add(firstArtist, artist);
    }

    if (!string.IsNullOrWhiteSpace(song.Album))
    {
        if (!artist.Albums.TryGetValue(song.Album, out var album))
        {
            album = new Album([]);
            artist.Albums.Add(song.Album, album);
        }
        album.Songs.Add(song);
    }
    else
    {
        artist.Songs.Add(song);
    }
}

foreach (var (name, artist) in artists)
{
    var subDir = directory.CreateSubdirectory(name);

    Console.WriteLine(name);
    foreach (var (aName, album) in artist.Albums)
    {
        subDir.CreateSubdirectory(aName);
        Console.WriteLine($"\t{aName}");
        foreach (var song in album.Songs)
        {
            var dest = $"{directory.FullName}/{name}/{aName}/{song.Title}.mp3";
            try
            {
                System.IO.File.Move(song.Path, dest);
            }
            catch (DirectoryNotFoundException e)
            {
                Console.WriteLine($"Directory not found: ({name}) ({aName}) ({song.Title})");
                Console.WriteLine(dest);
            }
            Console.WriteLine($"\t\t{song.Title}");
        }
    }
    
    foreach (var song in artist.Songs)
    {
        var dest = $"{directory.FullName}/{name}/{song.Title}.mp3";
        try
        {
            System.IO.File.Move(song.Path, dest);
        }
        catch (DirectoryNotFoundException e)
        {
            Console.WriteLine($"Directory not found: ({name}) ({song.Title})");
            Console.WriteLine(dest);
        }
    }
}

void MoveSong(File file, string path)
{
    var title = file.Tag.Title;
    if (string.IsNullOrWhiteSpace(title))
        title = Path.GetFileNameWithoutExtension(path);
    
    var artists = ((IEnumerable<string>)[..file.Tag.AlbumArtists,..file.Tag.Performers]).Distinct().ToArray();
    var album = file.Tag.Album;
    
    if (artists.Length > 0)
    {
        var artist = artists[0];
        var artistDir = directory.CreateSubdirectory(artist);

        if (album != null)
        {
            var albumDir = artistDir.CreateSubdirectory(album);
            
        }
    }
    
}

async Task GetEntries(DirectoryInfo directoryInfo)
{
    foreach (var fileInfo in directoryInfo.GetFiles())
    {
        if (fileInfo.Extension != ".mp3") continue;

        try
        {
            var tf = File.Create(fileInfo.FullName);
        
            var artists = ((IEnumerable<string>)[..tf.Tag.AlbumArtists,..tf.Tag.Performers]).Distinct().ToArray();
            var title = tf.Tag.Title;
            
            if (string.IsNullOrWhiteSpace(title))
                title = Path.GetFileNameWithoutExtension(fileInfo.FullName);
            
            songs.Add(new SongInfo(fileInfo.FullName, title, artists, tf.Tag.Album));
        }
        catch (CorruptFileException e)
        {
            
        }
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
}

file sealed record Album(List<SongInfo> Songs);
file sealed record Artist(List<SongInfo> Songs, Dictionary<string, Album> Albums);
file sealed record SongInfo(string Path, string Title, string[] Artists, string Album);