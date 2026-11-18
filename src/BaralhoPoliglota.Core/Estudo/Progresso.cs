using BaralhoPoliglota.Core.Models;

namespace BaralhoPoliglota.Core.Estudo;

/// <summary>Números do baralho para a tela inicial de estudo e a de progresso.</summary>
public sealed record Progresso(int Total, int Novas, int ParaHoje, int Aprendendo, int Maduras, int Revisoes, int Acertos)
{
    public double TaxaDeAcerto => Revisoes == 0 ? 0 : (double)Acertos / Revisoes;

    public static Progresso De(IReadOnlyCollection<PalavraSalva> palavras, DateTime agora) => new(
        Total: palavras.Count,
        Novas: palavras.Count(p => p.EhNova),
        ParaHoje: palavras.Count(p => p.Pendente(agora)),
        Aprendendo: palavras.Count(p => !p.EhNova && !p.Madura),
        Maduras: palavras.Count(p => p.Madura),
        Revisoes: palavras.Sum(p => p.TotalRevisoes),
        Acertos: palavras.Sum(p => p.TotalAcertos));
}
