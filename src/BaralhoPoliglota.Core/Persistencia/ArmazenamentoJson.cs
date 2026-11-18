using System.Text.Json;

namespace BaralhoPoliglota.Core.Persistencia;

/// <summary>
/// Guarda uma lista de itens em um arquivo JSON. Operações são serializadas por um semáforo e a gravação
/// é feita em arquivo temporário para não deixar o arquivo pela metade. Arquivo corrompido vira lista vazia.
/// </summary>
public sealed class ArmazenamentoJson<T>
{
    private static readonly JsonSerializerOptions Opcoes = new() { WriteIndented = true };

    private readonly string _caminho;
    private readonly SemaphoreSlim _trava = new(1, 1);

    public ArmazenamentoJson(string caminho) => _caminho = caminho;

    /// <summary>Executa <paramref name="operacao"/> sobre a lista; grava só se ela devolver <c>Alterou = true</c>.</summary>
    public async Task<TResultado> ExecutarAsync<TResultado>(
        Func<List<T>, (TResultado Resultado, bool Alterou)> operacao)
    {
        await _trava.WaitAsync().ConfigureAwait(false);
        try
        {
            var itens = await LerAsync().ConfigureAwait(false);
            var (resultado, alterou) = operacao(itens);
            if (alterou)
                await GravarAsync(itens).ConfigureAwait(false);
            return resultado;
        }
        finally
        {
            _trava.Release();
        }
    }

    private async Task<List<T>> LerAsync()
    {
        if (!File.Exists(_caminho))
            return [];

        try
        {
            await using var fluxo = File.OpenRead(_caminho);
            return await JsonSerializer.DeserializeAsync<List<T>>(fluxo, Opcoes).ConfigureAwait(false) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private async Task GravarAsync(List<T> itens)
    {
        var pasta = Path.GetDirectoryName(_caminho);
        if (!string.IsNullOrEmpty(pasta))
            Directory.CreateDirectory(pasta);

        var temporario = _caminho + ".tmp";
        await using (var fluxo = File.Create(temporario))
        {
            await JsonSerializer.SerializeAsync(fluxo, itens, Opcoes).ConfigureAwait(false);
        }

        File.Move(temporario, _caminho, overwrite: true);
    }
}
