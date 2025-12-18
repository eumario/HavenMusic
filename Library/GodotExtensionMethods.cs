using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using Godot;
using RandomNumberGenerator = Godot.RandomNumberGenerator;

namespace HavenMusic.Library;

public static class GodotExtensionMethods
{
    public static Vector2I ToVector2I(this Vector2 point) => new Vector2I((int)point.X, (int)point.Y);
    public static Rect2I ToRect2I(this Rect2 rect) => new Rect2I(rect.Position.ToVector2I(), rect.Size.ToVector2I());
    public static string GlobalizePath(this string path) => ProjectSettings.GlobalizePath(path);

    public static SignalAwaiter ProcessFrame(this Node node) => node.ToSignal(node.GetTree(), SceneTree.SignalName.ProcessFrame);

    public static T? FindChild<T>(this Node? parent)
    {
        if (parent == null) return default;
        foreach (var child in parent.GetChildren())
        {
            if (child is T t)
                return t;
        }

        return default;
    }
    
    public static IEnumerable<(T item, int index)> WithIndex<T>(this IEnumerable<T> source)
    {
        return source.Select((item, index) => (item, index));
    }

    public static void QueueFreeChildren(this Node node)
    {
        foreach (var child in node.GetChildren())
            child.QueueFree();
    }

    public static void EmitSignalPressed(this Button button) => button.EmitSignal(Button.SignalName.Pressed);

    public static byte[] Sha512Hash(this byte[] data)
    {
        using var sha = SHA512.Create();
        return sha.ComputeHash(data);
    }

    public static string HashToStr(this byte[] data)
    {
        var sb = new StringBuilder();
        foreach (var t in data)
        {
            sb.Append(t.ToString("x2"));
        }

        return sb.ToString();
    }

    public static string ToDisplayTime(this TimeSpan time)
    {
        if (time.Hours > 0)
            return time.ToString(@"hh\:mm\:ss");
        else
            return time.ToString(@"mm\:ss");
    }
}