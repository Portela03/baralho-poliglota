using BaralhoPoliglota.Core.Estudo;

namespace BaralhoPoliglota.Core.Models;

/// <summary>Palavra guardada no baralho do usuário, com dados extras do dicionário.</summary>
public class PalavraSalva : Palavra
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime SalvaEm { get; set; } = DateTime.UtcNow;
    public bool Favorita { get; set; }

    /// <summary>Outras traduções aceitas (usadas na pergunta digitada).</summary>
    public List<string> Alternativas { get; set; } = [];

    /// <summary>Palavra em inglês a que a definição se refere (pode ser o texto ou a tradução).</summary>
    public string? DefinicaoDe { get; set; }

    public string? ClasseGramatical { get; set; }
    public string? Definicao { get; set; }
    public string? Exemplo { get; set; }
    public string? Fonetica { get; set; }
    public string? UrlAudio { get; set; }

    // --- Estado da repetição espaçada (só muda por Revisar) ------------------------------------

    public double Facilidade { get; set; } = Agendador.FacilidadeInicial;
    public int Repeticoes { get; set; }
    public int IntervaloDias { get; set; }

    /// <summary><c>null</c> = carta nova, nunca estudada.</summary>
    public DateTime? ProximaRevisao { get; set; }

    public int TotalRevisoes { get; set; }
    public int TotalAcertos { get; set; }

    public bool EhNova => ProximaRevisao is null;

    /// <summary>Confere a resposta digitada (sem acento, caixa ou espaços nas pontas) com a tradução e as alternativas.</summary>
    public bool AceitaResposta(string? resposta)
    {
        var digitada = Normalizador.Normalizar(resposta);
        return digitada.Length > 0 &&
               new[] { Traducao }.Concat(Alternativas).Any(t => Normalizador.Normalizar(t) == digitada);
    }

    /// <summary>Situação da carta no estudo, para a lista do baralho.</summary>
    public string StatusEstudo => EhNova
        ? "Nova"
        : Pendente(DateTime.Now) ? "Para revisar hoje" : $"Próxima revisão: {ProximaRevisao:dd/MM}";
    public bool Pendente(DateTime agora) => ProximaRevisao is null || ProximaRevisao <= agora;

    /// <summary>Carta "madura" no jargão do Anki: intervalo de 21 dias ou mais.</summary>
    public bool Madura => IntervaloDias >= 21;

    /// <summary>Intervalo (em dias) que a carta teria se fosse avaliada assim; usado nos botões.</summary>
    public int Prever(Avaliacao avaliacao, DateTime agora) =>
        Agendador.Calcular(Facilidade, Repeticoes, IntervaloDias, avaliacao, agora).IntervaloDias;

    public void Revisar(Avaliacao avaliacao, DateTime agora)
    {
        var a = Agendador.Calcular(Facilidade, Repeticoes, IntervaloDias, avaliacao, agora);
        Facilidade = a.Facilidade;
        Repeticoes = a.Repeticoes;
        IntervaloDias = a.IntervaloDias;
        ProximaRevisao = a.ProximaRevisao;
        TotalRevisoes++;
        if (avaliacao != Avaliacao.Errei)
            TotalAcertos++;
    }

    public bool TemDefinicao => !string.IsNullOrWhiteSpace(Definicao);
    public bool TemAudio => !string.IsNullOrWhiteSpace(UrlAudio);

    public override string Descricao => Favorita ? $"★ {base.Descricao}" : base.Descricao;

    public static PalavraSalva Criar(
        string texto,
        string traducao,
        string origem,
        string destino,
        IEnumerable<string>? alternativas = null,
        DefinicaoDicionario? definicao = null)
    {
        var palavra = new PalavraSalva
        {
            Texto = texto.Trim(),
            Traducao = traducao.Trim(),
            IdiomaOrigem = origem,
            IdiomaDestino = destino,
            Alternativas = alternativas?.ToList() ?? [],
        };

        if (definicao is not null)
        {
            palavra.DefinicaoDe = definicao.Palavra;
            palavra.ClasseGramatical = definicao.ClasseGramatical;
            palavra.Definicao = definicao.Definicao;
            palavra.Exemplo = definicao.Exemplo;
            palavra.Fonetica = definicao.Fonetica;
            palavra.UrlAudio = definicao.UrlAudio;
        }

        return palavra;
    }
}
