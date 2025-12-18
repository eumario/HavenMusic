using Godot;

namespace HavenMusic.UI;

public partial class Header : PanelContainer
{
    private bool isCaptured = false;
    private bool isMoving = false;
    private Vector2 mousePos = Vector2.Zero;
    
    public override partial void _Ready();
    
    [GodotOverride]
    public void OnReady()
    {
        isCaptured = false;
    }

    public void HideBar()
    {
        ThemeTypeVariation = "FullWindowContainer";
        GetNode<Button>("HBoxContainer/MenuButton").Visible = false;
        GetNode<HBoxContainer>("HBoxContainer/HBoxContainer").Visible = false;
        GetNode<Label>("HBoxContainer/CenterContainer/WinTitle").Visible = false;
    }

    public void ShowBar()
    {
        ThemeTypeVariation = "WindowHeader";
        GetNode<Button>("HBoxContainer/MenuButton").Visible = true;
        GetNode<HBoxContainer>("HBoxContainer/HBoxContainer").Visible = true;
        GetNode<Label>("HBoxContainer/CenterContainer/WinTitle").Visible = true;
    }
    
    public override partial void _GuiInput(InputEvent inputEvent);
    
    [GodotOverride]
    public void OnGuiInput(InputEvent inputEvent)
    {
        if (inputEvent is InputEventMouseButton mouseEvent)
        {
            if (mouseEvent.ButtonIndex == MouseButton.Left && mouseEvent.Pressed)
            {
                isCaptured = true;
                mousePos = GetLocalMousePosition();
            }
            else if (mouseEvent.ButtonIndex == MouseButton.Left && !mouseEvent.Pressed)
                isCaptured = false;
        } else if (inputEvent is InputEventMouseMotion mouseMotion && !isMoving && isCaptured)
        {
            var movement = GetLocalMousePosition() - mousePos;
            if (movement.Length() <= 0) return;
            isMoving = true;
            var newPos = GetViewport().GetWindow().Position + movement;
            GetViewport().GetWindow().Position = new Vector2I((int)newPos.X, (int)newPos.Y);
            isMoving = false;
        }
    }
}
