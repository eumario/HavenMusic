using Godot;
using HavenMusic.Library.Models;

namespace HavenMusic.Library;

[GlobalClass]
public partial class MediaPlayer : Node
{
    private AudioStreamPlayer _player;
    private Song? _currentSong;

    [Signal]
    public delegate void SongChangedEventHandler(Song? song);
    [Signal]
    public delegate void SongLoadedEventHandler(Song song);

    [Signal]
    public delegate void SongLoadFailedEventHandler(Song song);

    [Signal]
    public delegate void PlaybackStartedEventHandler();

    [Signal]
    public delegate void PlaybackStoppedEventHandler();

    [Signal]
    public delegate void PlaybackPausedEventHandler();
    
    [Signal]
    public delegate void PlaybackResumedEventHandler();

    [Signal]
    public delegate void PlaybackPositionChangedEventHandler(float position);

    [Signal]
    public delegate void PlaybackFinishedEventHandler();

    public Song? CurrentSong
    {
        get => _currentSong;
        set
        {
            _currentSong = value;
            if (_currentSong != null)
            {
                var media = new AudioStreamFFmpeg();
                if (media.Open(value.FilePath) == Error.Ok)
                {
                    EmitSignalSongLoaded(value);
                    _player.Stream = media;
                }
                else
                {
                    EmitSignalSongLoadFailed(value);
                    _player.Stream = null;
                    GD.PushError("MediaPlayer failed to load Audio Stream using AudioStreamFFmpeg.");
                }
            }
            else
            {
                _player.Stream = null;
            }

            EmitSignalSongChanged(value);
        }
    }
    

    public override void _Ready()
    {
        _player = new AudioStreamPlayer();
        AddChild(_player);
        _player.Finished += EmitSignalPlaybackFinished;
    }

    public override void _Process(double delta)
    {
        if (!_player.IsPlaying()) return;
        EmitSignalPlaybackPositionChanged((float)(_player.GetPlaybackPosition() + AudioServer.GetTimeSinceLastMix()));
    }

    public void Play(float fromPos = 0.0f)
    {
        _player.Play(fromPos);
        EmitSignalPlaybackStarted();
    }
    
    public void Stop()
    {
        _player.Stop();
        EmitSignalPlaybackStopped();
    }

    public void Pause()
    {
        _player.StreamPaused = true;
        EmitSignalPlaybackPaused();
    }

    public void Resume()
    {
        _player.StreamPaused = false;
        EmitSignalPlaybackResumed();
    }
    
    public void Seek(float position)
    {
        _player.Seek(position);
    }

    public float GetPlaybackPosition()
    {
        return _player.GetPlaybackPosition();
    }

    public float GetFullPlaybackPosition()
    {
        return (float)(GetPlaybackPosition() + AudioServer.GetTimeSinceLastMix());
    }

    public bool IsPaused() => _player.StreamPaused;
    public bool IsPlaying() => _player.IsPlaying();
}