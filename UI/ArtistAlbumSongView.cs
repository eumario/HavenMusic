using System;
using System.Linq;
using Godot;
using HavenMusic.Library;
using HavenMusic.Library.Models;
using HavenMusic.Library.Resources;

namespace HavenMusic.UI;

[SceneTree(root: "Tree")]
public partial class ArtistAlbumSongView : PanelContainer
{
    public MediaPlayer Player;
    private TreeItem? _albumRoot = null!;
    private TreeItem? _songRoot = null!;
    
    private Artist? _artist = null!;

    public Artist? Artist
    {
        get => _artist;
        set
        {
            _artist = value;
            if (ArtistImage == null) return;
            ArtistImage.Texture = value?.Artwork?.Texture ?? ArtTextures.NoArtistArtPng.Load();
            ArtistName.Text = value?.Name ?? "Unknown Artist";
            PopulateAlbums();
            PopulateSongs();
        }
    }
    
    public override partial void _Ready();
    
    [GodotOverride]
    public void OnReady()
    {
        AlbumList.SetColumnTitle(0, "Album Name");
        AlbumList.SetColumnTitle(1, "Artists");
        AlbumList.SetColumnTitle(2, "Song Count");
        AlbumList.SetColumnTitle(3, "Album Length");
        AlbumList.SetColumnExpand(0,true);
        AlbumList.SetColumnExpand(1,true);
        AlbumList.SetColumnExpand(2,false);
        AlbumList.SetColumnExpand(3,false);
        
        SongList.SetColumnTitle(0, "Song Title");
        SongList.SetColumnTitle(1, "Album Name");
        SongList.SetColumnTitle(2, "Song Length");
        SongList.SetColumnExpand(0,true);
        SongList.SetColumnExpand(1,true);
        SongList.SetColumnExpand(2,false);

        QueueArtist.Pressed += () =>
        {
            if (Artist == null) return;
            
            PlayerQueue.Instance.QueueSongs(Artist.Songs.ToList());
            var win = GetTree().Root.FindChild<MainWindow>();
            if (win == null) return;
            if (win.Player.IsPlaying()) return;
            win.Player.CurrentSong = PlayerQueue.Instance.CurrentSong;
            win.Player.Play();
        };

        SongList.ItemActivated += () =>
        {
            var item = SongList.GetSelected();
            if (item == null) return;
            var song = (Song)item.GetMetadata(0);
            GLogger.Debug($"Current Song: {song.Artists.First().Name} - {song.Title} - {song.FilePath}");
            PlayerQueue.Instance.QueueSong(song);
            if (!Player.IsPlaying() && !Player.IsPaused())
            {
                song = PlayerQueue.Instance.CurrentSong;
                Player.CurrentSong = song;
                Player.Play();
            }
        };

        AlbumList.ItemActivated += () =>
        {
            var item = AlbumList.GetSelected();
            if (item == null) return;
            var album = (Album)item.GetMetadata(0);

            MainWindow.Instance.UpdateHistory(new MainWindow.HistoryItem(MainWindow.MainView.AlbumSongListView, album));
        };
    }

    private void PopulateAlbums()
    {
        AlbumList.Clear();
        if (Artist == null) return;
        _albumRoot = AlbumList.CreateItem();

        foreach (var album in Artist.Albums.OrderBy(x => x.Title))
        {
            var item = _albumRoot.CreateChild();
            item.SetText(0, album.Title);
            item.SetText(1, string.Join(", ", album.Artists.Select(x => x.Name)));
            item.SetText(2, $"{album.Songs.Count}");
            item.SetText(3, TimeSpan.FromSeconds(album.Songs.Select(x => x.Length).Sum()).ToDisplayTime());
            item.SetMetadata(0, album);
        }
    }

    private void PopulateSongs()
    {
        SongList.Clear();
        if (Artist == null) return;
        _songRoot = SongList.CreateItem();

        foreach (var song in Artist.Songs.OrderBy(x => x.Album.Title).ThenBy(x => x.Title))
        {
            var item = _songRoot.CreateChild();
            item.SetText(0, song.Title);
            item.SetText(1, song.Album?.Title ?? "Unknown Album");
            item.SetText(2, TimeSpan.FromSeconds(song.Length).ToDisplayTime());
            item.SetMetadata(0, song);
        }
    }
}
