using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Godot;

namespace HavenMusic.Library.Models;

public partial class Artwork : RefCounted
{
    public int Id { get; set; }
    [Required]
    public string Hash { get; set; }
    [Required]
    public string ImagePath { get; set; }

    private Texture2D? _texture;

    [NotMapped]
    public Texture2D? Texture
    {
        get
        {
            if (string.IsNullOrEmpty(ImagePath))
                return null;

            if (_texture == null)
            {
                var img = Image.LoadFromFile(ImagePath);
                if (img == null)
                    return null;
                
                _texture = ImageTexture.CreateFromImage(img);
            }

            return _texture;
        }
        private set;
    }
}