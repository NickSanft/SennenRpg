using Godot;
using SennenRpg.Autoloads;
using SennenRpg.Core.Data;

namespace SennenRpg.Scenes.Battle;

public abstract partial class BardMinigameBase : Control
{
    [Signal] public delegate void SkillCompletedEventHandler(int grade);

    public abstract string SkillName        { get; }
    public abstract string SkillDescription { get; }

    /// <summary>Viewport size — use instead of Size, which is always (0,0) inside a Node2D parent.</summary>
    protected Vector2 VP => GetViewportRect().Size;

    protected abstract void OnActivate();

    public void Activate()
    {
        Visible = true;
        GetViewport().GuiReleaseFocus();
        OnActivate();
    }

    protected void Complete(HitGrade grade)
    {
        Visible = false;
        EmitSignal(SignalName.SkillCompleted, (int)grade);
    }
}
