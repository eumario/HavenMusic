using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using HavenMusic.Library;
using HavenMusic.Library.Models;

namespace HavenMusic.UI;

[SceneTree(root: "Tree")]
public partial class MainWindow : Control
{
    public static MainWindow Instance { get; private set; }
    #region Internal Variables
    private readonly Database _database;

    public enum MainView
    {
        AlbumView,                  // AlbumView
        ArtistView,                 // AlbumView
        SongListView,               // PlaylistView
        AlbumSongListView,          // PlaylistView
        ArtistSongListView,         // PlaylistView
        PlaylistView,               // PlaylistView
        QueueListView,              // PlaylistView
    }

    enum LoopMode
    {
        None,
        Queue,
        Song,
    }

    public record struct HistoryItem(MainView View, object? Item = null, int ScrollPos = -1, int ScrollPos2 = -1);

    private readonly List<HistoryItem> _history = [];
    private readonly List<HistoryItem> _backHistory = [];
    private Vector2 _playListQueueShown;
    private Vector2 _playListQueueHidden;

    private bool _animatingPlayQueue = false;
    private bool _isSeeking = false;
    private bool _waitingSeek = false;
    private bool _wasPlaying = false;
    private LoopMode _loopMode = LoopMode.None;
    private TreeItem? _playlistRoot;

    private MprisService _mprisService;
    #endregion

    #region ctor
    public MainWindow()
    {
        _database = Database.InitDatabase("user://library.db".GlobalizePath());
    }
    #endregion
    
    #region Godot Overrides
    public override partial void _Ready();

    [GodotOverride]
    public void OnReady()
    {
        GodotThreading.EstablishMainThread();
        if (!DirAccess.DirExistsAbsolute("user://cache"))
            DirAccess.MakeDirAbsolute("user://cache");
        if (!DirAccess.DirExistsAbsolute("user://cache/album_art"))
            DirAccess.MakeDirAbsolute("user://cache/album_art");
        if (!DirAccess.DirExistsAbsolute("user://cache/artist_art"))
            DirAccess.MakeDirAbsolute("user://cache/artist_art");
        Instance = this;
        FileScannerDialog.Database = _database;
        BusyOverlay.Visible = false;
        CurrentPlaying.Visible = false;
        BackButton.Visible = false;
        ArtistAlbumSongView.Player = Player;
        RandomButton.Set("color", Colors.DimGray);
        LoopButton.Set("color", Colors.DimGray);
        _history.Add(new HistoryItem(MainView.AlbumView));
        TimeProgress.Value = 0;
        _playListQueueShown = PlayQueueList.Position;
        _playListQueueHidden = new Vector2(1200.0f, PlayQueueList.Position.Y);
        PlayQueueList.Position = _playListQueueHidden;
        PlayQueueList.Visible = false;
        SwitchHistory();

        _mprisService = new MprisService(Player);

        SetupEventHandlers();
        
        GodotThreading.RunInMainThread(async () =>
        {
            await _mprisService.InitializeAsync();
        });
    }
    
    public override partial void _Process(double delta);
    
    [GodotOverride]
    public void OnProcess(double delta)
    {
        if (Input.IsActionJustPressed("ui_go_back")) HandleGoBack();
        if (Input.IsActionJustPressed("ui_go_forward")) HandleGoForward();

        if (Input.IsActionJustPressed("ui_play")) PlayButton.EmitSignalPressed();
        if (Input.IsActionJustPressed("ui_prev")) PrevButton.EmitSignalPressed();
        if (Input.IsActionJustPressed("ui_next")) NextButton.EmitSignalPressed();

        HistoryBackButton.Disabled = _history.Count == 1;
        HistoryForwardButton.Disabled = _backHistory.Count == 0;
        PlayQueueSep.Visible = PlayerQueue.Instance.Count > 0;
        PlayQueueButton.Visible = PlayerQueue.Instance.Count > 0;
        
        if (Input.IsActionJustPressed("ui_refresh"))
            GD.Print("Refresh");
    }

    #endregion
    
    #region Support Functions

    private void SwitchHistory()
    {
        var view = _history[^1];
        switch (view.View)
        {
            case MainView.AlbumView:
                ShowAlbumView();
                break;
            case MainView.ArtistView:
                ShowArtistList();
                break;
            case MainView.SongListView:
                ShowSongList();
                break;
            case MainView.PlaylistView:
                break;
            case MainView.AlbumSongListView:
                if (view.Item != null)
                    ShowAlbumSongs((Album)view.Item);
                break;
            case MainView.ArtistSongListView:
                if (view.Item != null)
                    ShowArtistSongListView((Artist)view.Item);
                break;
            case MainView.QueueListView:
                break;
        }
    }

    public void UpdateHistory(HistoryItem item)
    {
        _history.Add(item);
        _backHistory.Clear();
        SwitchHistory();
    }
    
    #endregion
    
    #region Event Handlers
    private void SetupEventHandlers()
    {
        MenuButton.Pressed += () =>
        {
            var rect = MenuButton.GetRect().ToRect2I();
            rect.Position = new Vector2I(rect.Position.X, rect.Position.Y + rect.Size.Y);
            MainMenu.Popup(rect);
        };

        Albums.Pressed += () =>
        {
            if (_history[^1].View == MainView.AlbumView)
                return;
            UpdateHistory(new HistoryItem(MainView.AlbumView));
        };
        
        Artists.Pressed += () =>
        {
            if (_history[^1].View == MainView.ArtistView)
                return;
            UpdateHistory(new HistoryItem(MainView.ArtistView));
        };

        Songs.Pressed += () =>
        {
            if (_history[^1].View == MainView.SongListView)
                return;
            UpdateHistory(new HistoryItem(MainView.SongListView));
        };

        PlayButton.Pressed += async () =>
        {
            if (Player.CurrentSong == null) return;
            var newIcon = "";
            if (Player.IsPlaying())
            {
                Player.Pause();
                newIcon = "circle-play";
            }
            else
            {
                Player.Resume();
                newIcon = "circle-pause";
            }

            PlayButton.Set("icon_name", newIcon);
        };

        PrevButton.Pressed += () =>
        {
            if (PlayerQueue.Instance.Count == 0) return;
            var song = PlayerQueue.Instance.PrevSong();
            if (song == null) return;
            Player.Stop();
            Player.CurrentSong = song;
            Player.Play();
        };

        NextButton.Pressed += () =>
        {
            if (PlayerQueue.Instance.Count == 0) return;
            var song = PlayerQueue.Instance.NextSong();
            if (song == null) return;
            Player.Stop();
            Player.CurrentSong = song;
            Player.Play();
        };
        
        PlayQueueButton.Pressed += () =>
        {
            if (_animatingPlayQueue) return;
            _animatingPlayQueue = true;
            if (PlayQueueList.Visible)
            {
                // Animate Closing
                var tween = CreateTween();
                tween.TweenProperty(PlayQueueList, "position", _playListQueueHidden, 0.25f);
                tween.TweenCallback(Callable.From(() => PlayQueueList.Visible = false));
                tween.TweenCallback(Callable.From(() => _animatingPlayQueue = false));
            }
            else
            {
                // Animate Opening
                var tween = CreateTween();
                tween.TweenCallback(Callable.From(() => PlayQueueList.Visible = true));
                tween.TweenProperty(PlayQueueList, "position", _playListQueueShown, 0.25f);
                tween.TweenCallback(Callable.From(() => _animatingPlayQueue = false));
            }
        };
        
        HistoryBackButton.Pressed += HandleGoBack;
        HistoryForwardButton.Pressed += HandleGoForward;
        
        MainMenu.IndexPressed += HandleMainMenu;
        MinimizeButton.Pressed += () => GetWindow().Mode = Window.ModeEnum.Minimized;
        CloseButton.Pressed += () => GetTree().Quit();

        LoopButton.Pressed += () =>
        {
            switch (_loopMode)
            {
                case LoopMode.None:
                    _loopMode = LoopMode.Queue;
                    LoopButton.Set("color", Colors.LimeGreen);
                    break;
                case LoopMode.Queue:
                    _loopMode = LoopMode.Song;
                    LoopSingular.Visible = true;
                    break;
                case LoopMode.Song:
                    _loopMode = LoopMode.None;
                    LoopSingular.Visible = false;
                    LoopButton.Set("color", Colors.DimGray);
                    break;
            }
            PlayerQueue.Instance.IsLooping = _loopMode == LoopMode.Queue;
        };
        RandomButton.Pressed += () =>
        {
            if (PlayerQueue.Instance.IsShuffled)
            {
                PlayerQueue.Instance.UnShuffle();
                RandomButton.Set("color", Colors.DimGray);
            }
            else
            {
                PlayerQueue.Instance.Shuffle();
                RandomButton.Set("color", Colors.LimeGreen);
            }
            
            if (Player.CurrentSong == null) return;
            var pos = PlayerQueue.Instance.GetPosition(Player.CurrentSong);
            if (pos == -1) return;
            PlayerQueue.Instance.SetPosition(pos);
        };

        QueuePlaylistSongs.Pressed += () =>
        {
            var songs = new List<Song>();
            foreach (var child in _playlistRoot.GetChildren())
            {
                if (child == null) continue;
                var song = (Song)child.GetMetadata(0);
                songs.Add(song);
            }

            PlayerQueue.Instance.QueueSongs(songs);
            if (!Player.IsPlaying() && !Player.IsPaused())
            {
                var song = PlayerQueue.Instance.CurrentSong;
                Player.CurrentSong = song;
                Player.Play();
            }
        };

        PlaylistView.ItemActivated += HandlePlaylistViewItemActivated;

        Player.SongChanged += async (Song? song) =>
        {
            CurrentPlaying.Visible = song != null;
            if (song == null)
                return;

            CurrentArt.Texture = song.Artwork?.Texture;
            CurrentTitle.Text = song.Title;
            CurrentTime.Text = "0:00";
            PlayButton.Set("icon_name", "circle-pause");
            TimeProgress.MaxValue = song.Length;
            TimeProgress.Value = 0;
            TotalTime.Text = TimeSpan.FromSeconds(song.Length).ToDisplayTime();
        };
        
        Player.PlaybackPositionChanged += async (pos) =>
        {
            CurrentTime.Text = TimeSpan.FromSeconds(pos).ToDisplayTime();
            if (!_isSeeking)
                TimeProgress.Value = pos;
        };

        Player.PlaybackPaused += () => Spectrum.Paused = true;
        Player.PlaybackStopped += () => Spectrum.Paused = true;
        Player.PlaybackStarted += () => Spectrum.Paused = false;
        Player.PlaybackResumed += () => Spectrum.Paused = false;
        
        Player.PlaybackFinished += async () =>
        {
            if (_isSeeking)
            {
                if (_waitingSeek) return;
                GD.Print("Playback finished, but we are still seeking, waiting for seeking to complete...");
                _waitingSeek = true;
                while (_isSeeking)
                    await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                GD.Print("Seeking completed, resuming check to see if we are at end of song.");
                _waitingSeek = false;
                if (Player.GetFullPlaybackPosition() < Player.CurrentSong!.Length)
                {
                    GD.Print("We're not at the end of the song, so we are trying to see if player was playing, and not playing anymore.");
                    if (_wasPlaying && !Player.IsPlaying())
                    {
                        GD.Print("We were playing, and not playing anymore, attempting to play again, and seek to the correct position.");
                        _wasPlaying = false;
                        Player.Play((float)TimeProgress.Value);
                        GD.Print("We have successfully Started playback, and seeked to the correct position.");
                    }
                    return;
                }
            }

            if (_loopMode == LoopMode.Song)
            {
                Player.Play();
                return;
            }
            var song = PlayerQueue.Instance.NextSong();
            if (song == null)
            {
                PlayButton.Set("icon_name", "circle-play");
                CurrentTime.Text = TimeSpan.FromSeconds(Player.CurrentSong.Length).ToDisplayTime();
                TimeProgress.Value = Player.CurrentSong.Length;
            }
            else
            {
                Player.CurrentSong = song;
                Player.Play();
            }
        };

        TimeProgress.DragStarted += () =>
        {
            _isSeeking = true;
            _wasPlaying = Player.IsPlaying();
        };

        TimeProgress.ValueChanged += value =>
        {
            if (!_isSeeking) return;
            Player.Seek((float)value);
        };

        TimeProgress.DragEnded += changed =>
        {
            _isSeeking = false;
        };

        AlbumPlaylistView.GetVScrollBar().Scrolling += () =>
        {
            var item = _history[^1];
            item.ScrollPos = AlbumPlaylistView.ScrollVertical;
            _history[^1] = item;
        };
        
        CurrentPlaying.GuiInput += (inputEvent) =>
        {
            if (inputEvent is not InputEventMouseButton mbEvent) return;
            if (mbEvent.ButtonIndex != MouseButton.Left || !mbEvent.Pressed) return;
            // Handle Showing CurrentlyPlaying Panel
            Header.HideBar();
            Content.Visible = false;
            Controls.Visible = false;
            CurrentlyPlayingPanel.Visible = true;
        };

        BackButton.Pressed += () =>
        {
            Header.ShowBar();
            Content.Visible = true;
            Controls.Visible = true;
            CurrentlyPlayingPanel.Visible = false;
        };

        Globals.Instance.AlbumArtworkUpdated += (song, art) =>
        {
            if (!Player.IsPlaying()) return;
            if (PlayerQueue.Instance.CurrentSong != song) return;
            GodotThreading.RunInMainThread(() =>
            {
                CurrentlyPlayingPanel.AlbumBackground.Texture = art.Texture;
                CurrentlyPlayingPanel.AlbumIcon.Texture = art.Texture;
                CurrentArt.Texture = art.Texture;
            });
        };

    }
    
    private void HandleGoBack()
    {
        if (_history.Count == 1)
            return;
        _backHistory.Add(_history[^1]);
        _history.RemoveAt(_history.Count - 1);
        SwitchHistory();
    }

    private void HandleGoForward()
    {
        if (_backHistory.Count == 0)
            return;
        _history.Add(_backHistory[^1]);
        _backHistory.RemoveAt(_backHistory.Count - 1);
        SwitchHistory();
    }
    
    private void HandleMainMenu(long index)
    {
        switch (index)
        {
            case 0: // Add Files to Library
                break;
            case 1: // Add Folder to Library
                var dlg = new FileDialog();
                dlg.UseNativeDialog = true;
                dlg.CurrentDir = OS.GetSystemDir(OS.SystemDir.Music);
                dlg.FileMode = FileDialog.FileModeEnum.OpenDir;
                dlg.Title = "Select Folder Containing Music...";
                dlg.DirSelected += HandleAddFolder;
                AddChild(dlg);
                dlg.PopupCentered();
                break;
            case 3: // New Playlist
                break;
            case 4: // Add to Playlist
                break;
            case 6: // Settings
                break;
            case 7: // About
                break;
            case 8: // Quit
                GetTree().Quit();
                break;
        }
    }

    private void HandlePlaylistViewItemActivated()
    {
        var item = PlaylistView.GetSelected();
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
    }

    private async void HandleAddFolder(string dir)
    {
        BusyOverlay.Visible = true;
        await FileScannerDialog.ScanFolder(dir);
        BusyOverlay.Visible = false;
        ShowAlbumView();
    }
    #endregion
    
    #region View Display

    private void ShowAlbumView()
    {
        AlbumView.Visible = true;
        PlaylistView.Visible = false;
        AlbumPlaylistView.Visible = true;
        ArtistAlbumSongView.Visible = false;
        AlbumView.QueueFreeChildren();

        foreach (var album in _database.Albums.OrderBy(x => x.Title))
        {
            var card = AlbumChip.Instantiate(album);
            card.AlbumSelected += selectedAlbum =>
            {
                UpdateHistory(new HistoryItem(MainView.AlbumSongListView, selectedAlbum));
                _backHistory.Clear();
                SwitchHistory();
            };
            AlbumView.AddChild(card);
        }

        Callable.From(() =>
        {
            var newPos = _history[^1].ScrollPos;
            Callable.From(() =>
            {
                AlbumPlaylistView.ScrollVertical = newPos;
            }).CallDeferred();
        }).CallDeferred();
    }

    private void ShowAlbumSongs(Album album)
    {
        AlbumView.Visible = false;
        PlaylistView.Visible = true;
        AlbumPlaylistView.Visible = true;
        ArtistAlbumSongView.Visible = false;
        PlaylistView.Columns = 3;
        PlaylistView.SetColumnTitle(0, "Title");
        PlaylistView.SetColumnTitle(1, "Artist");
        PlaylistView.SetColumnTitle(2, "Length");
        PlaylistView.SetColumnExpand(0, true);
        PlaylistView.SetColumnExpand(1, true);
        PlaylistView.SetColumnExpand(2, false);
        PlaylistView.SetColumnCustomMinimumWidth(2, 60);
        PlaylistView.Clear();
        _playlistRoot = PlaylistView.CreateItem();
        foreach (var song in album.Songs.OrderBy(x => x.Title))
        {
            var item = _playlistRoot.CreateChild();
            item.SetText(0, song.Title);
            item.SetText(1, song.Artists.ToList()[0].Name);
            item.SetMetadata(0, song);
            var songLength = TimeSpan.FromSeconds(song.Length);
            if (songLength.Hours > 0)
                item.SetText(2, songLength.ToString(@"hh\:mm\:ss"));
            else
                item.SetText(2, songLength.ToString(@"mm\:ss"));
        }
        
        Callable.From(() =>
        {
            var newPos = _history[^1].ScrollPos;
            Callable.From(() =>
            {
                AlbumPlaylistView.ScrollVertical = newPos;
            }).CallDeferred();
        }).CallDeferred();
    }

    private void ShowSongList()
    {
        AlbumView.Visible = false;
        PlaylistView.Visible = true;
        AlbumPlaylistView.Visible = true;
        ArtistAlbumSongView.Visible = false;
        PlaylistView.Columns = 4;
        PlaylistView.SetColumnTitle(0, "Title");
        PlaylistView.SetColumnTitle(1, "Artist");
        PlaylistView.SetColumnTitle(2, "Album");
        PlaylistView.SetColumnTitle(3, "Length");
        PlaylistView.SetColumnExpand(0, true);
        PlaylistView.SetColumnExpand(1, true);
        PlaylistView.SetColumnExpand(2, true);
        PlaylistView.SetColumnExpand(3, false);
        PlaylistView.SetColumnCustomMinimumWidth(3, 60);
        PlaylistView.Clear();
        _playlistRoot = PlaylistView.CreateItem();
        foreach (var song in _database.Songs.OrderBy(x => x.Album.Title).ThenBy(x => x.Title))
        {
            var item = _playlistRoot.CreateChild();
            item.SetText(0, song.Title);
            item.SetText(1, song.Artists.ToList()[0].Name);
            item.SetText(2, song.Album.Title);
            item.SetMetadata(0, song);
            var songLength = TimeSpan.FromSeconds(song.Length);
            item.SetText(3, songLength.ToDisplayTime());
        }
        
        Callable.From(() =>
        {
            var newPos = _history[^1].ScrollPos;
            Callable.From(() =>
            {
                AlbumPlaylistView.ScrollVertical = newPos;
            }).CallDeferred();
        }).CallDeferred();
    }

    private void ShowArtistList()
    {
        AlbumView.Visible = true;
        PlaylistView.Visible = false;
        AlbumPlaylistView.Visible = true;
        ArtistAlbumSongView.Visible = false;
        AlbumView.QueueFreeChildren();
        foreach (var artist in _database.Artists.OrderBy(x => x.Name))
        {
            var card = ArtistChip.Instantiate(artist);
            card.ArtistSelected += (artist) =>
            {
                UpdateHistory(new HistoryItem(MainView.ArtistSongListView, artist));
                _backHistory.Clear();
                SwitchHistory();
            };
            AlbumView.AddChild(card);
        }
        
        Callable.From(() =>
        {
            var newPos = _history[^1].ScrollPos;
            Callable.From(() =>
            {
                AlbumPlaylistView.ScrollVertical = newPos;
            }).CallDeferred();
        }).CallDeferred();
    }

    private void ShowArtistSongListView(Artist artist)
    {
        AlbumView.Visible = false;
        PlaylistView.Visible = false;
        AlbumPlaylistView.Visible = false;
        ArtistAlbumSongView.Visible = true;
        ArtistAlbumSongView.Artist = artist;
    }
    #endregion

}
