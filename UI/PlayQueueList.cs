using System;
using Godot;
using HavenMusic.Library;

namespace HavenMusic.UI;

[SceneTree(root: "Tree")]
public partial class PlayQueueList : PanelContainer
{
    public override partial void _Ready();
    
    [GodotOverride]
    public void OnReady()
    {
        PlayerQueue.Instance.QueueChanged += HandleQueueChanged;
        PlayerQueue.Instance.PositionChanged += HandlePositionChanged;
    }

    public void HandleQueueChanged(object? sender, EventArgs e)
    {
        SongList.QueueFreeChildren();
        foreach (var song in PlayerQueue.Instance.Queue)
        {
            var entry = QueueEntry.Instantiate(song);
            if (PlayerQueue.Instance.CurrentSong == song)
                entry.ThemeTypeVariation = "QueueEntrySelected";
            SongList.AddChild(entry);
        }
    }

    public async void HandlePositionChanged(object? sender, int position)
    {
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        foreach (var (entry, pos) in SongList.GetChildren().WithIndex())
        {
            var songEntry = ((QueueEntry)entry);
            songEntry.ThemeTypeVariation = pos == position ? "QueueEntrySelected" : "QueueEntryNormal";
            if (pos == position)
                ((ScrollContainer)Tree.VBoxContainer.ScrollContainer).ScrollVertical = (int)songEntry.Position.Y;
        }
    }
}
