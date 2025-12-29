using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using HavenMusic.Library.Models;
using Tmds.DBus;
using Tmds.DBus.Protocol;

namespace HavenMusic.Library;

public class MprisService : IDisposable
{
    private readonly MediaPlayer _player;

    private Connection? _connection;
    private MprisObject? _mprisObject;
    private bool _isInitialized;
    private bool _disposed;

    private const string BusName = "org.mpris.MediaPlayer2.haven_music";

    public event EventHandler? NextRequested;
    public event EventHandler? PreviousRequested;
    public event EventHandler? RaiseRequested;
    public event EventHandler? QuitRequested;

    public MprisService(MediaPlayer player)
    {
        _player = player ?? throw new ArgumentNullException(nameof(player));
        
        // Register Events
        _player.PlaybackFinished += OnPlaybackStateChanged;
        _player.PlaybackPaused += OnPlaybackStateChanged;
        //_player.PlaybackPositionChanged += OnPlaybackPositionChanged;
        _player.PlaybackResumed += OnPlaybackStateChanged;
        _player.PlaybackStarted += OnPlaybackStateChanged;
        _player.PlaybackStopped += OnPlaybackStateChanged;
        _player.SongChanged += OnSongChanged;

        Globals.Instance.AlbumArtworkUpdated += (song, art) =>
        {
            if (_player.CurrentSong != song) return;
            OnSongChanged(song);
        };
    }

    public async Task InitializeAsync()
    {
        GLogger.Debug("Initializing D-Bus...");
        GLogger.Debug($"Is Linux: {RuntimeInformation.IsOSPlatform(OSPlatform.Linux)}");
        GLogger.Debug($"Already initialized: {_isInitialized}");

        if (_isInitialized || !RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            GLogger.Error($"Skipping initialization of D-Bus...");
            return;
        }

        try
        {
            GLogger.Debug("Creating connection to session bus...");
            _connection = new Connection(Address.Session!);
            await _connection.ConnectAsync();
            GLogger.Debug("Connected to session bus.");

            GLogger.Debug("Creating MPRIS object...");
            _mprisObject = new MprisObject(_player, this);
            GLogger.Debug("MPRIS object created.");

            GLogger.Debug($"Registering object at {MprisObject.Path}...");
            await _connection.RegisterObjectAsync(_mprisObject);
            GLogger.Debug("Object registered.");

            GLogger.Debug($"Registering service name: {BusName}...");
            await _connection.RegisterServiceAsync(BusName);
            GLogger.Debug("Service name registered.");

            _isInitialized = true;
            GLogger.Info($"Service initialized successfully as {BusName}");
        }
        catch (Exception ex)
        {
            GLogger.Error($"Initialization Failed, Message: {ex.Message}");
        }
    }
    
    #region Internal Functions / Event Handlers
    internal void OnNext() => NextRequested?.Invoke(this, EventArgs.Empty);
    internal void OnPrevious() => PreviousRequested?.Invoke(this, EventArgs.Empty);
    internal void OnRaise() => RaiseRequested?.Invoke(this, EventArgs.Empty);
    internal void OnQuit() => QuitRequested?.Invoke(this, EventArgs.Empty);

    private void OnPlaybackStateChanged()
    {
        if (_mprisObject != null && _isInitialized)
        {
            var changes = new Dictionary<string, object>
            {
                ["PlaybackStatus"] = _mprisObject.GetPlaybackStatus()
            };
            _mprisObject.EmitPropertyChanged("org.mpris.MediaPlayer2.Player", changes);
        }
    }

    private void OnSongChanged(Song? song)
    {
        if (_mprisObject != null && _isInitialized)
        {
            var changes = new Dictionary<String, object>
            {
                ["Metadata"] = _mprisObject.GetMetadata()
            };
            _mprisObject.EmitPropertyChanged("org.mpris.MediaPlayer2.Player", changes);
        }
    }
    #endregion
    
    public void Dispose()
    {
        if (_disposed)
            return;
        
        _player.PlaybackFinished -= OnPlaybackStateChanged;
        _player.PlaybackPaused -= OnPlaybackStateChanged;
        //_player.PlaybackPositionChanged -= OnPlaybackPositionChanged;
        _player.PlaybackResumed -= OnPlaybackStateChanged;
        _player.PlaybackStarted -= OnPlaybackStateChanged;
        _player.PlaybackStopped -= OnPlaybackStateChanged;
        _player.SongChanged -= OnSongChanged;

        _connection?.Dispose();
        _disposed = true;
    }
}

// MPRIS Root Interface (org.mpris.MediaPlayer2)
[DBusInterface("org.mpris.MediaPlayer2")]
public interface IMediaPlayer2 : IDBusObject
{
    Task RaiseAsync();
    Task QuitAsync();
    Task<object> GetAsync(string prop);
    Task<IDictionary<string, object>> GetAllAsync();
    Task SetAsync(string prop, object val);
    Task<IDisposable> WatchPropertiesAsync(Action<PropertyChanges> handler);
}

// MPRIS Player Interface (org.mpris.MediaPlayer2.Player)
[DBusInterface("org.mpris.MediaPlayer2.Player")]
public interface IMediaPlayer2Player : IDBusObject
{
    Task PlayAsync();
    Task PauseAsync();
    Task PlayPauseAsync();
    Task StopAsync();
    Task NextAsync();
    Task PreviousAsync();
    Task SeekAsync(long offset);
    Task SetPositionAsync(ObjectPath trackId, long position);
    Task OpenUriAsync(string uri);
    Task<object> GetAsync(string prop);
    Task<IDictionary<string, object>> GetAllAsync();
    Task SetAsync(string prop, object val);
    Task<IDisposable> WatchPropertiesAsync(Action<PropertyChanges> handler);
    
    // Seeked signal
    Task<IDisposable> WatchSeekedAsync(Action<long> handler);
}

// Combined MPRIS object implementing both interfaces
internal class MprisObject : IMediaPlayer2, IMediaPlayer2Player
{
    private readonly MediaPlayer _player;
    private readonly MprisService _service;
    private readonly List<Action<PropertyChanges>> _mediaPlayer2PropertyWatchers = new();
    private readonly List<Action<PropertyChanges>> _playerPropertyWatchers = new();
    private readonly List<Action<long>> _seekedWatchers = new();
    
    // Cache reflected fields for PropertyChanges construction
    private static readonly System.Reflection.FieldInfo? ChangedField;
    private static readonly System.Reflection.FieldInfo? InvalidatedField;

    static MprisObject()
    {
        var propertyChangesType = typeof(PropertyChanges);
        var allFields = propertyChangesType.GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        ChangedField = allFields.FirstOrDefault(f => f.Name == "_changed");
        InvalidatedField = allFields.FirstOrDefault(f => f.Name == "_invalidated");

        if (ChangedField == null || InvalidatedField == null)
        {
            GLogger.Warning("Could not find PropertyChanges backing fields.  Signal emission will not work!");
        }
    }

    public static readonly ObjectPath Path = new ObjectPath("/org/mpris/MediaPlayer2");
    public ObjectPath ObjectPath => Path;

    public MprisObject(MediaPlayer player, MprisService service)
    {
        _player = player ?? throw new ArgumentNullException(nameof(player));
        _service = service ?? throw new ArgumentNullException(nameof(service));
    }

    internal void EmitPropertyChanged(string interfaceName, IDictionary<string, object> changedProperties)
    {
        var watchers = interfaceName switch
        {
            "org.mpris.MediaPlayer2" => _mediaPlayer2PropertyWatchers,
            "org.mpris.MediaPlayer2.Player" => _playerPropertyWatchers,
            _ => null
        };

        if (watchers != null && watchers.Count > 0)
        {
            if (ChangedField == null || InvalidatedField == null)
            {
                GLogger.Debug("Cannot emit signal: PropertyChanges fields not found");
                return;
            }

            try
            {
                var propertyChangesType = typeof(PropertyChanges);

                // Create uninitalized instance - keep boxed for SetValue to work on structs
                object changesObj =
                    System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(propertyChangesType);

                // _changed is KeyValuePair<string, object>[], convert dictionary to array
                var changedArray = changedProperties.Select(kvp => new KeyValuePair<string, object>(kvp.Key, kvp.Value))
                    .ToArray();
                ChangedField.SetValue(changesObj, changedArray);
                InvalidatedField.SetValue(changesObj, Array.Empty<string>());

                // Unbox back to PropertyChanges struct
                var changes = (PropertyChanges)changesObj;

                // Invoke watchers - Tmds.DBus will emit D-Bus PropertiesChanged signal
                foreach (var watcher in watchers.ToArray())
                {
                    try
                    {
                        watcher(changes);
                    }
                    catch (Exception ex)
                    {
                        GLogger.Debug($"Error emitting signal: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                GLogger.Debug($"Error creating PropertyChanges: {ex.Message}");
            }
        }
    }

    // IMediaPlayer2 implementation
    Task IMediaPlayer2.RaiseAsync()
    {
        _service.OnRaise();
        return Task.CompletedTask;
    }

    Task IMediaPlayer2.QuitAsync()
    {
        _service.OnQuit();
        return Task.CompletedTask;
    }

    Task<object> IMediaPlayer2.GetAsync(string prop)
    {
        object value = prop switch
        {
            "Identity" => "HavenMusic",
            "DesktopEntry" => "haven_music",
            "CanQuit" => true,
            "CanRaise" => true,
            "HasTrackList" => false,
            "SupportedUriSchemes" => new string[] { "file" },
            "SupportedMimeTypes" => new string[]
                { "audio/mpeg", "audio/ogg", "audio/flac", "audio/x-flac", "audio/mp4" },
            _ => throw new ArgumentException($"Unknown property: {prop}")
        };
        
        return Task.FromResult(value);
    }

    Task<IDictionary<string, object>> IMediaPlayer2.GetAllAsync()
    {
        var properties = new Dictionary<string, object>
        {
            ["Identity"] = "HavenMusic",
            ["DesktopEntry"] = "haven_music",
            ["CanQuit"] = true,
            ["CanRaise"] = true,
            ["HasTrackList"] = false,
            ["SupportedUriSchemes"] = new string[] { "file" },
            ["SupportedMimeTypes"] = new string[] { "audio/mpeg", "audio/ogg", "audio/flac", "audio/x-flac", "audio/mp4" }
        };
        
        return Task.FromResult<IDictionary<string, object>>(properties);
    }
    
    Task IMediaPlayer2.SetAsync(string prop, object val) => Task.CompletedTask;

    Task<IDisposable> IMediaPlayer2.WatchPropertiesAsync(Action<PropertyChanges> handler)
    {
        _mediaPlayer2PropertyWatchers.Add(handler);
        GLogger.Debug($"MediaPlayer2 property watcher registered (total: {_mediaPlayer2PropertyWatchers.Count})");
        return Task.FromResult<IDisposable>(new PropertyWatcherDisposable(() =>
        {
            _mediaPlayer2PropertyWatchers.Remove(handler);
            GLogger.Debug($"MediaPlayer2 property watcher removed (total: {_mediaPlayer2PropertyWatchers.Count})");
        }));
    }
    
    // IMediaPlayer2Player implementation
    Task IMediaPlayer2Player.PlayAsync()
    {
        GodotThreading.RunInMainThread(() =>
        {
            if (!_player.IsPlaying() && _player.CurrentSong != null)
            {
                if (_player.IsPaused())
                    _player.Resume();
                else
                    _player.Play();
            }
        });
        return Task.CompletedTask;
    }

    Task IMediaPlayer2Player.PauseAsync()
    {
        GodotThreading.RunInMainThread(() =>
        {
            if (_player.IsPlaying())
            {
                if (_player.IsPaused())
                    _player.Resume();
                else
                    _player.Pause();
            }
        });
        return Task.CompletedTask;
    }

    Task IMediaPlayer2Player.PlayPauseAsync()
    {
        GodotThreading.RunInMainThread(() =>
        {
            if (!_player.IsPlaying())
                _player.Play();
            else
            {
                if (_player.IsPaused())
                    _player.Resume();
                else
                    _player.Pause();
            }
        });
        return Task.CompletedTask;
    }

    Task IMediaPlayer2Player.StopAsync()
    {
        GodotThreading.RunInMainThread(() =>
        {
            if (_player.IsPlaying())
                _player.Stop();
        });
        return Task.CompletedTask;
    }

    Task IMediaPlayer2Player.NextAsync()
    {
        GodotThreading.RunInMainThread(() =>
        {
            if (PlayerQueue.Instance.Count == 0) return;
            var song = PlayerQueue.Instance.NextSong();
            if (song == null) return;
            _player.Stop();
            _player.CurrentSong = song;
            _player.Play();
        });
        return Task.CompletedTask;
    }

    Task IMediaPlayer2Player.PreviousAsync()
    {
        GodotThreading.RunInMainThread(() =>
        {
            if (PlayerQueue.Instance.Count == 0) return;
            var song = PlayerQueue.Instance.PrevSong();
            if (song == null) return;
            _player.Stop();
            _player.CurrentSong = song;
            _player.Play();
        });
        return Task.CompletedTask;
    }

    Task IMediaPlayer2Player.SeekAsync(long offset)
    {
        GodotThreading.RunInMainThread(() =>
        {
            var currentPosition = TimeSpan.FromSeconds(_player.GetFullPlaybackPosition());
            var newPosition = currentPosition + TimeSpan.FromMicroseconds(offset);
            if (newPosition >= TimeSpan.Zero)
            {
                _player.Seek((float)newPosition.TotalSeconds);
            }
        });
        return Task.CompletedTask;
    }

    Task IMediaPlayer2Player.SetPositionAsync(ObjectPath trackId, long position)
    {
        GodotThreading.RunInMainThread(() =>
        {
            var newPosition = TimeSpan.FromMicroseconds(position);
            if (newPosition >= TimeSpan.Zero)
            {
                _player.Seek((float)newPosition.TotalSeconds);
            }
        });
        return Task.CompletedTask;
    }

    Task IMediaPlayer2Player.OpenUriAsync(string uri)
    {
        // Not implemented yet.
        GLogger.Debug($"OpenUriAsync called on bus.  Uri: {uri}");
        return Task.CompletedTask;
    }

    Task<object> IMediaPlayer2Player.GetAsync(string prop)
    {
        GLogger.Debug($"GetAsync called for property: {prop}");

        object value = prop switch
        {
            "PlaybackStatus" => GetPlaybackStatus(),
            "LoopStatus" => "None", // TODO: Handle Looping fetch data.
            "Rate" => 1.0,
            "Shuffle" => false, // TODO: Handle Shuffle fetch data.
            "Metadata" => GetMetadata(),
            "Volume" => 100.0, // TODO: Handle Getting Volume Bus Audio Level.
            "Position" => (long)TimeSpan.FromSeconds(_player.GetFullPlaybackPosition()).TotalMicroseconds,
            "MinimumRate" => 1.0,
            "MaximumRate" => 1.0,
            "CanGoNext" => false, // TODO: Handle Playlist next/looping.
            "CanGoPrevious" => false, // TODO: Handle Playlist prev/looping.
            "CanPlay" => true,
            "CanPause" => true,
            "CanSeek" => true,
            "CanControl" => true,
            _ => throw new ArgumentException($"Unknown property: {prop}")
        };

        if (prop == "Metadata" && value is IDictionary<string, object> metadata)
        {
            GLogger.Debug($"Returning Metadata with {metadata.Count} keys");
        }
        
        return Task.FromResult(value);
    }

    public Task<IDictionary<string, object>> GetAllAsync()
    {
        var properties = new Dictionary<string, object>
        {
            ["PlaybackStatus"] = GetPlaybackStatus(),
            ["LoopStatus"] = "None", // TODO: Handle Looping fetch data.
            ["Rate"] = 1.0,
            ["Shuffle"] = false, // TODO: Handle Shuffle fetch data.
            ["Metadata"] = GetMetadata(),
            ["Volume"] = 100.0, // TODO: Handle Getting Volume Bus Audio Level.
            ["Position"] = (long)TimeSpan.FromSeconds(_player.GetFullPlaybackPosition()).TotalMicroseconds,
            ["MinimumRate"] = 1.0,
            ["MaximumRate"] = 1.0,
            ["CanGoNext"] = false, // TODO: Handle Playlist next/looping.
            ["CanGoPrevious"] = false, // TODO: Handle Playlist prev/looping.
            ["CanPlay"] = true,
            ["CanPause"] = true,
            ["CanSeek"] = true,
            ["CanControl"] = true,
        };
        
        return Task.FromResult<IDictionary<string, object>>(properties);
    }

    Task IMediaPlayer2Player.SetAsync(string prop, object value)
    {
        if (prop == "Volume")
        {
            // TODO: Handle Player Volume Bus Audio Level.
        }
        return Task.CompletedTask;
    }

    Task<IDisposable> IMediaPlayer2Player.WatchPropertiesAsync(Action<PropertyChanges> handler)
    {
        _playerPropertyWatchers.Add(handler);
        GLogger.Debug($"MediaPlayer2.Player property watcher registered (total: {_playerPropertyWatchers.Count})");
        return Task.FromResult<IDisposable>(new PropertyWatcherDisposable(() =>
        {
            _playerPropertyWatchers.Remove(handler);
            GLogger.Debug($"MediaPlayer2.Player property watcher removed (total: {_playerPropertyWatchers.Count})");
        }));
    }

    Task<IDisposable> IMediaPlayer2Player.WatchSeekedAsync(Action<long> handler)
    {
        _seekedWatchers.Add(handler);
        GLogger.Debug($"Seeked signal watcher registered (total: {_seekedWatchers.Count})");
        return Task.FromResult<IDisposable>(new PropertyWatcherDisposable(() =>
        {
            _seekedWatchers.Remove(handler);
            GLogger.Debug($"Seeked signal watcher removed (total: {_seekedWatchers.Count})");
        }));
    }

    internal string GetPlaybackStatus() => _player.IsPlaying() ? (_player.IsPaused() ? "Paused" : "Playing") : "Stopped";

    internal IDictionary<string, object> GetMetadata()
    {
        var metadata = new Dictionary<string, object>();

        if (_player.CurrentSong != null)
        {
            var song = _player.CurrentSong;

            metadata["mpris:trackid"] = new ObjectPath($"/org/mpris/MediaPlayer2/Track/{song.Id}");

            if (!string.IsNullOrEmpty(song.Title))
                metadata["xesam:title"] = song.Title;

            if (song.Artists.Count > 0)
                metadata["xesam:artist"] = song.Artists.Select(x => x.Name);

            if (!string.IsNullOrEmpty(song.Album.Title))
                metadata["xesam:album"] = song.Album.Title;

            if (song.Length > 0)
                metadata["mpris:length"] = (long)TimeSpan.FromSeconds(song.Length).TotalMicroseconds;

            if (song.Album.Artwork != null)
                metadata["mpris:artUrl"] = $"file://{song.Album.Artwork.ImagePath}";
        }
        else
        {
            metadata["mpris:trackid"] = new ObjectPath("/org/mpris/MediaPlayer2/TrackList/NoTrack");
        }

        return metadata;
    }
}

internal class PropertyWatcherDisposable : IDisposable
{
    private readonly Action _handler;
    private bool _disposed;

    public PropertyWatcherDisposable(Action handler)
    {
        _handler = handler;
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _handler();
            _disposed = true;
        }
    }
}