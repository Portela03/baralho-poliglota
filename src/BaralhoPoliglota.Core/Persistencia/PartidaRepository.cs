using BaralhoPoliglota.Core.Models;
using BaralhoPoliglota.Core.Services;

namespace BaralhoPoliglota.Core.Persistencia;

/// <summary>Histórico de partidas em <c>partidas.json</c>.</summary>
public sealed class PartidaRepository : IPartidaRepository
{
    private readonly ArmazenamentoJson<ResultadoPartida> _armazenamento;

    public PartidaRepository(string pasta) =>
        _armazenamento = new ArmazenamentoJson<ResultadoPartida>(Path.Combine(pasta, "partidas.json"));

    public Task<IReadOnlyList<ResultadoPartida>> ListarAsync() =>
        _armazenamento.ExecutarAsync<IReadOnlyList<ResultadoPartida>>(itens =>
            (itens.OrderByDescending(p => p.Data).ToList(), false));

    public Task AdicionarAsync(ResultadoPartida resultado) =>
        _armazenamento.ExecutarAsync(itens =>
        {
            itens.Add(resultado);
            return (true, true);
        });
}
