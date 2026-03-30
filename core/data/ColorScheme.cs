using Godot;

namespace SennenRpg.Core.Data;

[GlobalClass]
public partial class ColorScheme : Resource
{
    [Export] public string SchemeName { get; set; } = "Default";
    [Export] public Color  Tint       { get; set; } = Colors.White;
}
