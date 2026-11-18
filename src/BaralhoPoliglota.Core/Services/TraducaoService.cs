using System.Net;
using System.Text.Json;
using BaralhoPoliglota.Core.Models;

namespace BaralhoPoliglota.Core.Services;

/// <summary>Tradução via MyMemory (https://api.mymemory.translated.net).</summary>
public sealed class TraducaoService : ITraducaoService
{
    public const string UrlBase = "https://api.mymemory.translated.net/get";
    // O MyMemory é colaborativo: a primeira sugestão nem sempre é a melhor, então mostramos várias.
    private const int MaximoDeAlternativas = 4;

    private readonly HttpClient _http;

    public TraducaoService(HttpClient http) => _http = http;

    public async Task<ResultadoTraducao> TraduzirAsync(
        string texto, string origem, string destino, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(texto))
            throw new ArgumentException("Informe uma palavra para traduzir.", nameof(texto));
        if (string.Equals(origem, destino, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("O idioma de origem e o de destino devem ser diferentes.");

        var url = $"{UrlBase}?q={Uri.EscapeDataString(texto.Trim())}&langpair={Uri.EscapeDataString($"{origem}|{destino}")}";

        using var doc = await HttpJson.ObterAsync(_http, url, "o serviço de tradução (MyMemory)", ct)
            ?? throw new ServicoIndisponivelException("O serviço de tradução (MyMemory) não encontrou o recurso.");

        return Interpretar(doc.RootElement);
    }

    internal static ResultadoTraducao Interpretar(JsonElement raiz)
    {
        if (raiz.TryGetProperty("quotaFinished", out var cota) && cota.ValueKind == JsonValueKind.True)
            throw new CotaExcedidaException();

        var status = LerStatus(raiz);
        if (status == 429)
            throw new CotaExcedidaException();
        if (status != 200)
            throw new ServicoIndisponivelException(
                raiz.TextoOuNulo("responseDetails") ?? $"O serviço de tradução respondeu com status {status}.");

        if (!raiz.TryGetProperty("responseData", out var dados))
            throw new ServicoIndisponivelException("Resposta do serviço de tradução sem dados.");

        var principal = WebUtility.HtmlDecode(dados.TextoOuNulo("translatedText") ?? string.Empty).Trim();
        if (principal.Length == 0)
            throw new ServicoIndisponivelException("O serviço de tradução não devolveu nenhuma tradução.");

        var alternativas = new List<string>();
        var vistas = new HashSet<string> { Normalizador.Normalizar(principal) };

        if (raiz.TryGetProperty("matches", out var matches) && matches.ValueKind == JsonValueKind.Array)
        {
            foreach (var match in matches.EnumerateArray())
            {
                var candidata = WebUtility.HtmlDecode(match.TextoOuNulo("translation") ?? string.Empty).Trim();
                if (candidata.Length > 0 && vistas.Add(Normalizador.Normalizar(candidata)))
                    alternativas.Add(candidata);
                if (alternativas.Count >= MaximoDeAlternativas)
                    break;
            }
        }

        return new ResultadoTraducao(principal, alternativas);
    }

    /// <summary>O MyMemory devolve <c>responseStatus</c> ora como número, ora como texto.</summary>
    private static int LerStatus(JsonElement raiz)
    {
        if (!raiz.TryGetProperty("responseStatus", out var status))
            return 200;

        return status.ValueKind switch
        {
            JsonValueKind.Number => status.GetInt32(),
            JsonValueKind.String when int.TryParse(status.GetString(), out var n) => n,
            _ => 200,
        };
    }
}
