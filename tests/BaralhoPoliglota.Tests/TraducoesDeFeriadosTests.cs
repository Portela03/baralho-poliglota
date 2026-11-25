using BaralhoPoliglota.Core.Estudo;
using BaralhoPoliglota.Core.Models;
using Xunit;

namespace BaralhoPoliglota.Tests;

public class TraducoesDeFeriadosTests
{
    [Theory]
    [InlineData("Natal", "en", "Christmas")]
    [InlineData("natal", "de", "Weihnachten")]
    [InlineData("Páscoa", "fr", "Pâques")]
    [InlineData("Dia da consciência negra", "es", "Día de la Conciencia Negra")]
    public void Busca_traducao_fixa_ignorando_caixa_e_acentos(string nome, string idioma, string esperado) =>
        Assert.Equal(esperado, TraducoesDeFeriados.Buscar(nome, idioma));

    [Fact]
    public void Nome_ou_idioma_desconhecido_devolve_nulo()
    {
        Assert.Null(TraducoesDeFeriados.Buscar("Feriado inventado", "en"));
        Assert.Null(TraducoesDeFeriados.Buscar("Natal", "ja"));
    }

    [Fact]
    public void Todos_os_idiomas_estudaveis_tem_traducao_para_todos_os_feriados()
    {
        string[] nomes = ["Confraternização mundial", "Carnaval", "Sexta-feira Santa", "Páscoa", "Tiradentes",
            "Dia do trabalho", "Corpus Christi", "Independência do Brasil", "Nossa Senhora Aparecida",
            "Finados", "Proclamação da República", "Dia da consciência negra", "Natal"];

        foreach (var idioma in Idioma.Suportados.Where(i => i.Codigo != "pt"))
            foreach (var nome in nomes)
                Assert.False(string.IsNullOrWhiteSpace(TraducoesDeFeriados.Buscar(nome, idioma.Codigo)), $"{nome}/{idioma.Codigo}");
    }
}
