namespace BaralhoPoliglota.Core.Models;

/// <summary>Palavra com a sua tradução. Classe base abstrata das palavras do app.</summary>
public abstract class Palavra
{
    public string Texto { get; set; } = string.Empty;
    public string Traducao { get; set; } = string.Empty;
    public string IdiomaOrigem { get; set; } = "en";
    public string IdiomaDestino { get; set; } = "pt";

    /// <summary>Chave que identifica a palavra (texto + par de idiomas), usada para evitar duplicatas.</summary>
    public string Chave => $"{IdiomaOrigem}|{IdiomaDestino}|{Normalizador.Normalizar(Texto)}";

    /// <summary>Texto curto para mostrar em listas. Subclasses podem personalizar.</summary>
    public virtual string Descricao => $"{Texto} → {Traducao}";

    public string ParDeIdiomas => $"{IdiomaOrigem.ToUpperInvariant()} → {IdiomaDestino.ToUpperInvariant()}";
}
