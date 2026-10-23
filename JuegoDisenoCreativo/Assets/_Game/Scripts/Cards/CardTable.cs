using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// Mesa de cartas (prototipo visual, todavía sin reglas).
// En el centro están el mazo (para agarrar) y, al lado, el montón de cartas jugadas.
// Al sentarse el jugador se revela la carta inicial y se reparten 4 cartas a cada uno.
// Sentado: clic en una carta de la mano = ponerla en el montón; clic en el mazo = agarrar una;
// clic en la carta de arriba del montón = recogerla. Al levantarse, todo vuelve al mazo.
[DefaultExecutionOrder(-10)]
public class CardTable : MonoBehaviour
{
    [Header("Asientos")]
    public Seat playerSeat;
    public Seat opponentSeat;

    [Header("Mazo")]
    [Tooltip("Por ahora solo cartas numéricas 1-10 (40). Desactivar para usar el mazo completo de 56 con comodines.")]
    public bool numbersOnly = true;

    [Header("Mesa")]
    [Tooltip("Altura de la superficie de la mesa sobre su pivote.")]
    public float tableTopHeight = 0.316f;
    [Tooltip("Distancia del mazo y del montón al centro de la mesa (uno a cada lado).")]
    public float centerSpacing = 0.08f;
    [Tooltip("Distancia del centro a la que el rival deja su mano boca abajo.")]
    public float opponentHandDistance = 0.28f;

    [Header("Mano del jugador (relativa a la cámara)")]
    public int handSize = 4;
    public float handDistance = 0.45f;
    public float handDrop = 0.17f;
    public float handSpacing = 0.095f;
    public float maxHandWidth = 0.5f;
    public float fanAngle = 25f;
    public float hoverLift = 0.025f;

    [Header("Animación")]
    public float startDelay = 0.6f;
    public float dealInterval = 0.15f;

    const float DeckCardHeight = 0.0008f;

    readonly List<CardData> deck = new List<CardData>();
    readonly List<CardView> hand = new List<CardView>();
    readonly List<CardView> opponentHand = new List<CardView>();
    readonly List<CardView> pile = new List<CardView>();
    readonly System.Random rng = new System.Random();

    Transform deckVisual, deckBody;
    CardView deckTopCard;
    PlayerController seatedPlayer;
    CardView hovered;
    bool dealing, returning;

    public int DeckCount => deck.Count;
    public CardView PileTop => pile.Count > 0 ? pile[pile.Count - 1] : null;
    public IReadOnlyList<CardView> Hand => hand;

    // ---------- Geometría de la mesa ----------

    Vector3 Forward => playerSeat != null
        ? Vector3.ProjectOnPlane(playerSeat.transform.forward, Vector3.up).normalized
        : transform.forward;
    Vector3 Left => Vector3.Cross(Forward, Vector3.up);
    Vector3 Center => transform.position + Vector3.up * tableTopHeight;
    Vector3 DeckPosition => Center + Left * centerSpacing;
    Vector3 PilePosition => Center - Left * centerSpacing;
    Quaternion FaceUp => Quaternion.LookRotation(Vector3.down, Forward);
    Quaternion FaceDown => Quaternion.LookRotation(Vector3.up, Forward);
    Vector3 DeckTop => DeckPosition + Vector3.up * (deck.Count * DeckCardHeight + CardView.Thickness * 0.5f);

    // ---------- Ciclo de vida ----------

    void Awake()
    {
        ResetDeck();
        CreateDeckVisual();
    }

    void OnEnable()
    {
        if (playerSeat == null) return;
        playerSeat.PlayerSat += OnPlayerSat;
        playerSeat.PlayerStoodUp += OnPlayerStoodUp;
    }

    void OnDisable()
    {
        if (playerSeat == null) return;
        playerSeat.PlayerSat -= OnPlayerSat;
        playerSeat.PlayerStoodUp -= OnPlayerStoodUp;
    }

    void OnPlayerSat(PlayerController player)
    {
        seatedPlayer = player;
        StartCoroutine(StartRound());
    }

    void OnPlayerStoodUp(PlayerController player)
    {
        seatedPlayer = null;
        hovered = null;
        StopAllCoroutines();
        dealing = false;
        StartCoroutine(ReturnAllToDeck());
    }

    // ---------- Reparto ----------

    IEnumerator StartRound()
    {
        dealing = true;
        while (returning) yield return null;
        yield return new WaitForSeconds(startDelay);

        // Carta inicial: la primera numérica desde arriba del mazo (Reglamento §3.4).
        int index = deck.FindLastIndex(c => !c.IsWildcard);
        if (index >= 0)
        {
            var data = deck[index];
            deck.RemoveAt(index);
            UpdateDeckVisual();
            PlaceOnPile(CardView.Create(data, DeckTop, FaceDown));
            yield return new WaitForSeconds(0.35f);
        }

        for (int i = 0; i < handSize; i++)
        {
            DrawToHand();
            yield return new WaitForSeconds(dealInterval);
            DealToOpponent();
            yield return new WaitForSeconds(dealInterval);
        }
        dealing = false;
    }

    CardView TakeFromDeck()
    {
        if (deck.Count == 0) return null;
        var data = deck[deck.Count - 1];
        deck.RemoveAt(deck.Count - 1);
        var view = CardView.Create(data, DeckTop, FaceDown);
        UpdateDeckVisual();
        return view;
    }

    void DrawToHand()
    {
        var view = TakeFromDeck();
        if (view == null) return;
        hand.Add(view);
        HandSlot(hand.Count - 1, hand.Count, false, out var pos, out var rot);
        view.FlyTo(pos, rot, 0.4f, 0.12f);
    }

    void DealToOpponent()
    {
        var view = TakeFromDeck();
        if (view == null) return;
        opponentHand.Add(view);
        for (int i = 0; i < opponentHand.Count; i++)
        {
            OpponentSlot(i, opponentHand.Count, out var pos, out var rot);
            if (opponentHand[i] == view) view.FlyTo(pos, rot, 0.35f, 0.06f);
            else opponentHand[i].SetTarget(pos, rot);
        }
    }

    void PlaceOnPile(CardView view)
    {
        pile.Add(view);
        Vector3 pos = PilePosition + Vector3.up * (pile.Count * CardView.Thickness * 0.8f);
        Quaternion rot = Quaternion.AngleAxis(Random.Range(-15f, 15f), Vector3.up) * FaceUp;
        view.FlyTo(pos, rot, 0.35f, 0.1f);
    }

    void PlayToPile(CardView view)
    {
        hand.Remove(view);
        if (hovered == view) hovered = null;
        PlaceOnPile(view);
    }

    void TakeFromPile()
    {
        var top = PileTop;
        if (top == null) return;
        pile.RemoveAt(pile.Count - 1);
        hand.Add(top);
        HandSlot(hand.Count - 1, hand.Count, false, out var pos, out var rot);
        top.FlyTo(pos, rot, 0.35f, 0.1f);
    }

    IEnumerator ReturnAllToDeck()
    {
        returning = true;
        var all = new List<CardView>();
        all.AddRange(hand);
        all.AddRange(opponentHand);
        all.AddRange(pile);
        hand.Clear();
        opponentHand.Clear();
        pile.Clear();

        foreach (var view in all)
        {
            if (view == null) continue;
            view.FlyTo(DeckTop, FaceDown, 0.35f, 0.08f);
            yield return new WaitForSeconds(0.03f);
        }
        yield return new WaitForSeconds(0.4f);
        foreach (var view in all)
            if (view != null) Destroy(view.gameObject);

        ResetDeck();
        UpdateDeckVisual();
        returning = false;
    }

    void ResetDeck()
    {
        deck.Clear();
        deck.AddRange(numbersOnly ? CardData.CreateNumberDeck() : CardData.CreateFullDeck());
        CardData.Shuffle(deck, rng);
    }

    // ---------- Mazo visual ----------

    void CreateDeckVisual()
    {
        deckVisual = new GameObject("Deck").transform;
        deckVisual.SetParent(transform, true);
        deckVisual.SetPositionAndRotation(DeckPosition, FaceDown);

        var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
        body.name = "Stack";
        deckBody = body.transform;
        deckBody.SetParent(deckVisual, false);
        body.GetComponent<Renderer>().sharedMaterial = CardView.GetMaterial(CardView.EdgeColor);

        deckTopCard = CardView.Create(new CardData(CardKind.Number, 1), DeckPosition, FaceDown, deckVisual);
        deckTopCard.enabled = false; // es solo decorativa: no se anima
        UpdateDeckVisual();
    }

    void UpdateDeckVisual()
    {
        if (deckVisual == null) return;
        float h = Mathf.Max(deck.Count * DeckCardHeight, 0.0005f);
        deckVisual.gameObject.SetActive(deck.Count > 0);
        deckBody.localScale = new Vector3(CardView.Width, CardView.Height, h);
        deckBody.localPosition = new Vector3(0f, 0f, h * 0.5f);
        deckTopCard.transform.localPosition = new Vector3(0f, 0f, h + CardView.Thickness * 0.5f);
        deckTopCard.transform.localRotation = Quaternion.identity;
    }

    // ---------- Posiciones de las manos ----------

    void HandSlot(int i, int count, bool lifted, out Vector3 pos, out Quaternion rot)
    {
        var cam = Camera.main != null ? Camera.main.transform : transform;
        float spacing = count > 1 ? Mathf.Min(handSpacing, maxHandWidth / (count - 1)) : handSpacing;
        float x = (i - (count - 1) * 0.5f) * spacing;
        pos = cam.position + cam.forward * handDistance - cam.up * handDrop
              + cam.right * x - cam.up * (x * x * 2f)
              - cam.forward * (i * 0.002f); // las de la derecha quedan encima
        if (lifted) pos += cam.up * hoverLift;
        rot = cam.rotation * Quaternion.Euler(0f, 0f, -x * fanAngle);
    }

    void OpponentSlot(int i, int count, out Vector3 pos, out Quaternion rot)
    {
        float x = (i - (count - 1) * 0.5f) * 0.05f;
        pos = Center + Forward * opponentHandDistance - Left * x + Vector3.up * (CardView.Thickness * 0.5f + i * 0.0005f);
        rot = Quaternion.AngleAxis(180f + x * 200f, Vector3.up) * FaceDown;
    }

    // ---------- Entrada ----------

    void Update()
    {
        hovered = null;
        if (seatedPlayer == null || dealing || returning) return;
        var mouse = Mouse.current;
        var cam = Camera.main;
        if (mouse == null || cam == null) return;

        CardView hitCard = null;
        bool hitDeck = false;
        Ray ray = cam.ScreenPointToRay(mouse.position.ReadValue());
        if (Physics.Raycast(ray, out var hit, 3f, ~0, QueryTriggerInteraction.Ignore))
        {
            if (deckVisual != null && hit.transform.IsChildOf(deckVisual)) hitDeck = true;
            else hitCard = hit.collider.GetComponentInParent<CardView>();
        }
        if (hitCard != null && hand.Contains(hitCard)) hovered = hitCard;

        if (!mouse.leftButton.wasPressedThisFrame) return;
        if (hovered != null) PlayToPile(hovered);
        else if (hitDeck) DrawToHand();
        else if (hitCard != null && hitCard == PileTop) TakeFromPile();
    }

    void LateUpdate()
    {
        for (int i = 0; i < hand.Count; i++)
        {
            HandSlot(i, hand.Count, hand[i] == hovered, out var pos, out var rot);
            hand[i].SetTarget(pos, rot);
        }
    }

    // ---------- Ayuda en pantalla (provisional) ----------

    GUIStyle infoStyle, hintStyle;

    void OnGUI()
    {
        if (seatedPlayer == null) return;
        if (infoStyle == null)
        {
            infoStyle = new GUIStyle(GUI.skin.label) { fontSize = 20, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            infoStyle.normal.textColor = Color.white;
            hintStyle = new GUIStyle(infoStyle) { fontSize = 15, fontStyle = FontStyle.Normal };
        }

        string top = PileTop != null ? PileTop.Data.ShortLabel : "—";
        var previous = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, 0.55f);
        GUI.DrawTexture(new Rect(Screen.width * 0.5f - 170f, 12f, 340f, 34f), Texture2D.whiteTexture);
        GUI.color = previous;
        GUI.Label(new Rect(Screen.width * 0.5f - 170f, 12f, 340f, 34f), $"Mazo: {deck.Count}   ·   Mesa: {top}   ·   Mano: {hand.Count}", infoStyle);

        GUI.Label(new Rect(0f, 50f, Screen.width, 24f),
            "Clic en una carta: ponerla en la mesa  ·  Clic en el mazo: agarrar  ·  Clic en la carta de la mesa: recogerla", hintStyle);
    }
}
