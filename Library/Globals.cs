using Godot;
using System;
using HavenMusic.Library.Models;

[Singleton]
public partial class Globals : Node
{
    [Signal]
    public delegate void ArtistArtworkUpdatedEventHandler(Artist artist, Artwork artwork);

    [Signal]
    public delegate void AlbumArtworkUpdatedEventHandler(Song song, Artwork artwork);

    public void EmitArtistArtworkUpdated(Artist artist, Artwork artwork) =>
        EmitSignalArtistArtworkUpdated(artist, artwork);
    public void EmitAlbumArtworkUpdated(Song song, Artwork artwork) => EmitSignalAlbumArtworkUpdated(song, artwork);
}
