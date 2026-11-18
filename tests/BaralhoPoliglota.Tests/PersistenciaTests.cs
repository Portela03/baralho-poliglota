using BaralhoPoliglota.Core.Models;
using BaralhoPoliglota.Core.Persistencia;

namespace BaralhoPoliglota.Tests;

public sealed class PersistenciaTests : IDisposable
{
    private readonly string _pasta = Path.Combine(Path.GetTempPath(), "baralho-testes-" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_pasta))
            Directory.Delete(_pasta, recursive: true);
    }

    [Fact]
    public async Task Palavras_persistem_entre_instancias()
    {
        var palavra = PalavraSalva.Criar("house", "casa", "en", "pt", ["lar"]);
        Assert.True(await new PalavraRepository(_pasta).AdicionarAsync(palavra));

        var lidas = await new PalavraRepository(_pasta).ListarAsync();

        var lida = Assert.Single(lidas);
        Assert.Equal(palavra.Id, lida.Id);
        Assert.Equal("casa", lida.Traducao);
        Assert.Equal(["lar"], lida.Alternativas);
    }

    [Fact]
    public async Task Nao_aceita_duplicata_do_mesmo_par_de_idiomas()
    {
        var repo = new PalavraRepository(_pasta);

        Assert.True(await repo.AdicionarAsync(PalavraSalva.Criar("Água", "water", "pt", "en")));
        Assert.False(await repo.AdicionarAsync(PalavraSalva.Criar(" agua ", "water", "pt", "en")));
        Assert.True(await repo.AdicionarAsync(PalavraSalva.Criar("agua", "agua", "pt", "es")));
        Assert.Equal(2, (await repo.ListarAsync()).Count);
    }

    [Fact]
    public async Task Atualiza_e_remove()
    {
        var repo = new PalavraRepository(_pasta);
        var palavra = PalavraSalva.Criar("dog", "cachorro", "en", "pt");
        await repo.AdicionarAsync(palavra);

        palavra.Favorita = true;
        await repo.AtualizarAsync(palavra);
        Assert.True((await repo.ObterAsync(palavra.Id))!.Favorita);

        await repo.RemoverAsync(palavra.Id);
        Assert.Null(await repo.ObterAsync(palavra.Id));
        Assert.Empty(await repo.ListarAsync());
    }

    [Fact]
    public async Task Arquivo_corrompido_comeca_vazio_e_se_recupera()
    {
        Directory.CreateDirectory(_pasta);
        await File.WriteAllTextAsync(Path.Combine(_pasta, "palavras.json"), "{ isto não é json");
        var repo = new PalavraRepository(_pasta);

        Assert.Empty(await repo.ListarAsync());
        Assert.True(await repo.AdicionarAsync(PalavraSalva.Criar("sun", "sol", "en", "pt")));
        Assert.Single(await repo.ListarAsync());
    }

    [Fact]
    public async Task Gravacoes_simultaneas_nao_perdem_dados()
    {
        var repo = new PalavraRepository(_pasta);

        await Task.WhenAll(Enumerable.Range(0, 20).Select(i =>
            repo.AdicionarAsync(PalavraSalva.Criar($"palavra{i}", $"palavra{i}", "en", "pt"))));

        Assert.Equal(20, (await repo.ListarAsync()).Count);
    }

    [Fact]
    public async Task Partidas_ficam_em_ordem_da_mais_recente()
    {
        var repo = new PartidaRepository(_pasta);
        await repo.AdicionarAsync(new ResultadoPartida { Pontos = 10, Data = new DateTime(2026, 1, 1) });
        await repo.AdicionarAsync(new ResultadoPartida { Pontos = 99, Data = new DateTime(2026, 6, 1) });

        var lidas = await repo.ListarAsync();

        Assert.Equal([99, 10], lidas.Select(p => p.Pontos));
    }
}
