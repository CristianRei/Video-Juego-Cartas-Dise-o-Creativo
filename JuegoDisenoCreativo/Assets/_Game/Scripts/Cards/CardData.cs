using System.Collections.Generic;

// Tipos de carta del Reglamento v1.0 (ver Docs/Reglamento.md).
public enum CardKind
{
    Number,
    Reverse,            // Cambio de sentido
    InvertComparison,   // Invertir comparación
    Skip,               // Salto
    SetValue            // Fijar valor
}

[System.Serializable]
public struct CardData
{
    public CardKind kind;
    public int value; // 1-10 solo para cartas numéricas

    public CardData(CardKind kind, int value = 0)
    {
        this.kind = kind;
        this.value = value;
    }

    public bool IsWildcard => kind != CardKind.Number;

    // Valor para el desempate por bloqueo: cada comodín cuenta 10.
    public int TieBreakValue => IsWildcard ? 10 : value;

    public string ShortLabel => kind switch
    {
        CardKind.Number => value.ToString(),
        CardKind.Reverse => "Sentido",
        CardKind.InvertComparison => "Invertir",
        CardKind.Skip => "Salto",
        CardKind.SetValue => "Fijar",
        _ => "?"
    };

    public override string ToString() => kind == CardKind.Number ? value.ToString() : ShortLabel;

    // Mazo completo: números 1-10 ×4 (40) + 4 tipos de comodín ×4 (16) = 56 cartas.
    public static List<CardData> CreateFullDeck()
    {
        var deck = new List<CardData>(56);
        for (int copy = 0; copy < 4; copy++)
        {
            for (int v = 1; v <= 10; v++) deck.Add(new CardData(CardKind.Number, v));
            deck.Add(new CardData(CardKind.Reverse));
            deck.Add(new CardData(CardKind.InvertComparison));
            deck.Add(new CardData(CardKind.Skip));
            deck.Add(new CardData(CardKind.SetValue));
        }
        return deck;
    }

    // Solo números 1-10 ×4 (40 cartas), sin comodines.
    public static List<CardData> CreateNumberDeck()
    {
        var deck = new List<CardData>(40);
        for (int copy = 0; copy < 4; copy++)
            for (int v = 1; v <= 10; v++) deck.Add(new CardData(CardKind.Number, v));
        return deck;
    }

    public static void Shuffle(List<CardData> cards, System.Random rng)
    {
        for (int i = cards.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (cards[i], cards[j]) = (cards[j], cards[i]);
        }
    }
}
