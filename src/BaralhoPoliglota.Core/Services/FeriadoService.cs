using System.Globalization;
using System.Text.Json;
using BaralhoPoliglota.Core.Models;

namespace BaralhoPoliglota.Core.Services;

/// <summary>Feriados nacionais via BrasilAPI (https://brasilapi.com.br).</summary>
public sealed class FeriadoService : IFeriadoService
{
    public const string UrlBase = "https://brasilapi.com.br/api/feriados/v1";
    private const string Nome = "a BrasilAPI";

    private readonly HttpClient _http;

    public FeriadoService(HttpClient http) => _http = http;

    public async Task<IReadOnlyList<Feriado>> ListarAsync(int ano, CancellationToken ct = default)
    {
        using var doc = await HttpJson.ObterAsync(_http, $"{UrlBase}/{ano}", Nome, ct)
            ?? throw new ServicoIndisponivelException($"{Nome} não tem feriados para {ano}.");

        if (doc.RootElement.ValueKind != JsonValueKind.Array)
            throw new ServicoIndisponivelException($"{Nome} devolveu uma resposta inesperada.");

        var feriados = new List<Feriado>();
        foreach (var item in doc.RootElement.EnumerateArray())
        {
            var nome = item.TextoOuNulo("name");
            var data = item.TextoOuNulo("date");
            if (nome is null || data is null ||
                !DateOnly.TryParseExact(data, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d))
                continue;

            feriados.Add(new Feriado(d, nome, item.TextoOuNulo("weekday") ?? string.Empty));
        }

        return feriados.OrderBy(f => f.Data).ToList();
    }
}
