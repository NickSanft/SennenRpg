using Godot;

namespace SennenRpg.Core.Extensions;

public static class NodeExtensions
{
    /// <summary>
    /// Returns the first descendant of type T, or null if not found.
    /// </summary>
    public static T? FindChild<T>(this Node node) where T : Node
    {
        foreach (Node child in node.GetChildren())
        {
            if (child is T typed) return typed;
            T? found = child.FindChild<T>();
            if (found is not null) return found;
        }
        return null;
    }

    /// <summary>
    /// Returns true if the node is valid and not queued for deletion.
    /// Use this instead of != null for Godot nodes — a C# wrapper can remain
    /// non-null after the underlying Godot object has been freed.
    /// </summary>
    public static bool IsValid(this GodotObject? obj) =>
        obj is not null && GodotObject.IsInstanceValid(obj);
}
