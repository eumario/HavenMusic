using Godot;
using System.Linq;
using HavenMusic.Library.Models;
using HavenMusic.Library.Resources;

namespace HavenMusic.UI;

[SceneTree(root: "Tree")]
public partial class AlbumChip : PanelContainer
{
    [Export]
    public Album? Album;
    

    [Signal]
    public delegate void AlbumSelectedEventHandler(Album album);
    

    [OnInstantiate]

    public void OnInit(Album? album = null)
    {
        Album = album;
    }
    
    public override partial void _Ready();
    
    [GodotOverride]
    public void OnReady()
    {
        if (Album == null) return;
        
        Artwork.Texture = Album.Artwork?.Texture ?? ArtTextures.NoAlbumArtPng.Load();

        Title.Text = Album.Title;
        Artists.Text = string.Join(", ", Album.Artists.Select(artist => artist.Name));
        SongCount.Text = $"{Album.Songs.Count} songs";
    }
    
    public override partial void _GuiInput(InputEvent inputEvent);
    
    [GodotOverride]
    public void OnGuiInput(InputEvent inputEvent)
    {
        if (inputEvent is InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true })
        {
            EmitSignalAlbumSelected(Album);
        }
    }
}
