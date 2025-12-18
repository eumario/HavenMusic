using System;
using System.Linq;
using Godot;
using HavenMusic.Library;
using HavenMusic.Library.Models;

namespace HavenMusic.UI;

[SceneTree(root: "Tree")]
public partial class QueueEntry : PanelContainer
{
    public Song Song;
    [OnInstantiate]
    public void Init(Song song)
    {
        Song = song;
    }
    
    public override partial void _Ready();
    
    [GodotOverride]
    public void OnReady()
    {
        var artwork = Song.Artwork;
        SongIcon.Texture = artwork?.Texture;
        SongTitle.Text = Song.Title;
        SongArtists.Text = string.Join(", ", Song.Artists.Select(x => x.Name));
        SongLength.Text = TimeSpan.FromSeconds(Song.Length).ToDisplayTime();
    }
}
