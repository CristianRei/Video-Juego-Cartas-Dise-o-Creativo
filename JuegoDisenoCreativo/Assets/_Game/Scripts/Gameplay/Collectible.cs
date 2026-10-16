using UnityEngine;

// Objeto que gira y se recoge al tocarlo (monedas, llaves, pociones).
public class Collectible : MonoBehaviour
{
    public enum Kind { Coin, Key, Potion }

    public Kind kind = Kind.Coin;
    public float spinSpeed = 120f;
    public float bobHeight = 0.05f;
    public float bobSpeed = 2f;

    Vector3 basePosition;

    void Start()
    {
        basePosition = transform.position;
    }

    void Update()
    {
        transform.Rotate(0f, spinSpeed * Time.deltaTime, 0f, Space.World);
        transform.position = basePosition + Vector3.up * (Mathf.Sin(Time.time * bobSpeed) * bobHeight);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.GetComponentInParent<PlayerController>() == null) return;
        GameHUD.Add(kind);
        Destroy(gameObject);
    }
}
