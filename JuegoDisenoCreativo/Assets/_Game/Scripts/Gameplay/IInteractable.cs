// Cualquier objeto con el que el jugador puede interactuar con la tecla E.
public interface IInteractable
{
    bool CanInteract(PlayerController player);
    void Interact(PlayerController player);
}
