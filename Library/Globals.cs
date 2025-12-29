using Godot;
using HavenMusic.Library.Models;

public partial class Globals : Node
{
    public static Globals Instance { get; private set; }
    [Signal]
    public delegate void ArtistArtworkUpdatedEventHandler(Artist artist, Artwork artwork);

    [Signal]
    public delegate void AlbumArtworkUpdatedEventHandler(Song song, Artwork artwork);

    public void EmitArtistArtworkUpdated(Artist artist, Artwork artwork) =>
        EmitSignalArtistArtworkUpdated(artist, artwork);
    public void EmitAlbumArtworkUpdated(Song song, Artwork artwork) => EmitSignalAlbumArtworkUpdated(song, artwork);

    public override void _Ready()
    {
        Instance = this;
    }
}
