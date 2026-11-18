using System.Text.Json;
using BaralhoPoliglota.Core.Models;

namespace BaralhoPoliglota.Core.Services;

/// <summary>Baralho e cartas via Deck of Cards API (https://deckofcardsapi.com).</summary>
public sealed class BaralhoService : IBaralhoService
{
    public const string UrlBase = "https://deckofcardsapi.com/api/deck";
    private const string Nome = "a Deck of Cards API";

    private readonly HttpClient _http;

    public BaralhoService(HttpClient http) => _http = http;

    public async Task<BaralhoInfo> NovoBaralhoAsync(CancellationToken ct = default)
    {
        using var doc = await HttpJson.ObterAsync(_http, $"{UrlBase}/new/shuffle/?deck_count=1", Nome, ct)
            ?? throw new ServicoIndisponivelException($"Não foi possível criar um baralho em {Nome}.");

        var raiz = doc.RootElement;
        GarantirSucesso(raiz);

        var deckId = raiz.TextoOuNulo("deck_id")
            ?? throw new ServicoIndisponivelException($"{Nome} não devolveu o identificador do baralho.");
        var restantes = raiz.TryGetProperty("remaining", out var r) && r.ValueKind == JsonValueKind.Number
            ? r.GetInt32()
            : 52;

        return new BaralhoInfo(deckId, restantes);
    }

    public async Task<Carta> SacarAsync(string deckId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(deckId))
            throw new ArgumentException("Informe o identificador do baralho.", nameof(deckId));

        var url = $"{UrlBase}/{Uri.EscapeDataString(deckId)}/draw/?count=1";
        using var doc = await HttpJson.ObterAsync(_http, url, Nome, ct)
            ?? throw new ServicoIndisponivelException($"O baralho informado não existe em {Nome}.");

        var raiz = doc.RootElement;
        GarantirSucesso(raiz);

        if (!raiz.TryGetProperty("cards", out var cartas) ||
            cartas.ValueKind != JsonValueKind.Array ||
            cartas.GetArrayLength() == 0)
            throw new ServicoIndisponivelException("O baralho acabou: não há mais cartas para sacar.");

        var c = cartas[0];
        var carta = new Carta(
            c.TextoOuNulo("code") ?? throw Invalida(),
            c.TextoOuNulo("value") ?? throw Invalida(),
            c.TextoOuNulo("suit") ?? throw Invalida(),
            c.TextoOuNulo("image") ?? string.Empty);

        // Valida valor e naipe agora, para que uma resposta inesperada falhe aqui e não no meio da partida.
        try
        {
            _ = carta.ValorNumerico;
            _ = carta.NaipeIndice;
        }
        catch (InvalidOperationException ex)
        {
            throw new ServicoIndisponivelException(ex.Message, ex);
        }

        return carta;
    }

    private static void GarantirSucesso(JsonElement raiz)
    {
        if (raiz.TryGetProperty("success", out var s) && s.ValueKind == JsonValueKind.False)
            throw new ServicoIndisponivelException(
                raiz.TextoOuNulo("error") ?? $"{Nome} informou uma falha.");
    }

    private static ServicoIndisponivelException Invalida() =>
        new($"{Nome} devolveu uma carta incompleta.");
}
