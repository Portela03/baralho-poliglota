using BaralhoPoliglota.Core.Jogo;
using BaralhoPoliglota.Core.Models;

namespace BaralhoPoliglota.Tests;

public class CartaTests
{
    [Theory]
    [InlineData("ACE", 1, 11)]
    [InlineData("2", 2, 2)]
    [InlineData("10", 10, 10)]
    [InlineData("JACK", 11, 10)]
    [InlineData("QUEEN", 12, 10)]
    [InlineData("KING", 13, 10)]
    public void Valor_e_pontos_seguem_a_regra(string valor, int numerico, int pontos)
    {
        var carta = Fabrica.Carta(valor, "SPADES");

        Assert.Equal(numerico, carta.ValorNumerico);
        Assert.Equal(pontos, carta.Pontos);
    }

    [Theory]
    [InlineData("SPADES", 0, false)]
    [InlineData("HEARTS", 1, true)]
    [InlineData("DIAMONDS", 2, true)]
    [InlineData("CLUBS", 3, false)]
    public void Naipe_define_indice_e_cor(string naipe, int indice, bool vermelha)
    {
        var carta = Fabrica.Carta("5", naipe);

        Assert.Equal(indice, carta.NaipeIndice);
        Assert.Equal(vermelha, carta.EhVermelha);
    }

    [Fact]
    public void Valor_desconhecido_lanca_excecao()
    {
        Assert.Throws<InvalidOperationException>(() => Fabrica.Carta("JOKER", "HEARTS").ValorNumerico);
    }
}

public class NormalizadorTests
{
    [Theory]
    [InlineData("  Água  ", "agua")]
    [InlineData("CAFÉ  com   Leite", "cafe com leite")]
    [InlineData("coração", "coracao")]
    [InlineData("", "")]
    [InlineData(null, "")]
    public void Remove_acentos_caixa_e_espacos(string? entrada, string esperado) =>
        Assert.Equal(esperado, Normalizador.Normalizar(entrada));
}

public class PartidaTests
{
    [Fact]
    public void Acertos_seguidos_dao_bonus_ate_o_limite()
    {
        var partida = new Partida(10);
        var carta = Fabrica.Carta("5", "HEARTS"); // 5 pontos

        var ganhos = Enumerable.Range(0, 8).Select(_ => partida.Registrar(carta, true)).ToList();

        // sequência 1..8 → bônus 0,1,2,3,4,5,5,5
        Assert.Equal([5, 6, 7, 8, 9, 10, 10, 10], ganhos);
        Assert.Equal(8, partida.MelhorSequencia);
        Assert.Equal(65, partida.Pontos);
    }

    [Fact]
    public void Erro_zera_sequencia_e_pontos_da_rodada()
    {
        var partida = new Partida(10);
        var carta = Fabrica.Carta("ACE", "CLUBS"); // 11 pontos

        partida.Registrar(carta, true);
        partida.Registrar(carta, true);
        var ganho = partida.Registrar(carta, false);
        var depois = partida.Registrar(carta, true);

        Assert.Equal(0, ganho);
        Assert.Equal(11, depois); // sem bônus: a sequência recomeçou
        Assert.Equal(2, partida.MelhorSequencia);
        Assert.Equal(3, partida.Acertos);
    }

    [Fact]
    public void Partida_finalizada_nao_aceita_mais_respostas()
    {
        var partida = new Partida(1);
        partida.Registrar(Fabrica.Carta("2", "SPADES"), true);

        Assert.True(partida.Finalizada);
        Assert.Throws<InvalidOperationException>(() => partida.Registrar(Fabrica.Carta("2", "SPADES"), true));
    }

    [Fact]
    public void Gera_resultado_para_o_placar()
    {
        var partida = new Partida(2);
        partida.Registrar(Fabrica.Carta("KING", "HEARTS"), true);
        partida.Registrar(Fabrica.Carta("3", "CLUBS"), false);

        var resultado = partida.ParaResultado();

        Assert.Equal(10, resultado.Pontos);
        Assert.Equal(1, resultado.Acertos);
        Assert.Equal(2, resultado.TotalRodadas);
        Assert.Equal(0.5, resultado.TaxaDeAcerto);
    }
}

public class PerguntaTests
{
    private static readonly Carta Carta = Fabrica.Carta("5", "CLUBS");

    [Fact]
    public void Digitada_ignora_acento_caixa_e_aceita_alternativas()
    {
        var palavra = Fabrica.Palavra("water", "água", "agua potável");
        var pergunta = new PerguntaDigitada(Carta, palavra);

        Assert.True(pergunta.Verificar("  AGUA "));
        Assert.True(pergunta.Verificar("Água Potável"));
        Assert.False(pergunta.Verificar("fogo"));
        Assert.False(pergunta.Verificar(""));
        Assert.False(pergunta.Verificar(null));
    }

    [Fact]
    public void Multipla_escolha_aceita_so_a_traducao_principal()
    {
        var palavra = Fabrica.Palavra("dog", "cachorro", "cão");
        Pergunta pergunta = new PerguntaMultiplaEscolha(Carta, palavra, ["gato", "cachorro", "pato", "boi"]);

        Assert.Equal(TipoPergunta.MultiplaEscolha, pergunta.Tipo);
        Assert.True(pergunta.Verificar("Cachorro"));
        Assert.False(pergunta.Verificar("cão"));
    }

    [Fact]
    public void Enunciado_cita_a_palavra_e_o_idioma_de_destino()
    {
        var pergunta = new PerguntaDigitada(Carta, Fabrica.Palavra("book", "livro"));

        Assert.Contains("book", pergunta.Enunciado);
        Assert.Contains("Português", pergunta.Enunciado);
    }
}

public class MotorDoJogoTests
{
    [Fact]
    public void Exige_pelo_menos_quatro_palavras()
    {
        var palavras = Fabrica.Palavras().Take(3);

        Assert.Throws<InvalidOperationException>(() => new MotorDoJogo(new BaralhoFalso(), palavras));
    }

    [Theory]
    [InlineData("HEARTS", TipoPergunta.MultiplaEscolha)]
    [InlineData("DIAMONDS", TipoPergunta.MultiplaEscolha)]
    [InlineData("SPADES", TipoPergunta.Digitada)]
    [InlineData("CLUBS", TipoPergunta.Digitada)]
    public async Task Naipe_define_o_tipo_da_pergunta(string naipe, TipoPergunta esperado)
    {
        var motor = new MotorDoJogo(new BaralhoFalso(Fabrica.Carta("7", naipe)), Fabrica.Palavras());

        var pergunta = await motor.ProximaPerguntaAsync();

        Assert.Equal(esperado, pergunta.Tipo);
    }

    [Fact]
    public async Task Multipla_escolha_tem_quatro_opcoes_unicas_com_a_correta()
    {
        var motor = new MotorDoJogo(new BaralhoFalso(Fabrica.Carta("7", "HEARTS")), Fabrica.Palavras(), random: new Random(1));

        var pergunta = Assert.IsType<PerguntaMultiplaEscolha>(await motor.ProximaPerguntaAsync());

        Assert.Equal(4, pergunta.Opcoes.Count);
        Assert.Equal(4, pergunta.Opcoes.Select(Normalizador.Normalizar).Distinct().Count());
        Assert.Contains(pergunta.Palavra.Traducao, pergunta.Opcoes);
    }

    [Fact]
    public void Palavra_sorteada_segue_a_formula_da_spec()
    {
        var palavras = Fabrica.Palavras(); // N = 5
        var motor = new MotorDoJogo(new BaralhoFalso(), palavras);
        var carta = Fabrica.Carta("3", "HEARTS"); // valor 3, naipe 1

        // ((3 − 1) × 4 + 1 + rodada 2) mod 5 = 11 mod 5 = 1
        Assert.Same(palavras[1], motor.EscolherPalavra(carta, rodada: 2));
    }

    [Fact]
    public async Task Nao_repete_palavra_enquanto_houver_outra_disponivel()
    {
        var cartas = Enumerable.Repeat(Fabrica.Carta("2", "SPADES"), 5).ToArray();
        var motor = new MotorDoJogo(new BaralhoFalso(cartas), Fabrica.Palavras(), rodadas: 5);
        var ids = new List<Guid>();

        for (var i = 0; i < 5; i++)
        {
            var pergunta = await motor.ProximaPerguntaAsync();
            ids.Add(pergunta.Palavra.Id);
            motor.Responder(pergunta, pergunta.RespostaCorreta);
        }

        Assert.Equal(5, ids.Distinct().Count());
    }

    [Fact]
    public async Task Cria_o_baralho_uma_vez_e_fecha_a_partida_apos_as_rodadas()
    {
        var baralho = new BaralhoFalso(Fabrica.Carta("4", "CLUBS"), Fabrica.Carta("9", "SPADES"));
        var motor = new MotorDoJogo(baralho, Fabrica.Palavras(), rodadas: 2);

        for (var i = 0; i < 2; i++)
        {
            var pergunta = await motor.ProximaPerguntaAsync();
            motor.Responder(pergunta, "errado");
        }

        Assert.Equal(1, baralho.BaralhosCriados);
        Assert.True(motor.Partida.Finalizada);
        Assert.Equal(0, motor.Partida.Pontos);
        await Assert.ThrowsAsync<InvalidOperationException>(() => motor.ProximaPerguntaAsync());
    }

    [Fact]
    public async Task Responder_certo_pontua_com_o_valor_da_carta()
    {
        var motor = new MotorDoJogo(new BaralhoFalso(Fabrica.Carta("8", "CLUBS")), Fabrica.Palavras());
        var pergunta = await motor.ProximaPerguntaAsync();

        var ganho = motor.Responder(pergunta, pergunta.RespostaCorreta);

        Assert.Equal(8, ganho);
        Assert.Equal(1, motor.Partida.Acertos);
    }
}
