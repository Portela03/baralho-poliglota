using System.Text.Json;
using BaralhoPoliglota.Core.Models;

namespace BaralhoPoliglota.Core.Services;

/// <summary>Definições em inglês via Free Dictionary API (https://api.dictionaryapi.dev).</summary>
public sealed class DicionarioService : IDicionarioService
{
    public const string UrlBase = "https://api.dictionaryapi.dev/api/v2/entries/en/";
    private const int MaximoDeSinonimos = 5;

    private readonly HttpClient _http;

    public DicionarioService(HttpClient http) => _http = http;

    public async Task<DefinicaoDicionario?> BuscarAsync(string palavraEmIngles, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(palavraEmIngles))
            return null;

        var url = UrlBase + Uri.EscapeDataString(palavraEmIngles.Trim());

        using var doc = await HttpJson.ObterAsync(_http, url, "o dicionário (Free Dictionary API)", ct);
        return doc is null ? null : Interpretar(doc.RootElement);
    }

    internal static DefinicaoDicionario? Interpretar(JsonElement raiz)
    {
        if (raiz.ValueKind != JsonValueKind.Array || raiz.GetArrayLength() == 0)
            return null;

        var entrada = raiz[0];
        var palavra = entrada.TextoOuNulo("word");
        if (palavra is null)
            return null;

        var fonetica = entrada.TextoOuNulo("phonetic");
        string? audio = null;

        if (entrada.TryGetProperty("phonetics", out var fonetics) && fonetics.ValueKind == JsonValueKind.Array)
        {
            foreach (var f in fonetics.EnumerateArray())
            {
                fonetica ??= f.TextoOuNulo("text");
                audio ??= f.TextoOuNulo("audio");
            }
        }

        string? classe = null;
        string? definicao = null;
        string? exemplo = null;
        var sinonimos = new List<string>();

        if (entrada.TryGetProperty("meanings", out var significados) && significados.ValueKind == JsonValueKind.Array)
        {
            // Os sinônimos vêm só do primeiro significado: os dos demais costumam ser de outro sentido
            // da palavra (ex.: "house" → "shop") e confundem quem está estudando.
            var primeiroSignificado = true;
            foreach (var significado in significados.EnumerateArray())
            {
                classe ??= significado.TextoOuNulo("partOfSpeech");
                if (primeiroSignificado)
                    ColetarSinonimos(significado, sinonimos);

                if (significado.TryGetProperty("definitions", out var definicoes) &&
                    definicoes.ValueKind == JsonValueKind.Array)
                {
                    foreach (var d in definicoes.EnumerateArray())
                    {
                        definicao ??= d.TextoOuNulo("definition");
                        exemplo ??= d.TextoOuNulo("example");
                        if (primeiroSignificado)
                            ColetarSinonimos(d, sinonimos);
                    }
                }

                primeiroSignificado = false;
            }
        }

        if (definicao is null)
            return null;

        return new DefinicaoDicionario(
            palavra,
            classe,
            definicao,
            exemplo,
            fonetica,
            audio,
            sinonimos.Distinct(StringComparer.OrdinalIgnoreCase).Take(MaximoDeSinonimos).ToList());
    }

    private static void ColetarSinonimos(JsonElement origem, List<string> destino)
    {
        if (!origem.TryGetProperty("synonyms", out var lista) || lista.ValueKind != JsonValueKind.Array)
            return;

        foreach (var s in lista.EnumerateArray())
        {
            if (s.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(s.GetString()))
                destino.Add(s.GetString()!);
        }
    }
}
