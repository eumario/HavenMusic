using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Godot;

namespace HavenMusic.Library.Models;

public partial class Song : RefCounted
{
    public int Id { get; set; }
    [Required]
    public string Title { get; set; } = null!;
    [Required]
    public string FilePath { get; set; } = null!;

    public float Length { get; set; } = 0;

    public virtual Artwork? Artwork { get; set; } = null!;
    
    // FK to Album
    public int? AlbumId { get; set; }
    // Navigation back to Album
    public virtual Album? Album { get; set; }
    
    // Artists who performed this song (many-to-many)
    public virtual ICollection<Artist> Artists { get; set; } = [];

    // Playlists with this song on it (many-to-many)
    public virtual ICollection<Playlist> Playlists { get; set; } = [];
}