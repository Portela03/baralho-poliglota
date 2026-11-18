using System.Globalization;
using BaralhoPoliglota.Core.Models;
using BaralhoPoliglota.Core.Services;

namespace BaralhoPoliglota.Core.Estudo;

/// <summary>Resumo de uma importação de feriados.</summary>
public sealed record ResultadoImportacao(int Adicionadas, int JaExistiam, int Falhas);

/// <summary>
/// Monta o baralho "Feriados do Brasil": busca os feriados na BrasilAPI e traduz o nome de cada
/// um (MyMemory) para o idioma estudado. A frente da carta fica no idioma estudado e o verso em português.
/// </summary>
public sealed class ImportadorDeFeriados
{
    private static readonly CultureInfo Pt = new("pt-BR");

    private readonly IFeriadoService _feriados;
    private readonly ITraducaoService _traducao;
    private readonly IPalavraRepository _palavras;

    public ImportadorDeFeriados(IFeriadoService feriados, ITraducaoService traducao, IPalavraRepository palavras)
    {
        _feriados = feriados;
        _traducao = traducao;
        _palavras = palavras;
    }

    /// <param name="progresso">Chamado com (feitos, total) a cada feriado processado.</param>
    public async Task<ResultadoImportacao> ImportarAsync(
        int ano, string idioma, IProgress<(int Feitos, int Total)>? progresso = null, CancellationToken ct = default)
    {
        if (idioma == "pt")
            throw new ArgumentException("Escolha um idioma diferente do português.", nameof(idioma));

        var feriados = await _feriados.ListarAsync(ano, ct);

        // "Carnaval" aparece em dois dias: uma carta basta.
        var unicos = feriados.GroupBy(f => Normalizador.Normalizar(f.Nome)).Select(g => g.First()).ToList();

        int adicionadas = 0, existiam = 0, falhas = 0, feitos = 0;
        foreach (var feriado in unicos)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                var traducao = await _traducao.TraduzirAsync(feriado.Nome, "pt", idioma, ct);

                var carta = PalavraSalva.Criar(traducao.Traducao, feriado.Nome, idioma, "pt", traducao.Alternativas);
                carta.Exemplo = $"Feriado nacional: {feriado.Data.ToString("d 'de' MMMM", Pt)} ({feriado.DiaDaSemana})";

                if (await _palavras.AdicionarAsync(carta)) adicionadas++;
                else existiam++;
            }
            catch (ServicoIndisponivelException) when (!ct.IsCancellationRequested)
            {
                falhas++;
            }

            progresso?.Report((++feitos, unicos.Count));
        }

        return new ResultadoImportacao(adicionadas, existiam, falhas);
    }
}
