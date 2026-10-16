using UnityEngine;

// Ayudas de la tecla E:
// - Flotando sobre el objeto cercano con el que se puede interactuar ("[E] Sentarse").
// - Abajo a la izquierda mientras el jugador está sentado ("[E] Levantarse").
// Provisional con OnGUI, igual que GameHUD; se migrará a la UI definitiva.
public class InteractionPromptUI : MonoBehaviour
{
    public PlayerController player;
    public string keyLabel = "E";
    public float keySize = 40f;
    public float bobAmplitude = 4f;
    public float bobSpeed = 3f;

    GUIStyle keyStyle, textStyle;

    void Start()
    {
        if (player == null) player = FindFirstObjectByType<PlayerController>();
    }

    void OnGUI()
    {
        if (player == null) return;
        if (keyStyle == null) CreateStyles();

        if (player.IsSitting)
        {
            float height = keySize + 16f;
            DrawPrompt(new Vector2(20f, Screen.height - 50f - height), "Levantarse");
            return;
        }

        var interactable = player.FocusedInteractable;
        var cam = Camera.main;
        if (interactable == null || cam == null) return;

        Vector3 screen = cam.WorldToScreenPoint(interactable.PromptPosition);
        if (screen.z <= 0f) return;

        string text = interactable.GetPrompt(player);
        float width = PromptWidth(text);
        float bob = Mathf.Sin(Time.time * bobSpeed) * bobAmplitude;
        DrawPrompt(new Vector2(screen.x - width * 0.5f, Screen.height - screen.y - (keySize + 16f) + bob), text);
    }

    float PromptWidth(string text) => keySize + 26f + textStyle.CalcSize(new GUIContent(text)).x + 8f;

    // Dibuja un panel oscuro con una tecla y el texto de la acción; topLeft en coordenadas de GUI.
    void DrawPrompt(Vector2 topLeft, string text)
    {
        float width = PromptWidth(text);
        float height = keySize + 16f;
        var panel = new Rect(topLeft.x, topLeft.y, width, height);

        var previous = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, 0.6f);
        GUI.DrawTexture(panel, Texture2D.whiteTexture);

        // Tecla con "sombra" inferior para que parezca una tecla física.
        var key = new Rect(panel.x + 8f, panel.y + 8f, keySize, keySize);
        GUI.color = new Color(0.55f, 0.55f, 0.6f);
        GUI.DrawTexture(key, Texture2D.whiteTexture);
        GUI.color = new Color(0.96f, 0.96f, 0.96f);
        GUI.DrawTexture(new Rect(key.x, key.y, key.width, key.height - 5f), Texture2D.whiteTexture);
        GUI.color = previous;

        GUI.Label(new Rect(key.x, key.y, key.width, key.height - 5f), keyLabel, keyStyle);
        GUI.Label(new Rect(key.xMax + 12f, panel.y, width - keySize - 20f, height), text, textStyle);
    }

    void CreateStyles()
    {
        keyStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 22,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        keyStyle.normal.textColor = new Color(0.12f, 0.12f, 0.15f);

        textStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 20,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleLeft
        };
        textStyle.normal.textColor = Color.white;
    }
}
