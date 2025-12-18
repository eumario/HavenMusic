using System;
using System.Collections.Generic;
using HavenMusic.Library.Models;
using Microsoft.EntityFrameworkCore;

namespace HavenMusic.Library;

public class Database : DbContext
{
    private readonly string _path;
    public virtual DbSet<Album> Albums { get; set; }
    public virtual DbSet<Artist> Artists { get; set; }
    public virtual DbSet<Artwork> Artworks { get; set; }
    public virtual DbSet<Song> Songs { get; set; }
    public virtual DbSet<Playlist> Playlists { get; set; }
    
    public Database(string path)
    {
        _path = path;
    }

    public static Database InitDatabase(string path)
    {
        var context = new Database(path);
        context.Database.EnsureCreated();
        // Disable Journal logging
        context.Database.ExecuteSqlRaw("PRAGMA journal_mode='DELETE';");
        return context;
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) =>
        optionsBuilder
            .UseLazyLoadingProxies()
            .UseSqlite($"Data Source={_path}");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Song -> Album many-to-one (many songs in one album)
        modelBuilder.Entity<Song>()
            .HasOne(s => s.Album)
            .WithMany(a => a.Songs)
            .HasForeignKey(s => s.AlbumId)
            .OnDelete(DeleteBehavior.Cascade);
        
        // Playlist <-> Song many-to-many (use explicit join table name)
        modelBuilder.Entity<Playlist>()
            .HasMany(p => p.Songs)
            .WithMany(a => a.Playlists)
            .UsingEntity<Dictionary<string, object>>(
                "PlaylistSong",
                j => j.HasOne<Song>().WithMany().HasForeignKey("SongId").OnDelete(DeleteBehavior.Cascade),
                j => j.HasOne<Playlist>().WithMany().HasForeignKey("PlaylistId").OnDelete(DeleteBehavior.Cascade),
                j =>
                {
                    j.HasKey("PlaylistId", "SongId");
                    j.ToTable("PlaylistSongs");
                });
        
        // Artist <-> Album many-to-many (use explicit join table name)
        modelBuilder.Entity<Artist>()
            .HasMany(a => a.Albums)
            .WithMany(al => al.Artists)
            .UsingEntity<Dictionary<string, object>>(
                "ArtistAlbum",
                j => j.HasOne<Album>().WithMany().HasForeignKey("AlbumId").OnDelete(DeleteBehavior.Cascade),
                j => j.HasOne<Artist>().WithMany().HasForeignKey("ArtistId").OnDelete(DeleteBehavior.Cascade),
                j =>
                {
                    j.HasKey("ArtistId", "AlbumId");
                    j.ToTable("ArtistAlbums");
                });
        
        // Artist <-> Song many-to-many (use explicit join table)
        modelBuilder.Entity<Artist>()
            .HasMany(a => a.Songs)
            .WithMany(s => s.Artists)
            .UsingEntity<Dictionary<string, object>>(
                "ArtistSong",
                j => j.HasOne<Song>().WithMany().HasForeignKey("SongId").OnDelete(DeleteBehavior.Cascade),
                j => j.HasOne<Artist>().WithMany().HasForeignKey("ArtistId").OnDelete(DeleteBehavior.Cascade),
                j =>
                {
                    j.HasKey("ArtistId", "SongId");
                    j.ToTable("ArtistSongs");
                });
    }
}