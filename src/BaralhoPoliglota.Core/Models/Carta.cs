namespace BaralhoPoliglota.Core.Models;

/// <summary>Carta sacada da Deck of Cards API.</summary>
public sealed record Carta(string Codigo, string Valor, string Naipe, string UrlImagem)
{
    /// <summary>Posição da carta: A=1, 2–10, J=11, Q=12, K=13.</summary>
    public int ValorNumerico => Valor.ToUpperInvariant() switch
    {
        "ACE" => 1,
        "JACK" => 11,
        "QUEEN" => 12,
        "KING" => 13,
        var v when int.TryParse(v, out var n) && n is >= 2 and <= 10 => n,
        _ => throw new InvalidOperationException($"Valor de carta desconhecido: '{Valor}'."),
    };

    /// <summary>Pontos da carta: A=11, 2–10 = valor, J/Q/K = 10.</summary>
    public int Pontos => ValorNumerico switch
    {
        1 => 11,
        > 10 => 10,
        var n => n,
    };

    /// <summary>Espadas=0, Copas=1, Ouros=2, Paus=3.</summary>
    public int NaipeIndice => Naipe.ToUpperInvariant() switch
    {
        "SPADES" => 0,
        "HEARTS" => 1,
        "DIAMONDS" => 2,
        "CLUBS" => 3,
        _ => throw new InvalidOperationException($"Naipe desconhecido: '{Naipe}'."),
    };

    public bool EhVermelha => NaipeIndice is 1 or 2;

    public string SimboloNaipe => NaipeIndice switch { 0 => "♠", 1 => "♥", 2 => "♦", _ => "♣" };

    public string NomeValor => Valor.ToUpperInvariant() switch
    {
        "ACE" => "Ás",
        "JACK" => "Valete",
        "QUEEN" => "Dama",
        "KING" => "Rei",
        _ => Valor,
    };

    public string NomeNaipe => NaipeIndice switch { 0 => "Espadas", 1 => "Copas", 2 => "Ouros", _ => "Paus" };

    public string Descricao => $"{NomeValor} de {NomeNaipe} {SimboloNaipe}";
}
