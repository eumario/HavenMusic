using System;
using System.Collections.Generic;
using System.Linq;
using HavenMusic.Library.Models;

namespace HavenMusic.Library;

public class PlayerQueue
{
    private static PlayerQueue _instance;
    public static PlayerQueue Instance => _instance ??= new PlayerQueue();
    private List<Song> _queue = [];
    private List<Song> _original = [];
    private int _pos = 0;

    public event EventHandler? QueueChanged;
    public event EventHandler<int>? PositionChanged;
    
    public ICollection<Song> Queue => _queue.AsReadOnly();

    public bool IsLooping { get; set; }
    public bool IsShuffled => _original.Count > 0;

    public void Shuffle()
    {
        _original = _queue.ToList();
        _queue = _queue.Shuffle().ToList();
        QueueChanged?.Invoke(this, EventArgs.Empty);
        _pos = 0;
    }

    public void UnShuffle()
    {
        _queue = _original.ToList();
        _original = new List<Song>();
        _pos = 0;
        QueueChanged?.Invoke(this, EventArgs.Empty);
    }

    public void SetPosition(int position)
    {
        _pos = position;
        PositionChanged?.Invoke(this, position);
    }

    public int GetPosition(Song song)
    {
        var indx = _queue.FindIndex(qs => qs.Id == song.Id);
        return indx;
    }

    public void QueueSong(Song song)
    {
        _queue.Add(song);
        QueueChanged?.Invoke(this, EventArgs.Empty);
    }

    public void QueueSongs(List<Song> songs)
    {
        songs.ForEach(x => _queue.Add(x));
        QueueChanged?.Invoke(this, EventArgs.Empty);
    }

    public Song? NextSong()
    {
        _pos++;
        if (_pos >= _queue.Count && !IsLooping)
            return null;
        if (_pos >= _queue.Count)
            _pos = 0;
        
        PositionChanged?.Invoke(this, _pos);
        return _queue[_pos];
    }

    public Song? PrevSong()
    {
        _pos--;
        if (_pos < 0 && !IsLooping)
        {
            _pos = 0;
            return null;
        }

        if (_pos < 0)
            _pos = _queue.Count - 1;
        
        PositionChanged?.Invoke(this, _pos);
        return _queue[_pos];
    }

    public void ClearQueue()
    {
        _pos = 0;
        _queue.Clear();
        QueueChanged?.Invoke(this, EventArgs.Empty);
    }

    public Song? CurrentSong => _pos > _queue.Count ? null : _queue[_pos];

    public int Count => _queue.Count;
}