using UnityEngine;
using UnityEngine.InputSystem;

// Cámara en tercera persona con vista elevada (estilo diorama).
// Mantén clic derecho y mueve el mouse para girarla; la rueda hace zoom.
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

    Vector3 focusPoint;

    void Start()
    {
        if (target != null) focusPoint = target.position + targetOffset;
    }

    void LateUpdate()
    {
        if (target == null) return;

        var mouse = Mouse.current;
        if (mouse != null)
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

        focusPoint = Vector3.Lerp(focusPoint, target.position + targetOffset, followSmoothing * Time.deltaTime);
        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
        transform.position = focusPoint - rotation * Vector3.forward * distance;
        transform.rotation = rotation;
    }
}
