using System.Net;
using System.Text.Json;

namespace BaralhoPoliglota.Core.Services;

/// <summary>Leitura de JSON por HTTP com tradução dos erros de rede para <see cref="ServicoIndisponivelException"/>.</summary>
internal static class HttpJson
{
    /// <summary>Devolve o JSON da resposta ou <c>null</c> quando o servidor responde 404.</summary>
    public static async Task<JsonDocument?> ObterAsync(
        HttpClient http, string url, string nomeDoServico, CancellationToken ct)
    {
        try
        {
            using var resposta = await http.GetAsync(url, ct).ConfigureAwait(false);

            if (resposta.StatusCode == HttpStatusCode.NotFound)
                return null;

            if (resposta.StatusCode == HttpStatusCode.TooManyRequests)
                throw new CotaExcedidaException();

            if (!resposta.IsSuccessStatusCode)
                throw new ServicoIndisponivelException(
                    $"{nomeDoServico} respondeu com erro {(int)resposta.StatusCode}.");

            await using var fluxo = await resposta.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
            return await JsonDocument.ParseAsync(fluxo, cancellationToken: ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            throw new ServicoIndisponivelException($"{nomeDoServico} demorou demais para responder.");
        }
        catch (HttpRequestException ex)
        {
            throw new ServicoIndisponivelException(
                $"Não foi possível acessar {nomeDoServico}. Verifique sua conexão.", ex);
        }
        catch (JsonException ex)
        {
            throw new ServicoIndisponivelException($"{nomeDoServico} devolveu uma resposta inválida.", ex);
        }
    }

    public static string? TextoOuNulo(this JsonElement elemento, string propriedade)
    {
        if (elemento.ValueKind != JsonValueKind.Object ||
            !elemento.TryGetProperty(propriedade, out var valor) ||
            valor.ValueKind != JsonValueKind.String)
            return null;

        var texto = valor.GetString();
        return string.IsNullOrWhiteSpace(texto) ? null : texto;
    }
}
