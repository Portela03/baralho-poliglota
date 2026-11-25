namespace BaralhoPoliglota.Core.Models;

/// <summary>Resultado de uma tradução (MyMemory).</summary>
public sealed record ResultadoTraducao(
    string Traducao, IReadOnlyList<string> Alternativas, string? MelhorPontuada = null)
{
    /// <summary>
    /// Torna <paramref name="escolhida"/> a tradução principal (a antiga principal vira alternativa).
    /// Se ela não estiver entre as opções, devolve o resultado sem mudanças.
    /// </summary>
    public ResultadoTraducao ComPrincipal(string? escolhida)
    {
        var todas = new[] { Traducao }.Concat(Alternativas).ToList();
        var alvo = todas.FirstOrDefault(t => Normalizador.Normalizar(t) == Normalizador.Normalizar(escolhida));
        if (alvo is null)
            return this;

        var indice = todas.IndexOf(alvo);
        todas.RemoveAt(indice);
        return new ResultadoTraducao(alvo, todas);
    }
}

/// <summary>Dados de dicionário de uma palavra em inglês (Free Dictionary API).</summary>
public sealed record DefinicaoDicionario(
    string Palavra,
    string? ClasseGramatical,
    string Definicao,
    string? Exemplo,
    string? Fonetica,
    string? UrlAudio,
    IReadOnlyList<string> Sinonimos);

/// <summary>Feriado nacional devolvido pela BrasilAPI.</summary>
public sealed record Feriado(DateOnly Data, string Nome, string DiaDaSemana);
