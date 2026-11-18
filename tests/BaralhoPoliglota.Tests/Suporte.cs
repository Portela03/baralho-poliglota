using System.Net;
using System.Text;
using BaralhoPoliglota.Core.Models;
using BaralhoPoliglota.Core.Services;

namespace BaralhoPoliglota.Tests;

/// <summary>Handler que devolve respostas prontas, sem acessar a rede.</summary>
internal sealed class HandlerFalso : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;

    public HandlerFalso(Func<HttpRequestMessage, HttpResponseMessage> responder) => _responder = responder;

    public List<Uri> Requisicoes { get; } = [];

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        Requisicoes.Add(request.RequestUri!);
        return Task.FromResult(_responder(request));
    }

    public static (HttpClient Http, HandlerFalso Handler) Json(string json, HttpStatusCode status = HttpStatusCode.OK)
    {
        var handler = new HandlerFalso(_ => new HttpResponseMessage(status)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
        });
        return (new HttpClient(handler), handler);
    }

    public static HttpClient Falha(Exception ex) => new(new HandlerFalso(_ => throw ex));
}

internal sealed class BaralhoFalso : IBaralhoService
{
    private readonly Queue<Carta> _cartas;

    public BaralhoFalso(params Carta[] cartas) => _cartas = new Queue<Carta>(cartas);

    public int BaralhosCriados { get; private set; }

    public Task<BaralhoInfo> NovoBaralhoAsync(CancellationToken ct = default)
    {
        BaralhosCriados++;
        return Task.FromResult(new BaralhoInfo("deck-teste", 52));
    }

    public Task<Carta> SacarAsync(string deckId, CancellationToken ct = default) =>
        Task.FromResult(_cartas.Dequeue());
}

internal static class Fabrica
{
    public static Carta Carta(string valor, string naipe) =>
        new($"{valor}{naipe[0]}", valor, naipe, "https://exemplo.test/carta.png");

    public static PalavraSalva Palavra(string texto, string traducao, params string[] alternativas) =>
        PalavraSalva.Criar(texto, traducao, "en", "pt", alternativas);

    public static List<PalavraSalva> Palavras() =>
    [
        Palavra("house", "casa"),
        Palavra("water", "água", "agua potável"),
        Palavra("book", "livro"),
        Palavra("dog", "cachorro", "cão"),
        Palavra("sun", "sol"),
    ];
}
