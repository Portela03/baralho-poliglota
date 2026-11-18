using BaralhoPoliglota.Core.Models;

namespace BaralhoPoliglota.Core.Jogo;

public enum TipoPergunta
{
    MultiplaEscolha,
    Digitada,
}

/// <summary>Pergunta de uma rodada. Cada subclasse define como a resposta é verificada.</summary>
public abstract class Pergunta
{
    protected Pergunta(Carta carta, PalavraSalva palavra)
    {
        Carta = carta;
        Palavra = palavra;
    }

    public Carta Carta { get; }
    public PalavraSalva Palavra { get; }

    public abstract TipoPergunta Tipo { get; }

    public string Enunciado =>
        $"Traduza para {Idioma.NomeDe(Palavra.IdiomaDestino)}: “{Palavra.Texto}”";

    public string RespostaCorreta => Palavra.Traducao;

    public abstract bool Verificar(string? resposta);

    protected static bool Igual(string? a, string? b) =>
        Normalizador.Normalizar(a) == Normalizador.Normalizar(b)
        && !string.IsNullOrEmpty(Normalizador.Normalizar(a));
}

/// <summary>Quatro opções; a carta é de naipe vermelho (Copas/Ouros).</summary>
public sealed class PerguntaMultiplaEscolha : Pergunta
{
    public PerguntaMultiplaEscolha(Carta carta, PalavraSalva palavra, IReadOnlyList<string> opcoes)
        : base(carta, palavra)
    {
        Opcoes = opcoes;
    }

    public IReadOnlyList<string> Opcoes { get; }

    public override TipoPergunta Tipo => TipoPergunta.MultiplaEscolha;

    public override bool Verificar(string? resposta) => Igual(resposta, Palavra.Traducao);
}

/// <summary>O usuário digita a tradução; a carta é de naipe preto (Espadas/Paus).</summary>
public sealed class PerguntaDigitada : Pergunta
{
    public PerguntaDigitada(Carta carta, PalavraSalva palavra) : base(carta, palavra)
    {
    }

    public override TipoPergunta Tipo => TipoPergunta.Digitada;

    public override bool Verificar(string? resposta) =>
        Igual(resposta, Palavra.Traducao) || Palavra.Alternativas.Any(a => Igual(resposta, a));
}
