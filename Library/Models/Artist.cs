using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Godot;

namespace HavenMusic.Library.Models;

public partial class Artist : RefCounted
{
    public int Id { get; set; }
    [Required]
    public string Name { get; set; } = null!;

    public virtual Artwork? Artwork { get; set; } = null!;
    
    // Albums this artist is associated with (many-to-many)
    public virtual ICollection<Album> Albums { get; set; } = [];
    
    // Songs this artist performed (many-to-many)
    public virtual ICollection<Song> Songs { get; set; } = [];
}