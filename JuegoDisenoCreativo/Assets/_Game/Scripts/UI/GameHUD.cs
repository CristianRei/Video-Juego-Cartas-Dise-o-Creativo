using UnityEngine;

// HUD provisional: contador de objetos recogidos y controles.
// Se reemplazará por UI real cuando estén definidas las reglas del juego.
public class GameHUD : MonoBehaviour
{
    static int coins, keys, potions;

    public static void Add(Collectible.Kind kind)
    {
        switch (kind)
        {
            case Collectible.Kind.Coin: coins++; break;
            case Collectible.Kind.Key: keys++; break;
            case Collectible.Kind.Potion: potions++; break;
        }
    }

    void Awake()
    {
        coins = keys = potions = 0;
    }

    void OnGUI()
    {
        var style = new GUIStyle(GUI.skin.label) { fontSize = 22, fontStyle = FontStyle.Bold };
        style.normal.textColor = Color.white;
        GUI.Label(new Rect(20, 15, 400, 30), $"Monedas: {coins}   Llaves: {keys}   Pociones: {potions}", style);

        var help = new GUIStyle(GUI.skin.label) { fontSize = 14 };
        help.normal.textColor = new Color(1f, 1f, 1f, 0.8f);
        GUI.Label(new Rect(20, Screen.height - 30, 900, 25),
            "WASD mover · Shift correr · Espacio saltar · Clic izq. atacar · E abrir · Clic der. + mouse girar cámara · Rueda zoom", help);
    }
}
