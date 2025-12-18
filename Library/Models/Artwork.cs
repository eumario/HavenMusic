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
    public byte[]? ImageData { get; set; }

    private Texture2D? _texture;

    [NotMapped]
    public Texture2D? Texture
    {
        get
        {
            if (ImageData == null || ImageData.Length == 0)
                return null;

            if (_texture == null)
            {
                var img = new Image();
                img.LoadPngFromBuffer(ImageData);
                _texture = ImageTexture.CreateFromImage(img);
            }

            return _texture;
        }
        private set;
    }
}