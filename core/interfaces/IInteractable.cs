namespace SennenRpg.Core.Interfaces;

public interface IInteractable
{
    void Interact(Godot.Node player);
    string GetInteractPrompt();

    /// <summary>Called when this becomes the nearest interactable. Show a prompt label.</summary>
    void ShowPrompt() { }

    /// <summary>Called when this is no longer the nearest interactable. Hide the prompt label.</summary>
    void HidePrompt() { }
}
