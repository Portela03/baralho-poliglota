namespace BaralhoPoliglota.Core.Models;

/// <summary>Resultado de uma tradução (MyMemory).</summary>
public sealed record ResultadoTraducao(string Traducao, IReadOnlyList<string> Alternativas);

/// <summary>Dados de dicionário de uma palavra em inglês (Free Dictionary API).</summary>
public sealed record DefinicaoDicionario(
    string Palavra,
    string? ClasseGramatical,
    string Definicao,
    string? Exemplo,
    string? Fonetica,
    string? UrlAudio,
    IReadOnlyList<string> Sinonimos);

/// <summary>Baralho criado na Deck of Cards API.</summary>
public sealed record BaralhoInfo(string DeckId, int Restantes);

/// <summary>Resultado de uma partida, guardado no placar.</summary>
public sealed class ResultadoPartida
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime Data { get; set; } = DateTime.Now;
    public int Pontos { get; set; }
    public int Acertos { get; set; }
    public int TotalRodadas { get; set; }
    public int MelhorSequencia { get; set; }

    public double TaxaDeAcerto => TotalRodadas == 0 ? 0 : (double)Acertos / TotalRodadas;
    public string Resumo => $"{Pontos} pts · {Acertos}/{TotalRodadas} acertos";
}
