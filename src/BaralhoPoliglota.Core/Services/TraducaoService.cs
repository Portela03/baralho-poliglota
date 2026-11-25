using System.Globalization;
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

        // Pontuação de cada candidata: soma, por tradução, de qualidade × similaridade (match) × (1 + usos).
        // Traduções repetidas em várias entradas da memória acumulam pontos, então a mais consensual vence.
        var pontos = new Dictionary<string, (string Texto, double Pontos)>();
        pontos[Normalizador.Normalizar(principal)] = (principal, 0);

        if (raiz.TryGetProperty("matches", out var matches) && matches.ValueKind == JsonValueKind.Array)
        {
            foreach (var match in matches.EnumerateArray())
            {
                var candidata = WebUtility.HtmlDecode(match.TextoOuNulo("translation") ?? string.Empty).Trim();
                if (candidata.Length == 0)
                    continue;

                var chave = Normalizador.Normalizar(candidata);
                var ponto = LerNumero(match, "quality") / 100.0 * LerNumero(match, "match") * (1 + LerNumero(match, "usage-count"));
                pontos[chave] = pontos.TryGetValue(chave, out var atual)
                    ? (atual.Texto, atual.Pontos + ponto)
                    : (candidata, ponto);

                if (vistas.Add(chave) && alternativas.Count < MaximoDeAlternativas)
                    alternativas.Add(candidata);
            }
        }

        var melhor = pontos.Values.OrderByDescending(p => p.Pontos).First().Texto;
        return new ResultadoTraducao(principal, alternativas, melhor);
    }

    /// <summary>Número vindo do MyMemory, que ora é texto ("74"), ora número (1).</summary>
    private static double LerNumero(JsonElement elemento, string nome)
    {
        if (!elemento.TryGetProperty(nome, out var valor))
            return 0;

        return valor.ValueKind switch
        {
            JsonValueKind.Number => valor.GetDouble(),
            JsonValueKind.String when double.TryParse(
                valor.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var n) => n,
            _ => 0,
        };
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
