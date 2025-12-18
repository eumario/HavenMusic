using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Godot;

namespace HavenMusic.Library.Models;

public partial class Album : RefCounted
{
    public int Id { get; set; }
    [Required]
    public string Title { get; set; } = null!;
    
    // Songs in this album (one-to-many)
    public virtual ICollection<Song> Songs { get; set; } = [];
    
    // Artists credited on this album (many-to-many)
    public virtual ICollection<Artist> Artists { get; set; } = [];
    
    // Single Artwork associated with this Album
    public virtual Artwork? Artwork { get; set; } = null!;

}