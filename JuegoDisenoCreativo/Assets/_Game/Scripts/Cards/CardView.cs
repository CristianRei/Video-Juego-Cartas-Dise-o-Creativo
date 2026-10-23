using System.Collections.Generic;
using TMPro;
using UnityEngine;

// Carta 3D genérica (provisional) construida por código.
// Ejes locales: X = ancho, Y = alto, Z = grosor. La cara frontal mira hacia -Z
// (igual que un Quad), así que con la rotación de la cámara la carta se ve de frente.
public class CardView : MonoBehaviour
{
    public const float Width = 0.09f;
    public const float Height = 0.13f;
    public const float Thickness = 0.002f;

    [Tooltip("Qué tan rápido sigue a su destino cuando no está en vuelo.")]
    public float followSharpness = 16f;

    public CardData Data { get; private set; }
    public bool InFlight => inFlight;

    Vector3 targetPosition, startPosition;
    Quaternion targetRotation, startRotation;
    float flightTime, flightDuration, arcHeight;
    bool inFlight;

    static readonly Dictionary<Color, Material> materialCache = new Dictionary<Color, Material>();
    static readonly Color FaceColor = new Color(0.96f, 0.93f, 0.85f);
    static readonly Color BackColor = new Color(0.55f, 0.12f, 0.14f);
    static readonly Color BackInnerColor = new Color(0.78f, 0.62f, 0.25f);
    static readonly Color InkColor = new Color(0.15f, 0.13f, 0.12f);

    // ---------- Construcción ----------

    public static CardView Create(CardData data, Vector3 position, Quaternion rotation, Transform parent = null)
    {
        var root = new GameObject("Card " + data);
        root.transform.SetParent(parent, false);
        root.transform.SetPositionAndRotation(position, rotation);
        var view = root.AddComponent<CardView>();
        view.Data = data;
        view.targetPosition = position;
        view.targetRotation = rotation;

        // Cuerpo (también es el collider para hacer clic).
        var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
        body.name = "Body";
        body.transform.SetParent(root.transform, false);
        body.transform.localScale = new Vector3(Width, Height, Thickness);
        body.GetComponent<Renderer>().sharedMaterial = GetMaterial(FaceTint(data));

        // Reverso: marco rojo con un rombo dorado.
        AddQuad(root.transform, "Back", new Vector3(0f, 0f, Thickness * 0.5f + 0.0002f), Width * 0.98f, Height * 0.98f, BackColor, true);
        var emblem = AddQuad(root.transform, "BackEmblem", new Vector3(0f, 0f, Thickness * 0.5f + 0.0004f), Width * 0.45f, Width * 0.45f, BackInnerColor, true);
        emblem.transform.localRotation *= Quaternion.Euler(0f, 0f, 45f);

        // Cara frontal: valor grande al centro y valores pequeños en las esquinas.
        float z = -Thickness * 0.5f - 0.0003f;
        Color ink = data.IsWildcard ? Color.white : InkColor;
        if (data.IsWildcard)
        {
            AddText(root.transform, WildcardTitle(data.kind), new Vector3(0f, 0.01f, z), 0.30f, ink, new Vector2(Width * 0.9f, Height * 0.6f));
            AddText(root.transform, WildcardSymbol(data.kind), new Vector3(0f, -0.035f, z), 0.45f, ink, new Vector2(Width * 0.9f, 0.04f));
        }
        else
        {
            AddText(root.transform, data.value.ToString(), new Vector3(0f, 0f, z), 0.6f, NumberColor(data.value), new Vector2(Width, Height * 0.6f));
            // El 6 y el 9 van subrayados para no confundirlos en la esquina invertida.
            string cornerText = data.value == 6 || data.value == 9 ? $"<u>{data.value}</u>" : data.value.ToString();
            AddText(root.transform, cornerText, new Vector3(-Width * 0.36f, Height * 0.4f, z), 0.22f, ink, new Vector2(0.03f, 0.025f));
            var corner = AddText(root.transform, cornerText, new Vector3(Width * 0.36f, -Height * 0.4f, z), 0.22f, ink, new Vector2(0.03f, 0.025f));
            corner.transform.localRotation = Quaternion.Euler(0f, 0f, 180f);
        }
        return view;
    }

    static GameObject AddQuad(Transform parent, string name, Vector3 localPos, float w, float h, Color color, bool facingBack)
    {
        var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.name = name;
        Object.Destroy(quad.GetComponent<Collider>());
        quad.transform.SetParent(parent, false);
        quad.transform.localPosition = localPos;
        quad.transform.localRotation = facingBack ? Quaternion.Euler(0f, 180f, 0f) : Quaternion.identity;
        quad.transform.localScale = new Vector3(w, h, 1f);
        quad.GetComponent<Renderer>().sharedMaterial = GetMaterial(color);
        return quad;
    }

    static TextMeshPro AddText(Transform parent, string text, Vector3 localPos, float size, Color color, Vector2 area)
    {
        var go = new GameObject("Text");
        go.transform.SetParent(parent, false);
        var tmp = go.AddComponent<TextMeshPro>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.color = color;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        tmp.rectTransform.sizeDelta = area;
        go.transform.localPosition = localPos;
        return tmp;
    }

    public static Color EdgeColor => FaceColor;

    public static Material GetMaterial(Color color)
    {
        if (materialCache.TryGetValue(color, out var mat) && mat != null) return mat;
        mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.SetColor("_BaseColor", color);
        mat.SetFloat("_Smoothness", 0.15f);
        materialCache[color] = mat;
        return mat;
    }

    static Color FaceTint(CardData data) => data.kind switch
    {
        CardKind.Reverse => new Color(0.20f, 0.45f, 0.75f),
        CardKind.InvertComparison => new Color(0.50f, 0.30f, 0.70f),
        CardKind.Skip => new Color(0.80f, 0.30f, 0.25f),
        CardKind.SetValue => new Color(0.80f, 0.60f, 0.15f),
        _ => FaceColor
    };

    static Color NumberColor(int value) => Color.Lerp(new Color(0.15f, 0.40f, 0.65f), new Color(0.75f, 0.20f, 0.15f), (value - 1) / 9f);

    static string WildcardTitle(CardKind kind) => kind switch
    {
        CardKind.Reverse => "CAMBIO\nDE SENTIDO",
        CardKind.InvertComparison => "INVERTIR\nCOMPARACIÓN",
        CardKind.Skip => "SALTO",
        CardKind.SetValue => "FIJAR\nVALOR",
        _ => ""
    };

    static string WildcardSymbol(CardKind kind) => kind switch
    {
        CardKind.Reverse => "<  >",
        CardKind.InvertComparison => ">= <=",
        CardKind.Skip => ">>",
        CardKind.SetValue => "1-10",
        _ => ""
    };

    // ---------- Movimiento ----------

    // Vuela en arco hasta un destino. El destino se puede seguir actualizando con SetTarget.
    public void FlyTo(Vector3 position, Quaternion rotation, float duration = 0.35f, float arc = 0.08f)
    {
        startPosition = transform.position;
        startRotation = transform.rotation;
        targetPosition = position;
        targetRotation = rotation;
        flightTime = 0f;
        flightDuration = Mathf.Max(0.01f, duration);
        arcHeight = arc;
        inFlight = true;
    }

    // Cambia el destino sin reiniciar el vuelo (por ejemplo, al reacomodar la mano).
    public void SetTarget(Vector3 position, Quaternion rotation)
    {
        targetPosition = position;
        targetRotation = rotation;
    }

    void LateUpdate()
    {
        float dt = Time.deltaTime;
        if (inFlight)
        {
            flightTime += dt;
            float t = Mathf.Clamp01(flightTime / flightDuration);
            float eased = t * t * (3f - 2f * t);
            transform.SetPositionAndRotation(
                Vector3.Lerp(startPosition, targetPosition, eased) + Vector3.up * (arcHeight * Mathf.Sin(t * Mathf.PI)),
                Quaternion.Slerp(startRotation, targetRotation, eased));
            if (t >= 1f) inFlight = false;
        }
        else
        {
            float k = 1f - Mathf.Exp(-followSharpness * dt);
            transform.SetPositionAndRotation(
                Vector3.Lerp(transform.position, targetPosition, k),
                Quaternion.Slerp(transform.rotation, targetRotation, k));
        }
    }
}
