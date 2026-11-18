using BaralhoPoliglota.Core.Models;

namespace BaralhoPoliglota.Tests;

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

public class ResultadoTraducaoTests
{
    [Fact]
    public void Escolher_alternativa_troca_com_a_principal_ignorando_acento_e_caixa()
    {
        var resultado = new ResultadoTraducao("eno", ["dom", "sol", "astro"]);

        var novo = resultado.ComPrincipal("SOL");

        Assert.Equal("sol", novo.Traducao);
        Assert.Equal(["eno", "dom", "astro"], novo.Alternativas);
    }

    [Fact]
    public void Opcao_desconhecida_nao_altera_o_resultado()
    {
        var resultado = new ResultadoTraducao("casa", ["lar"]);

        Assert.Same(resultado, resultado.ComPrincipal("carro"));
        Assert.Same(resultado, resultado.ComPrincipal(null));
    }
}

