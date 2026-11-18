using BaralhoPoliglota.Core.Models;

namespace BaralhoPoliglota.Core.Services;

public interface ITraducaoService
{
    /// <exception cref="ServicoIndisponivelException">Falha de rede, timeout ou erro do servidor.</exception>
    /// <exception cref="CotaExcedidaException">Cota gratuita da API esgotada.</exception>
    Task<ResultadoTraducao> TraduzirAsync(string texto, string origem, string destino, CancellationToken ct = default);
}

public interface IDicionarioService
{
    /// <summary>Busca a palavra em inglês. Devolve <c>null</c> quando a palavra não existe (HTTP 404).</summary>
    Task<DefinicaoDicionario?> BuscarAsync(string palavraEmIngles, CancellationToken ct = default);
}

public interface IBaralhoService
{
    Task<BaralhoInfo> NovoBaralhoAsync(CancellationToken ct = default);
    Task<Carta> SacarAsync(string deckId, CancellationToken ct = default);
}

public interface IPalavraRepository
{
    Task<IReadOnlyList<PalavraSalva>> ListarAsync();
    Task<PalavraSalva?> ObterAsync(Guid id);

    /// <summary>Devolve <c>false</c> se já existir a mesma palavra para o mesmo par de idiomas.</summary>
    Task<bool> AdicionarAsync(PalavraSalva palavra);

    Task AtualizarAsync(PalavraSalva palavra);
    Task RemoverAsync(Guid id);
}

public interface IPartidaRepository
{
    Task<IReadOnlyList<ResultadoPartida>> ListarAsync();
    Task AdicionarAsync(ResultadoPartida resultado);
}
