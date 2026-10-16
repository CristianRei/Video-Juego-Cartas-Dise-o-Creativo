using UnityEngine;

// Cualquier objeto con el que el jugador puede interactuar con la tecla E.
public interface IInteractable
{
    bool CanInteract(PlayerController player);
    void Interact(PlayerController player);

    // Texto de la ayuda en pantalla (por ejemplo "Sentarse") y punto del mundo donde se muestra.
    string GetPrompt(PlayerController player);
    Vector3 PromptPosition { get; }
}
