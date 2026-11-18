namespace BaralhoPoliglota.Core.Estudo;

/// <summary>Resultado do agendamento de uma carta após uma avaliação.</summary>
public readonly record struct Agendamento(double Facilidade, int Repeticoes, int IntervaloDias, DateTime ProximaRevisao);

/// <summary>
/// Repetição espaçada simplificada, inspirada no SM-2 (a base do Anki).
/// Quanto melhor a lembrança, maior o intervalo até a carta voltar.
/// </summary>
public static class Agendador
{
    public const double FacilidadeInicial = 2.5;
    public const double FacilidadeMinima = 1.3;

    public static Agendamento Calcular(
        double facilidade, int repeticoes, int intervaloDias, Avaliacao avaliacao, DateTime agora)
    {
        var hoje = agora.Date;

        switch (avaliacao)
        {
            case Avaliacao.Errei:
                // Volta a ser "nova" no ciclo e reaparece já (na mesma sessão).
                return new Agendamento(Math.Max(FacilidadeMinima, facilidade - 0.2), 0, 0, agora);

            case Avaliacao.Dificil:
            {
                var intervalo = repeticoes == 0 ? 1 : Math.Max(intervaloDias + 1, (int)Math.Round(intervaloDias * 1.2));
                return new Agendamento(Math.Max(FacilidadeMinima, facilidade - 0.15), repeticoes + 1, intervalo, hoje.AddDays(intervalo));
            }

            case Avaliacao.Bom:
            {
                var intervalo = repeticoes switch
                {
                    0 => 1,
                    1 => 3,
                    _ => Math.Max(intervaloDias + 1, (int)Math.Round(intervaloDias * facilidade)),
                };
                return new Agendamento(facilidade, repeticoes + 1, intervalo, hoje.AddDays(intervalo));
            }

            case Avaliacao.Facil:
            {
                var intervalo = repeticoes == 0
                    ? 4
                    : Math.Max(intervaloDias + 2, (int)Math.Round(intervaloDias * facilidade * 1.3));
                return new Agendamento(facilidade + 0.15, repeticoes + 1, intervalo, hoje.AddDays(intervalo));
            }

            default:
                throw new ArgumentOutOfRangeException(nameof(avaliacao));
        }
    }

    /// <summary>Texto curto para o botão, como o Anki mostra ("1 dia", "3 dias", "2 meses").</summary>
    public static string TextoDoIntervalo(int dias) => dias switch
    {
        <= 0 => "agora",
        1 => "1 dia",
        < 30 => $"{dias} dias",
        < 365 => $"{Math.Round(dias / 30.0):0} meses",
        _ => $"{dias / 365.0:0.#} anos",
    };
}
