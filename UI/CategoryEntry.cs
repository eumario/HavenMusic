using Godot;

namespace HavenMusic.UI;

[Tool, SceneTree(root: "Tree")]
public partial class CategoryEntry : Button
{
    [ExportGroup("Icon Info")]
    [Notify, Export] public partial int IconSize { get; set; }
    [Notify, Export(PropertyHint.Enum, "solid,regular,brands")] public partial string IconCategory { get; set; }
    [Notify, Export] public partial string IconName { get; set; }
    
    [ExportGroup("Text Info")]
    [Notify, Export] public partial string Text { get; set; }
    [Notify, Export] public partial int TextSize { get; set; }

    public CategoryEntry()
    {
        IconSize = 16;
        IconCategory = "solid";
        IconName = "circle-question";
        Text = "The Text";
        TextSize = 16;
    }
    
    public override partial void _Ready();
    
    [GodotOverride]
    public void OnReady()
    {
        IconNode.Set("icon_type", IconCategory);
        IconNode.Set("icon_size", IconSize);
        IconNode.Set("icon_name", IconName);
        TextNode.Text = Text;
        TextNode.AddThemeFontSizeOverride("font_size", TextSize);
        
        // Icon Events
        IconSizeChanged += () => IconNode?.Set("icon_size", IconSize);
        IconCategoryChanged += () => IconNode?.Set("icon_type", IconCategory);
        IconNameChanged += () => IconNode?.Set("icon_name", IconName);
        TextChanged += () => TextNode?.Text = Text;
        TextSizeChanged += () => TextNode?.AddThemeFontSizeOverride("font_size", TextSize);

    }
    
    public override partial void _Notification(int what);
    
    [GodotOverride]
    public async void OnNotification(int what)
    {
        if (what == NotificationResized)
        {
            var box = ((HBoxContainer)Tree.HBoxContainer);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            CustomMinimumSize = new Vector2(0, box.Size.Y);
        }
    }
}
