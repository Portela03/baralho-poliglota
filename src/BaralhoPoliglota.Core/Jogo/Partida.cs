using BaralhoPoliglota.Core.Models;

namespace BaralhoPoliglota.Core.Jogo;

/// <summary>Estado de uma partida. Pontos, sequência e rodadas só mudam por <see cref="Registrar"/>.</summary>
public sealed class Partida
{
    public const int BonusMaximo = 5;

    public Partida(int totalRodadas)
    {
        if (totalRodadas <= 0)
            throw new ArgumentOutOfRangeException(nameof(totalRodadas));
        TotalRodadas = totalRodadas;
    }

    public int TotalRodadas { get; }
    public int RodadasJogadas { get; private set; }
    public int Pontos { get; private set; }
    public int Acertos { get; private set; }
    public int SequenciaAtual { get; private set; }
    public int MelhorSequencia { get; private set; }

    public bool Finalizada => RodadasJogadas >= TotalRodadas;

    /// <summary>Registra a resposta e devolve os pontos ganhos na rodada.</summary>
    public int Registrar(Carta carta, bool acertou)
    {
        if (Finalizada)
            throw new InvalidOperationException("A partida já terminou.");

        RodadasJogadas++;

        if (!acertou)
        {
            SequenciaAtual = 0;
            return 0;
        }

        Acertos++;
        SequenciaAtual++;
        MelhorSequencia = Math.Max(MelhorSequencia, SequenciaAtual);

        var bonus = SequenciaAtual >= 2 ? Math.Min(SequenciaAtual - 1, BonusMaximo) : 0;
        var ganho = carta.Pontos + bonus;
        Pontos += ganho;
        return ganho;
    }

    public ResultadoPartida ParaResultado() => new()
    {
        Data = DateTime.Now,
        Pontos = Pontos,
        Acertos = Acertos,
        TotalRodadas = TotalRodadas,
        MelhorSequencia = MelhorSequencia,
    };
}
