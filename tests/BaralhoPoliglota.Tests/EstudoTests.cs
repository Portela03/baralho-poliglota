using System.Net;
using BaralhoPoliglota.Core.Estudo;
using BaralhoPoliglota.Core.Models;
using BaralhoPoliglota.Core.Persistencia;
using BaralhoPoliglota.Core.Services;

namespace BaralhoPoliglota.Tests;

public class AgendadorTests
{
    private static readonly DateTime Hoje = new(2026, 11, 18, 15, 30, 0);

    [Fact]
    public void Carta_nova_avaliada_como_boa_volta_em_1_dia_e_depois_em_3()
    {
        var p = Fabrica.Palavra("house", "casa");

        p.Revisar(Avaliacao.Bom, Hoje);
        Assert.Equal(1, p.IntervaloDias);
        Assert.Equal(new DateTime(2026, 11, 19), p.ProximaRevisao);

        p.Revisar(Avaliacao.Bom, Hoje.AddDays(1));
        Assert.Equal(3, p.IntervaloDias);
    }

    [Fact]
    public void Intervalos_crescem_com_acertos_seguidos()
    {
        var p = Fabrica.Palavra("house", "casa");
        var dia = Hoje;
        var intervalos = new List<int>();

        for (var i = 0; i < 5; i++)
        {
            p.Revisar(Avaliacao.Bom, dia);
            intervalos.Add(p.IntervaloDias);
            dia = p.ProximaRevisao!.Value;
        }

        Assert.Equal(intervalos.OrderBy(x => x), intervalos);
        Assert.True(intervalos[^1] > 20);
    }

    [Fact]
    public void Errar_zera_o_ciclo_e_a_carta_continua_pendente()
    {
        var p = Fabrica.Palavra("house", "casa");
        p.Revisar(Avaliacao.Bom, Hoje);
        p.Revisar(Avaliacao.Bom, Hoje.AddDays(1));

        p.Revisar(Avaliacao.Errei, Hoje.AddDays(4));

        Assert.Equal(0, p.Repeticoes);
        Assert.Equal(0, p.IntervaloDias);
        Assert.True(p.Pendente(Hoje.AddDays(4)));
        Assert.Equal(2, p.TotalAcertos);
        Assert.Equal(3, p.TotalRevisoes);
    }

    [Fact]
    public void Facilidade_nunca_passa_do_minimo()
    {
        var p = Fabrica.Palavra("house", "casa");
        for (var i = 0; i < 20; i++)
            p.Revisar(Avaliacao.Errei, Hoje);

        Assert.Equal(Agendador.FacilidadeMinima, p.Facilidade);
    }

    [Fact]
    public void Facil_agenda_mais_longe_que_Bom_e_Dificil_menos()
    {
        var p = Fabrica.Palavra("house", "casa");
        p.Revisar(Avaliacao.Bom, Hoje);
        p.Revisar(Avaliacao.Bom, Hoje.AddDays(1));

        var dificil = p.Prever(Avaliacao.Dificil, Hoje);
        var bom = p.Prever(Avaliacao.Bom, Hoje);
        var facil = p.Prever(Avaliacao.Facil, Hoje);

        Assert.True(dificil < bom && bom < facil, $"{dificil} < {bom} < {facil}");
    }

    [Fact]
    public void Prever_nao_altera_a_carta()
    {
        var p = Fabrica.Palavra("house", "casa");

        _ = p.Prever(Avaliacao.Facil, Hoje);

        Assert.True(p.EhNova);
        Assert.Equal(0, p.TotalRevisoes);
    }

    [Theory]
    [InlineData(0, "agora")]
    [InlineData(1, "1 dia")]
    [InlineData(6, "6 dias")]
    [InlineData(60, "2 meses")]
    public void Texto_do_intervalo(int dias, string esperado) =>
        Assert.Equal(esperado, Agendador.TextoDoIntervalo(dias));
}

public class SessaoDeEstudoTests
{
    private static readonly DateTime Hoje = new(2026, 11, 18, 10, 0, 0);

    [Fact]
    public void So_entram_cartas_novas_ou_vencidas_com_as_vencidas_primeiro()
    {
        var nova = Fabrica.Palavra("new", "nova");
        var vencida = Fabrica.Palavra("old", "velha");
        vencida.ProximaRevisao = Hoje.AddDays(-2);
        var futura = Fabrica.Palavra("later", "depois");
        futura.ProximaRevisao = Hoje.AddDays(3);

        var sessao = new SessaoDeEstudo([nova, futura, vencida], () => Hoje);

        Assert.Equal(2, sessao.Total);
        Assert.Same(vencida, sessao.Atual);
    }

    [Fact]
    public void Respeita_o_limite_por_sessao()
    {
        var sessao = new SessaoDeEstudo(Fabrica.Palavras(), () => Hoje, limite: 3);

        Assert.Equal(3, sessao.Total);
    }

    [Fact]
    public void Carta_errada_volta_para_o_fim_e_a_sessao_so_acaba_quando_acertar()
    {
        var a = Fabrica.Palavra("a", "a");
        var b = Fabrica.Palavra("b", "b");
        var sessao = new SessaoDeEstudo([a, b], () => Hoje);

        sessao.Avaliar(Avaliacao.Errei);       // a volta
        Assert.Same(b, sessao.Atual);
        sessao.Avaliar(Avaliacao.Bom);          // b sai
        Assert.Same(a, sessao.Atual);
        Assert.False(sessao.Concluida);

        sessao.Avaliar(Avaliacao.Facil);        // a sai
        Assert.True(sessao.Concluida);
        Assert.Null(sessao.Atual);
        Assert.Equal(3, sessao.Respondidas);
        Assert.Equal(2, sessao.Acertos);
        Assert.Equal(1, sessao.Erros);
    }

    [Fact]
    public void Avaliar_sessao_vazia_gera_erro()
    {
        var sessao = new SessaoDeEstudo([], () => Hoje);

        Assert.True(sessao.Concluida);
        Assert.Throws<InvalidOperationException>(() => sessao.Avaliar(Avaliacao.Bom));
    }

    [Fact]
    public void Progresso_conta_novas_pendentes_e_maduras()
    {
        var nova = Fabrica.Palavra("n", "n");
        var madura = Fabrica.Palavra("m", "m");
        madura.IntervaloDias = 30;
        madura.ProximaRevisao = Hoje.AddDays(10);
        madura.TotalRevisoes = 5;
        madura.TotalAcertos = 4;

        var progresso = Progresso.De([nova, madura], Hoje);

        Assert.Equal(2, progresso.Total);
        Assert.Equal(1, progresso.Novas);
        Assert.Equal(1, progresso.ParaHoje);
        Assert.Equal(1, progresso.Maduras);
        Assert.Equal(0.8, progresso.TaxaDeAcerto, 3);
    }
}

public class FeriadoServiceTests
{
    private const string Json = """
        [
          {"date":"2026-01-01","name":"Confraternização mundial","type":"national","weekday":"quinta-feira"},
          {"date":"2026-02-16","name":"Carnaval","type":"national","weekday":"segunda-feira"},
          {"date":"2026-02-17","name":"Carnaval","type":"national","weekday":"terça-feira"},
          {"date":"data-ruim","name":"Quebrado","type":"national"},
          {"name":"Sem data"}
        ]
        """;

    [Fact]
    public async Task Le_feriados_e_ignora_itens_invalidos()
    {
        var (http, handler) = HandlerFalso.Json(Json);

        var feriados = await new FeriadoService(http).ListarAsync(2026);

        Assert.Equal(3, feriados.Count);
        Assert.Equal(new DateOnly(2026, 1, 1), feriados[0].Data);
        Assert.Equal("quinta-feira", feriados[0].DiaDaSemana);
        Assert.Equal("https://brasilapi.com.br/api/feriados/v1/2026", handler.Requisicoes.Single().AbsoluteUri);
    }

    [Fact]
    public async Task Resposta_que_nao_e_lista_gera_erro()
    {
        var (http, _) = HandlerFalso.Json("""{"message":"erro"}""");

        await Assert.ThrowsAsync<ServicoIndisponivelException>(() => new FeriadoService(http).ListarAsync(2026));
    }

    [Fact]
    public async Task Servidor_fora_do_ar_gera_erro_claro()
    {
        var (http, _) = HandlerFalso.Json("{}", HttpStatusCode.BadGateway);

        var ex = await Assert.ThrowsAsync<ServicoIndisponivelException>(() => new FeriadoService(http).ListarAsync(2026));
        Assert.Contains("BrasilAPI", ex.Message);
    }
}

public sealed class ImportadorDeFeriadosTests : IDisposable
{
    private readonly string _pasta = Path.Combine(Path.GetTempPath(), "baralho-testes-" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_pasta))
            Directory.Delete(_pasta, recursive: true);
    }

    private sealed class FeriadosFalsos(params Feriado[] feriados) : IFeriadoService
    {
        public Task<IReadOnlyList<Feriado>> ListarAsync(int ano, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<Feriado>>(feriados);
    }

    private sealed class TraducaoFalsa(Func<string, string?> traduzir) : ITraducaoService
    {
        public Task<ResultadoTraducao> TraduzirAsync(string texto, string origem, string destino, CancellationToken ct = default)
        {
            var t = traduzir(texto) ?? throw new ServicoIndisponivelException("falhou");
            return Task.FromResult(new ResultadoTraducao(t, []));
        }
    }

    private static readonly Feriado[] Lista =
    [
        new(new DateOnly(2026, 2, 16), "Carnaval", "segunda-feira"),
        new(new DateOnly(2026, 2, 17), "Carnaval", "terça-feira"),
        new(new DateOnly(2026, 9, 7), "Independência do Brasil", "segunda-feira"),
    ];

    [Fact]
    public async Task Cria_uma_carta_por_feriado_sem_repetir_nomes()
    {
        var repo = new PalavraRepository(_pasta);
        var importador = new ImportadorDeFeriados(
            new FeriadosFalsos(Lista), new TraducaoFalsa(t => t == "Carnaval" ? "Carnival" : "Independence of Brazil"), repo);

        var resultado = await importador.ImportarAsync(2026, "en");

        Assert.Equal(new ResultadoImportacao(2, 0, 0), resultado);
        var cartas = await repo.ListarAsync();
        var carnaval = cartas.Single(c => c.Texto == "Carnival");
        Assert.Equal("Carnaval", carnaval.Traducao);
        Assert.Equal("en", carnaval.IdiomaOrigem);
        Assert.Equal("pt", carnaval.IdiomaDestino);
        Assert.Contains("16 de fevereiro", carnaval.Exemplo);
    }

    [Fact]
    public async Task Feriado_da_tabela_usa_a_traducao_fixa_e_nao_chama_o_servico()
    {
        var repo = new PalavraRepository(_pasta);
        var importador = new ImportadorDeFeriados(
            new FeriadosFalsos(new Feriado(new DateOnly(2026, 12, 25), "Natal", "sexta-feira")),
            new TraducaoFalsa(_ => "allowance"), repo);

        var resultado = await importador.ImportarAsync(2026, "en");

        Assert.Equal(new ResultadoImportacao(1, 0, 0), resultado);
        Assert.Equal("Christmas", (await repo.ListarAsync()).Single().Texto);
    }

    [Fact]
    public async Task Importar_de_novo_nao_duplica()
    {
        var repo = new PalavraRepository(_pasta);
        var importador = new ImportadorDeFeriados(new FeriadosFalsos(Lista), new TraducaoFalsa(t => t + " (en)"), repo);

        await importador.ImportarAsync(2026, "en");
        var segunda = await importador.ImportarAsync(2026, "en");

        Assert.Equal(new ResultadoImportacao(0, 2, 0), segunda);
        Assert.Equal(2, (await repo.ListarAsync()).Count);
    }

    [Fact]
    public async Task Falha_numa_traducao_nao_derruba_as_outras()
    {
        var repo = new PalavraRepository(_pasta);
        // Nomes fora da tabela fixa, para exercitar o caminho de reserva (MyMemory).
        Feriado[] foraDaTabela =
        [
            new(new DateOnly(2026, 3, 1), "Feriado Alfa", "domingo"),
            new(new DateOnly(2026, 3, 2), "Feriado Beta", "segunda-feira"),
        ];
        var importador = new ImportadorDeFeriados(
            new FeriadosFalsos(foraDaTabela), new TraducaoFalsa(t => t == "Feriado Alfa" ? null : "Independence Day"), repo);
        var passos = new List<(int, int)>();

        var resultado = await importador.ImportarAsync(2026, "en", new Progress<(int, int)>(passos.Add));
        await Task.Delay(50); // Progress<T> entrega em outra thread

        Assert.Equal(new ResultadoImportacao(1, 0, 1), resultado);
    }

    [Fact]
    public async Task Idioma_portugues_e_recusado()
    {
        var importador = new ImportadorDeFeriados(
            new FeriadosFalsos(Lista), new TraducaoFalsa(t => t), new PalavraRepository(_pasta));

        await Assert.ThrowsAsync<ArgumentException>(() => importador.ImportarAsync(2026, "pt"));
    }
}

public class RespostaDigitadaTests
{
    [Theory]
    [InlineData("casa", true)]
    [InlineData("  CASA ", true)]
    [InlineData("lar", true)]
    [InlineData("Lár", true)]
    [InlineData("carro", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void Aceita_traducao_ou_alternativa_sem_acento_e_caixa(string? resposta, bool esperado) =>
        Assert.Equal(esperado, Fabrica.Palavra("house", "casa", "lar").AceitaResposta(resposta));
}
