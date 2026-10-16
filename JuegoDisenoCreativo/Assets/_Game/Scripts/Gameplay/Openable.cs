using UnityEngine;

// Objeto que se abre y cierra con su animación (cofres, rejas).
// El Animator necesita un parámetro bool "Open".
public class Openable : MonoBehaviour, IInteractable
{
    public bool isOpen;
    [Tooltip("Collider que bloquea el paso mientras está cerrado (por ejemplo, la reja).")]
    public Collider blockingCollider;

    static readonly int OpenHash = Animator.StringToHash("Open");
    Animator animator;

    void Awake()
    {
        animator = GetComponentInChildren<Animator>();
        Apply();
    }

    public bool CanInteract(PlayerController player) => true;

    public void Interact(PlayerController player) => Toggle();

    public void Toggle()
    {
        isOpen = !isOpen;
        Apply();
    }

    void Apply()
    {
        if (animator != null) animator.SetBool(OpenHash, isOpen);
        if (blockingCollider != null) blockingCollider.enabled = !isOpen;
    }
}
