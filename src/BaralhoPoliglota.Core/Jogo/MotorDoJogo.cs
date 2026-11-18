using BaralhoPoliglota.Core.Models;
using BaralhoPoliglota.Core.Services;

namespace BaralhoPoliglota.Core.Jogo;

/// <summary>
/// Monta as rodadas: saca uma carta na Deck of Cards API e usa naipe e valor para decidir
/// o tipo da pergunta, a palavra sorteada e a pontuação.
/// </summary>
public sealed class MotorDoJogo
{
    public const int MinimoDePalavras = 4;
    public const int RodadasPorPartida = 10;
    private const int OpcoesPorPergunta = 4;

    private readonly IBaralhoService _baralho;
    private readonly IReadOnlyList<PalavraSalva> _palavras;
    private readonly Random _random;
    private readonly HashSet<Guid> _usadas = [];
    private string? _deckId;

    public MotorDoJogo(
        IBaralhoService baralho,
        IEnumerable<PalavraSalva> palavras,
        int rodadas = RodadasPorPartida,
        Random? random = null)
    {
        _baralho = baralho;
        _palavras = palavras.ToList();
        _random = random ?? Random.Shared;

        if (_palavras.Count < MinimoDePalavras)
            throw new InvalidOperationException(
                $"São necessárias pelo menos {MinimoDePalavras} palavras no baralho para jogar.");

        Partida = new Partida(rodadas);
    }

    public Partida Partida { get; }

    /// <summary>Saca a próxima carta e devolve a pergunta correspondente.</summary>
    public async Task<Pergunta> ProximaPerguntaAsync(CancellationToken ct = default)
    {
        if (Partida.Finalizada)
            throw new InvalidOperationException("A partida já terminou.");

        _deckId ??= (await _baralho.NovoBaralhoAsync(ct)).DeckId;
        var carta = await _baralho.SacarAsync(_deckId, ct);
        var palavra = EscolherPalavra(carta, Partida.RodadasJogadas);
        _usadas.Add(palavra.Id);

        return carta.EhVermelha
            ? new PerguntaMultiplaEscolha(carta, palavra, MontarOpcoes(palavra))
            : new PerguntaDigitada(carta, palavra);
    }

    /// <summary>Confere a resposta, atualiza a partida e devolve os pontos ganhos.</summary>
    public int Responder(Pergunta pergunta, string? resposta) =>
        Partida.Registrar(pergunta.Carta, pergunta.Verificar(resposta));

    /// <summary>((valor − 1) × 4 + naipe + rodada) mod N, pulando as palavras já usadas na partida.</summary>
    public PalavraSalva EscolherPalavra(Carta carta, int rodada)
    {
        var total = _palavras.Count;
        if (_usadas.Count >= total)
            _usadas.Clear();

        var indice = ((carta.ValorNumerico - 1) * 4 + carta.NaipeIndice + rodada) % total;
        while (_usadas.Contains(_palavras[indice].Id))
            indice = (indice + 1) % total;

        return _palavras[indice];
    }

    private List<string> MontarOpcoes(PalavraSalva correta)
    {
        var opcoes = new List<string> { correta.Traducao };
        var vistas = new HashSet<string> { Normalizador.Normalizar(correta.Traducao) };

        var candidatas = _palavras
            .Where(p => p.Id != correta.Id)
            .Select(p => p.Traducao)
            .OrderBy(_ => _random.Next())
            .ToList();

        foreach (var candidata in candidatas)
        {
            if (opcoes.Count >= OpcoesPorPergunta)
                break;
            if (vistas.Add(Normalizador.Normalizar(candidata)))
                opcoes.Add(candidata);
        }

        return opcoes.OrderBy(_ => _random.Next()).ToList();
    }
}
