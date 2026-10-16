using UnityEngine;
using UnityEngine.InputSystem;

// Cámara en tercera persona con vista elevada (estilo diorama).
// Mantén clic derecho y mueve el mouse para girarla; la rueda hace zoom.
// Cuando el jugador se sienta, pasa suavemente a una vista en primera persona mirando la mesa.
public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public float distance = 5f;
    public float minDistance = 2.5f;
    public float maxDistance = 9f;
    public float pitch = 45f;
    public float yaw = 0f;
    public float orbitSensitivity = 0.2f;
    public float zoomSensitivity = 0.005f;
    public float followSmoothing = 10f;
    public Vector3 targetOffset = new Vector3(0f, 0.4f, 0f);

    [Header("Vista sentado a la mesa")]
    [Tooltip("Distancia detrás de la silla (negativo = delante: vista en primera persona).")]
    public float seatedBack = -0.15f;
    [Tooltip("Altura de la cámara sobre la base de la silla.")]
    public float seatedHeight = 1.05f;
    [Tooltip("Distancia al frente de la silla a la que mira (centro de la mesa).")]
    public float seatedLookAhead = 1.0f;
    public float seatedLookHeight = 0.5f;
    public float seatedFieldOfView = 55f;
    [Tooltip("Velocidad de la transición entre vistas (1 = un segundo).")]
    public float transitionSpeed = 2f;

    Vector3 focusPoint;
    PlayerController player;
    Camera cam;
    float defaultFieldOfView;
    float seatedBlend;
    Transform lastSeat;

    void Start()
    {
        cam = GetComponent<Camera>();
        if (cam != null) defaultFieldOfView = cam.fieldOfView;
        if (target != null)
        {
            focusPoint = target.position + targetOffset;
            player = target.GetComponent<PlayerController>();
        }
    }

    void LateUpdate()
    {
        if (target == null) return;
        bool sitting = player != null && player.IsSitting;

        var mouse = Mouse.current;
        if (mouse != null && !sitting)
        {
            if (mouse.rightButton.isPressed)
            {
                Vector2 delta = mouse.delta.ReadValue();
                yaw += delta.x * orbitSensitivity;
                pitch = Mathf.Clamp(pitch - delta.y * orbitSensitivity, 20f, 80f);
            }
            float scroll = mouse.scroll.ReadValue().y;
            distance = Mathf.Clamp(distance - scroll * zoomSensitivity, minDistance, maxDistance);
        }

        // Vista normal (órbita).
        focusPoint = Vector3.Lerp(focusPoint, target.position + targetOffset, followSmoothing * Time.deltaTime);
        Quaternion orbitRotation = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 orbitPosition = focusPoint - orbitRotation * Vector3.forward * distance;

        if (sitting) lastSeat = player.CurrentSeat.transform;
        seatedBlend = Mathf.MoveTowards(seatedBlend, sitting ? 1f : 0f, transitionSpeed * Time.deltaTime);
        if (seatedBlend <= 0f || lastSeat == null)
        {
            transform.SetPositionAndRotation(orbitPosition, orbitRotation);
            if (cam != null) cam.fieldOfView = defaultFieldOfView;
            return;
        }

        // Vista sentado: a la altura de los ojos, mirando al centro de la mesa.
        var seat = lastSeat;
        Vector3 forward = Vector3.ProjectOnPlane(seat.forward, Vector3.up).normalized;
        Vector3 seatedPosition = seat.position - forward * seatedBack + Vector3.up * seatedHeight;
        Vector3 lookPoint = seat.position + forward * seatedLookAhead + Vector3.up * seatedLookHeight;
        Quaternion seatedRotation = Quaternion.LookRotation(lookPoint - seatedPosition);

        float t = Mathf.SmoothStep(0f, 1f, seatedBlend);
        transform.SetPositionAndRotation(
            Vector3.Lerp(orbitPosition, seatedPosition, t),
            Quaternion.Slerp(orbitRotation, seatedRotation, t));
        if (cam != null) cam.fieldOfView = Mathf.Lerp(defaultFieldOfView, seatedFieldOfView, t);
    }
}
