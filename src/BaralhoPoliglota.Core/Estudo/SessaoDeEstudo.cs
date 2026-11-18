using BaralhoPoliglota.Core.Models;

namespace BaralhoPoliglota.Core.Estudo;

/// <summary>
/// Uma rodada de estudo: percorre as cartas pendentes. Quem errou volta para o fim da fila
/// e só sai da sessão quando for lembrado.
/// </summary>
public sealed class SessaoDeEstudo
{
    public const int LimitePorSessao = 30;

    private readonly Queue<PalavraSalva> _fila;
    private readonly Func<DateTime> _agora;

    public SessaoDeEstudo(IEnumerable<PalavraSalva> palavras, Func<DateTime>? agora = null, int limite = LimitePorSessao)
    {
        _agora = agora ?? (() => DateTime.Now);
        var agoraAtual = _agora();

        // Cartas já estudadas e vencidas primeiro; depois as novas.
        var pendentes = palavras.Where(p => p.Pendente(agoraAtual))
            .OrderBy(p => p.EhNova)
            .ThenBy(p => p.ProximaRevisao)
            .Take(limite);

        _fila = new Queue<PalavraSalva>(pendentes);
        Total = _fila.Count;
    }

    public int Total { get; }
    public int Respondidas { get; private set; }
    public int Acertos { get; private set; }
    public int Erros { get; private set; }

    /// <summary>Cartas que ainda faltam (inclui as que voltaram por erro).</summary>
    public int Restantes => _fila.Count;

    public bool Concluida => _fila.Count == 0;

    /// <summary>Carta atual, ou <c>null</c> quando a sessão acabou.</summary>
    public PalavraSalva? Atual => _fila.Count > 0 ? _fila.Peek() : null;

    /// <summary>Registra a avaliação da carta atual e devolve a carta já atualizada (para salvar).</summary>
    public PalavraSalva Avaliar(Avaliacao avaliacao)
    {
        var carta = _fila.Count > 0
            ? _fila.Dequeue()
            : throw new InvalidOperationException("A sessão de estudo já terminou.");

        carta.Revisar(avaliacao, _agora());
        Respondidas++;

        if (avaliacao == Avaliacao.Errei)
        {
            Erros++;
            _fila.Enqueue(carta);
        }
        else
        {
            Acertos++;
        }

        return carta;
    }
}
