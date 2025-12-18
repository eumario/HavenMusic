using Godot;
using HavenMusic.Library.Models;
using HavenMusic.Library.Resources;

namespace HavenMusic.UI;

[SceneTree(root: "Tree")]
public partial class ArtistChip : PanelContainer
{
    [Export]
    public Artist? Artist;

    [Signal]
    public delegate void ArtistSelectedEventHandler(Artist artist);

    [OnInstantiate]
    public void OnInit(Artist? artist = null)
    {
        Artist = artist;
    }

    public override partial void _Ready();

    [GodotOverride]
    public void OnReady()
    {
        if (Artist == null) return;
        
        Artwork.Texture = Artist.Artwork?.Texture ?? ArtTextures.NoArtistArtPng.Load();

        ArtistName.Text = Artist.Name;
        AlbumCount.Text = $"{Artist.Albums.Count} albums";
        SongCount.Text = $"{Artist.Songs.Count} songs";
    }

    public override partial void _GuiInput(InputEvent inputEvent);

    [GodotOverride]
    public void OnGuiInput(InputEvent inputEvent)
    {
        if (inputEvent is InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true })
        {
            EmitSignalArtistSelected(Artist);
        }
    }
}
