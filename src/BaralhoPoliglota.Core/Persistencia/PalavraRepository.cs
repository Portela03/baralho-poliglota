using BaralhoPoliglota.Core.Models;
using BaralhoPoliglota.Core.Services;

namespace BaralhoPoliglota.Core.Persistencia;

/// <summary>Palavras guardadas em <c>palavras.json</c>.</summary>
public sealed class PalavraRepository : IPalavraRepository
{
    private readonly ArmazenamentoJson<PalavraSalva> _armazenamento;

    public PalavraRepository(string pasta) =>
        _armazenamento = new ArmazenamentoJson<PalavraSalva>(Path.Combine(pasta, "palavras.json"));

    public Task<IReadOnlyList<PalavraSalva>> ListarAsync() =>
        _armazenamento.ExecutarAsync<IReadOnlyList<PalavraSalva>>(itens =>
            (itens.OrderByDescending(p => p.SalvaEm).ToList(), false));

    public Task<PalavraSalva?> ObterAsync(Guid id) =>
        _armazenamento.ExecutarAsync(itens => (itens.FirstOrDefault(p => p.Id == id), false));

    public Task<bool> AdicionarAsync(PalavraSalva palavra) =>
        _armazenamento.ExecutarAsync(itens =>
        {
            if (itens.Any(p => p.Chave == palavra.Chave))
                return (false, false);

            itens.Add(palavra);
            return (true, true);
        });

    public Task AtualizarAsync(PalavraSalva palavra) =>
        _armazenamento.ExecutarAsync(itens =>
        {
            var indice = itens.FindIndex(p => p.Id == palavra.Id);
            if (indice < 0)
                return (false, false);

            itens[indice] = palavra;
            return (true, true);
        });

    public Task RemoverAsync(Guid id) =>
        _armazenamento.ExecutarAsync(itens => (true, itens.RemoveAll(p => p.Id == id) > 0));
}
