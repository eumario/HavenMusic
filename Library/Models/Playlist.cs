using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Godot;

namespace HavenMusic.Library.Models;

public partial class Playlist : RefCounted
{
    public int Id { get; set; }
    [Required]
    public string Name { get; set; } = null!;
    
    // Songs in this playlist (one-to-many)
    public virtual ICollection<Song> Songs { get; set; } = [];
}