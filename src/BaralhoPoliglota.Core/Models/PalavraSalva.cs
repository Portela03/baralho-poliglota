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
