using UnityEngine;

// Silla en la que se puede sentar el jugador (E) o un personaje desde el inicio.
// El frente de la silla es su eje Z local; quien se sienta mira hacia ese lado.
public class Seat : MonoBehaviour, IInteractable
{
    [Tooltip("Posición local del personaje sentado, relativa a la silla.")]
    public Vector3 sitOffset = new Vector3(0f, 0.30f, 0.13f);
    [Tooltip("Distancia detrás de la silla donde queda el jugador al levantarse.")]
    public float standDistance = 0.5f;
    [Tooltip("Personaje (no jugador) que empieza sentado aquí.")]
    public Animator seatedAtStart;

    static readonly int SitHash = Animator.StringToHash("Sit");

    PlayerController occupant;

    public Vector3 SitPosition => transform.TransformPoint(sitOffset);
    public Quaternion SitRotation => transform.rotation;
    public Vector3 StandPosition => transform.position - transform.forward * standDistance;
    public bool IsOccupied => occupant != null || seatedAtStart != null;

    void Start()
    {
        if (seatedAtStart == null) return;
        seatedAtStart.transform.SetPositionAndRotation(SitPosition, SitRotation);
        seatedAtStart.SetBool(SitHash, true);
    }

    public bool CanInteract(PlayerController player) => !IsOccupied || occupant == player;

    public string GetPrompt(PlayerController player) => occupant == player ? "Levantarse" : "Sentarse";

    public Vector3 PromptPosition => transform.position + Vector3.up * 0.75f;

    public void Interact(PlayerController player)
    {
        if (occupant == player)
        {
            occupant = null;
            player.StandUp();
        }
        else if (!IsOccupied)
        {
            occupant = player;
            player.SitOn(this);
        }
    }
}
