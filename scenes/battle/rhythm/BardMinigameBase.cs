using Godot;
using SennenRpg.Autoloads;
using SennenRpg.Core.Data;

namespace SennenRpg.Scenes.Battle;

public abstract partial class BardMinigameBase : Control
{
    [Signal] public delegate void SkillCompletedEventHandler(int grade);

    public abstract string SkillName        { get; }
    public abstract string SkillDescription { get; }

    protected abstract void OnActivate();

    public void Activate()
    {
        Visible = true;
        OnActivate();
    }

    protected void Complete(HitGrade grade)
    {
        Visible = false;
        EmitSignal(SignalName.SkillCompleted, (int)grade);
    }
}
