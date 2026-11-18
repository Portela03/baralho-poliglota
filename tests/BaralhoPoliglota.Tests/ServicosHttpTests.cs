using System.Net;
using BaralhoPoliglota.Core.Services;

namespace BaralhoPoliglota.Tests;

public class TraducaoServiceTests
{
    [Fact]
    public async Task Traduz_e_devolve_alternativas_sem_repetir()
    {
        var (http, handler) = HandlerFalso.Json("""
            {"responseData":{"translatedText":"olá","match":1},"quotaFinished":false,"responseStatus":200,
             "matches":[{"translation":"olá"},{"translation":"Alô"},{"translation":"ola"},{"translation":"oi"}]}
            """);

        var resultado = await new TraducaoService(http).TraduzirAsync("hello world", "en", "pt");

        Assert.Equal("olá", resultado.Traducao);
        Assert.Equal(["Alô", "oi"], resultado.Alternativas);
        var url = handler.Requisicoes.Single().AbsoluteUri;
        Assert.Contains("q=hello%20world", url);
        Assert.Contains("langpair=en%7Cpt", url);
    }

    [Fact]
    public async Task Aceita_responseStatus_como_texto_e_decodifica_html()
    {
        var (http, _) = HandlerFalso.Json("""
            {"responseData":{"translatedText":"it&#39;s"},"responseStatus":"200","matches":[]}
            """);

        var resultado = await new TraducaoService(http).TraduzirAsync("x", "pt", "en");

        Assert.Equal("it's", resultado.Traducao);
    }

    [Theory]
    [InlineData("""{"responseData":{"translatedText":"MYMEMORY WARNING"},"responseStatus":429}""")]
    [InlineData("""{"responseData":{"translatedText":"x"},"quotaFinished":true,"responseStatus":200}""")]
    public async Task Cota_esgotada_lanca_excecao_especifica(string json)
    {
        var (http, _) = HandlerFalso.Json(json);

        await Assert.ThrowsAsync<CotaExcedidaException>(() =>
            new TraducaoService(http).TraduzirAsync("hello", "en", "pt"));
    }

    [Fact]
    public async Task Erro_do_servidor_vira_ServicoIndisponivel()
    {
        var (http, _) = HandlerFalso.Json("{}", HttpStatusCode.BadGateway);

        await Assert.ThrowsAsync<ServicoIndisponivelException>(() =>
            new TraducaoService(http).TraduzirAsync("hello", "en", "pt"));
    }

    [Fact]
    public async Task Falha_de_rede_vira_ServicoIndisponivel()
    {
        var http = HandlerFalso.Falha(new HttpRequestException("sem rede"));

        await Assert.ThrowsAsync<ServicoIndisponivelException>(() =>
            new TraducaoService(http).TraduzirAsync("hello", "en", "pt"));
    }

    [Fact]
    public async Task Timeout_vira_ServicoIndisponivel()
    {
        var http = HandlerFalso.Falha(new TaskCanceledException("timeout"));

        var ex = await Assert.ThrowsAsync<ServicoIndisponivelException>(() =>
            new TraducaoService(http).TraduzirAsync("hello", "en", "pt"));

        Assert.Contains("demorou", ex.Message);
    }

    [Fact]
    public async Task Idiomas_iguais_sao_rejeitados()
    {
        var (http, _) = HandlerFalso.Json("{}");

        await Assert.ThrowsAsync<ArgumentException>(() =>
            new TraducaoService(http).TraduzirAsync("hello", "en", "EN"));
    }
}

public class DicionarioServiceTests
{
    private const string Hello = """
        [{"word":"hello","phonetic":"/həˈləʊ/",
          "phonetics":[{"text":"/həˈləʊ/","audio":""},{"text":"/həˈloʊ/","audio":"https://exemplo.test/hello.mp3"}],
          "meanings":[{"partOfSpeech":"noun",
                       "definitions":[{"definition":"A greeting.","example":"She said hello.","synonyms":["hi"]}],
                       "synonyms":["greeting","hi","salutation"]}]}]
        """;

    [Fact]
    public async Task Interpreta_definicao_fonetica_audio_e_sinonimos()
    {
        var (http, handler) = HandlerFalso.Json(Hello);

        var definicao = await new DicionarioService(http).BuscarAsync("hello");

        Assert.NotNull(definicao);
        Assert.Equal("hello", definicao.Palavra);
        Assert.Equal("noun", definicao.ClasseGramatical);
        Assert.Equal("A greeting.", definicao.Definicao);
        Assert.Equal("She said hello.", definicao.Exemplo);
        Assert.Equal("/həˈləʊ/", definicao.Fonetica);
        Assert.Equal("https://exemplo.test/hello.mp3", definicao.UrlAudio);
        Assert.Equal(["greeting", "hi", "salutation"], definicao.Sinonimos);
        Assert.EndsWith("/entries/en/hello", handler.Requisicoes.Single().AbsoluteUri);
    }

    [Fact]
    public async Task Sinonimos_vem_so_do_primeiro_significado()
    {
        var (http, _) = HandlerFalso.Json("""
            [{"word":"house",
              "meanings":[
                {"partOfSpeech":"noun","definitions":[{"definition":"A building.","synonyms":["home"]}],"synonyms":["dwelling"]},
                {"partOfSpeech":"verb","definitions":[{"definition":"To shelter.","synonyms":["shop"]}],"synonyms":["lodge"]}]}]
            """);

        var definicao = await new DicionarioService(http).BuscarAsync("house");

        Assert.Equal(["dwelling", "home"], definicao!.Sinonimos);
    }

    [Fact]
    public async Task Palavra_inexistente_404_devolve_nulo_sem_erro()
    {
        var (http, _) = HandlerFalso.Json("""{"title":"No Definitions Found"}""", HttpStatusCode.NotFound);

        Assert.Null(await new DicionarioService(http).BuscarAsync("asdfgh"));
    }

    [Fact]
    public async Task Texto_vazio_nao_chama_a_api()
    {
        var (http, handler) = HandlerFalso.Json(Hello);

        Assert.Null(await new DicionarioService(http).BuscarAsync("  "));
        Assert.Empty(handler.Requisicoes);
    }

    [Fact]
    public async Task Resposta_sem_definicao_devolve_nulo()
    {
        var (http, _) = HandlerFalso.Json("""[{"word":"x","meanings":[]}]""");

        Assert.Null(await new DicionarioService(http).BuscarAsync("x"));
    }

    [Fact]
    public async Task Json_invalido_vira_ServicoIndisponivel()
    {
        var (http, _) = HandlerFalso.Json("<html>erro</html>");

        await Assert.ThrowsAsync<ServicoIndisponivelException>(() =>
            new DicionarioService(http).BuscarAsync("hello"));
    }
}
