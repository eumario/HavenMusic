using Godot;
using System;
using System.Linq;
using HavenMusic.Library;
using HavenMusic.Library.Models;
using HavenMusic.Library.Resources;
using HavenMusic.UI;

[SceneTree(root: "Tree")]
public partial class CurrentlyPlayingPanel : PanelContainer
{
    public override partial void _Ready();
    public MediaPlayer? Player;
    
    private bool _animatingPlayQueue = false;
    private bool _isSeeking = false;
    private bool _waitingSeek = false;
    private bool _wasPlaying = false;
    
    [GodotOverride]
    public async void OnReady()
    {
        while (MainWindow.Instance == null)
            await this.ProcessFrame();
        
        Player = MainWindow.Instance.Player;
        
        Player.SongChanged += (Song? song) =>
        {
            if (song == null)
            {
                AlbumBackground.Texture = ArtTextures.NoAlbumArtPng.Load();
                AlbumIcon.Texture = ArtTextures.NoAlbumArtPng.Load();
                SongTitle.Text = "Not Playing";
                SongArtist.Text = "Unknown";
                SongAlbum.Text = "Unknown";
                TimeProgress.Value = 0;
                TimeProgress.MaxValue = 0;
                CurrentTime.Text = "0:00";
                TotalTime.Text = "0:00";
                PlayButton.Set("icon_name", "circle-play");
                return;
            }
            AlbumBackground.Texture = song.Album?.Artwork?.Texture ?? ArtTextures.NoAlbumArtPng.Load();
            AlbumIcon.Texture = song.Album?.Artwork?.Texture ?? ArtTextures.NoAlbumArtPng.Load();
            SongTitle.Text = song.Title;
            SongAlbum.Text = song.Album?.Title ?? "Unknown Album";
            SongArtist.Text = string.Join(", ", song.Artists.Select(x => x.Name));
            TimeProgress.MaxValue = song.Length;
            CurrentTime.Text = "0:00";
            TimeProgress.Value = 0;
            TotalTime.Text = TimeSpan.FromSeconds(song.Length).ToDisplayTime();
            PlayButton.Set("icon_name", "circle-pause");
        };
        
        Player.PlaybackPositionChanged += (pos) =>
        {
            CurrentTime.Text = TimeSpan.FromSeconds(pos).ToDisplayTime();
            if (!_isSeeking)
                TimeProgress.Value = pos;
        };
        
        Player.PlaybackPaused += () => Spectrum.Paused = Player.IsPaused();
        Player.PlaybackStopped += () => Spectrum.Paused = true;
        Player.PlaybackStarted += () => Spectrum.Paused = false;
        
        
    }
}
