namespace SennenRpg.Core.Interfaces;

public interface IInteractable
{
    void Interact(Godot.Node player);
    string GetInteractPrompt();
}
