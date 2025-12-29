using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using HavenMusic.Library;
using HavenMusic.Library.Models;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Png;
using SpotifyAPI.Web;
using TagLib;

namespace HavenMusic.UI;

[SceneTree(root: "Tree")]
public partial class FileScannerDialog : PanelContainer
{
    private readonly string[] _supportedFormats = [
        ".3ga", ".669", ".a52", ".aac", ".ac3", ".adt", ".adts",
        ".aif", ".aifc", ".aiff", ".amb", ".amr", ".aob", ".ape",
        ".au", ".awb", ".caf", ".dts", ".flac", ".it", ".kar",
        ".m4a", ".m4b", ".m5p", ".mid", ".mka", ".mlp", ".mod",
        ".mpa", ".mp1", ".mp2", ".mp3", ".mpc", ".mpga", ".mus",
        ".mpc", ".mpga", ".mus", ".oga", ".ogg", ".oma", ".opus",
        ".qcp", ".ra", ".rmi", ".s3m", ".sid", ".spx", ".tak", ".thd",
        ".tta", ".voc", ".vqf", ".w64", ".wav", ".wma", ".wv", ".xa",
        ".xm"
    ];
    
    private List<Song> _songsCache = [];
    private List<Album> _albumsCache = [];
    private List<Artist> _artistsCache = [];
    private List<Artwork> _artworksCache = [];
    private SpotifyClient _client;
    

    public Database Database;
    
    public override partial void _Ready();
    
    [GodotOverride]
    public void OnReady()
    {
        Visible = false;
        var config = SpotifyClientConfig
            .CreateDefault()
            .WithAuthenticator(new ClientCredentialsAuthenticator(Secrets.ClientId, Secrets.ClientSecret));
        _client = new SpotifyClient(config);
    }
    
    public async Task ScanFolder(string dir, bool recursive = false)
    {
        GodotThreading.RunInMainThread(() =>
        {
            Visible = true;
            Folder.Text = dir;
        });
        var files = DirAccess.GetFilesAt(dir)
            .Where(x => _supportedFormats.Contains(new FileInfo(x).Extension.ToLower())).ToList();
        
        foreach (var file in files)
        {
            GodotThreading.RunInMainThread(() => File.Text = file);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            
            var path = dir.PathJoin(file);
            if (Database.Songs.Any(x => x.FilePath == path) || _songsCache.Any(x => x.FilePath == path))
                continue;

            try
            {
                // Load up TagFile
                using var tagFile = TagLib.File.Create(path);
                var song = new Song();
                song.Title = tagFile.Tag.Title ?? path.GetBaseName().GetFile();
                song.FilePath = path;
                song.Length = (float)tagFile.Properties.Duration.TotalSeconds;
                
                // Setup Artists

                var artists = new List<string>();

                foreach (var artist in tagFile.Tag.AlbumArtists)
                {
                    if (artist.Contains(", "))
                        artists.AddRange(artist.Split(", ",
                            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
                    else
                        artists.Add(artist);
                }

                foreach (var artist in tagFile.Tag.Performers)
                {
                    if (artist.Contains(", "))
                        artists.AddRange(artist.Split(", ",
                            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
                    else
                        artists.Add(artist);
                }

                if (artists.Count > 0)
                {
                    foreach (var artist in artists)
                    {
                        var dbArtist = Database.Artists.FirstOrDefault(x => x.Name == artist) ??
                                       _artistsCache.FirstOrDefault(x => x.Name == artist);
                        if (dbArtist == null)
                        {
                            dbArtist = new Artist();
                            dbArtist.Name = artist;
                            Database.Artists.Add(dbArtist);
                            _artistsCache.Add(dbArtist);
                            await LookupArtist(dbArtist);
                            await ToSignal(GetTree().CreateTimer(0.2), Timer.SignalName.Timeout);
                        }
                        song.Artists.Add(dbArtist);
                    }
                }
                else
                {
                    var dbArtist = Database.Artists.FirstOrDefault(x => x.Name == "Unknown Artist") ??
                                   _artistsCache.FirstOrDefault(x => x.Name == "Unknown Artist");
                    if (dbArtist == null)
                    {
                        dbArtist = new Artist();
                        dbArtist.Name = "Unknown Artist";
                        Database.Artists.Add(dbArtist);
                        _artistsCache.Add(dbArtist);
                    }
                    song.Artists.Add(dbArtist);
                }

                // Attempt to load Artwork
                Artwork? artwork = null!;

                var picture = tagFile.Tag.Pictures.FirstOrDefault();

                if (picture != null)
                {
                    byte[] buffer;
                    if (picture.MimeType == "image/png")
                        buffer = picture.Data.ToArray();
                    else
                    {
                        var sharpImage = SixLabors.ImageSharp.Image.Load(picture.Data.ToArray());
                        var memStream = new MemoryStream();
                        await sharpImage.SaveAsPngAsync(memStream);
                        buffer = memStream.ToArray();
                    }

                    var hash = buffer.Sha512Hash().HashToStr();
                    artwork = Database.Artworks.FirstOrDefault(x => x.Hash == hash) ??
                              _artworksCache.FirstOrDefault(x => x.Hash == hash);
                    if (artwork == null)
                    {
                        artwork = new Artwork();
                        artwork.Hash = hash;
                        artwork.ImagePath = $"user://cache/album_art/{Guid.NewGuid()}.png".GlobalizePath();
                        await System.IO.File.WriteAllBytesAsync(artwork.ImagePath, buffer);
                        Database.Artworks.Add(artwork);
                        _artworksCache.Add(artwork);
                    }
                }

                // Setup Album
                var album = Database.Albums.FirstOrDefault(x => x.Title == (tagFile.Tag.Album ?? "Unknown Album")) ??
                            _albumsCache.FirstOrDefault(x => x.Title == (tagFile.Tag.Album ?? "Unknown Album"));
                if (album == null)
                {
                    album = new Album();
                    album.Title = tagFile.Tag.Album ?? "Unknown Album";
                    if (artwork != null)
                        album.Artwork = artwork;
                    
                    foreach (var artist in song.Artists)
                        album.Artists.Add(artist);
                    Database.Albums.Add(album);
                    _albumsCache.Add(album);
                }
                
                song.Album = album;
                if (artwork != null)
                    song.Artwork = artwork;
                
                // Save the song
                Database.Songs.Add(song);
                _songsCache.Add(song);
            }
            catch (UnsupportedFormatException)
            {
                // Handle No tag information present
            }
        }
        
        foreach(var rdir in DirAccess.GetDirectoriesAt(dir))
            await ScanFolder(dir.PathJoin(rdir), true);

        if (!recursive)
        {
            await Database.SaveChangesAsync();
            // Clean up the Caches from our scanning.
            Database.ChangeTracker.Clear();
            _albumsCache.Clear();
            _artistsCache.Clear();
            _artworksCache.Clear();
            _songsCache.Clear();
            GodotThreading.RunInMainThread(() => Visible = false);
        }
    }

    private async Task LookupArtist(Artist artist)
    {
        var req = new SearchRequest(SearchRequest.Types.Artist, artist.Name);
        var res = await _client.Search.Item(req);
        var spotifyArtist = res.Artists.Items?.FirstOrDefault();
        if (spotifyArtist == null)
            return;
        var image = spotifyArtist.Images.FirstOrDefault();
        if (image == null)
            return;
        using var httpClient = new System.Net.Http.HttpClient();
        try
        {
            var data = await httpClient.GetByteArrayAsync(image.Url);
            var imgFormat = SixLabors.ImageSharp.Image.DetectFormat(data);
            if (imgFormat.DefaultMimeType != "image/png")
            {
                var sharpImg = SixLabors.ImageSharp.Image.Load(data);
                var memStream = new MemoryStream();
                await sharpImg.SaveAsPngAsync(memStream);
                data = memStream.ToArray();
            }

            var art = new Artwork();
            art.Hash = data.Sha512Hash().HashToStr();
            art.ImagePath = $"user://cache/artist_art/{Guid.NewGuid()}.png".GlobalizePath();
            await System.IO.File.WriteAllBytesAsync(art.ImagePath, data);
            Database.Artworks.Add(art);
            artist.Artwork = art;
        }
        catch (HttpRequestException ex)
        {
            GD.PushError($"Failed to download Image from: {image.Url} for Artist '{artist.Name}'.");
        }
    }
}
