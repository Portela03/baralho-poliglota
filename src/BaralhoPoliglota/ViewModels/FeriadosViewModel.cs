using BaralhoPoliglota.Core.Estudo;
using BaralhoPoliglota.Core.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BaralhoPoliglota.ViewModels;

/// <summary>Cria cartas com os feriados do Brasil (BrasilAPI) traduzidos para o idioma estudado.</summary>
public partial class FeriadosViewModel : BaseViewModel
{
    private readonly ImportadorDeFeriados _importador;

    public FeriadosViewModel(ImportadorDeFeriados importador)
    {
        _importador = importador;
        _idioma = Idiomas[0];
    }

    public IReadOnlyList<Idioma> Idiomas { get; } = Idioma.Suportados.Where(i => i.Codigo != "pt").ToList();

    [ObservableProperty]
    private Idioma _idioma;

    [ObservableProperty]
    private string _anoTexto = DateTime.Now.Year.ToString();

    [ObservableProperty]
    private double _andamento;

    [ObservableProperty]
    private string _andamentoTexto = string.Empty;

    [ObservableProperty]
    private string? _resultadoTexto;

    [RelayCommand]
    private Task CriarAsync() => ExecutarAsync(async () =>
    {
        ResultadoTexto = null;
        if (!int.TryParse(AnoTexto, out var ano) || ano is < 1900 or > 2199)
        {
            Aviso = "Informe um ano válido (por exemplo, 2026).";
            return;
        }

        Andamento = 0;
        AndamentoTexto = "Buscando os feriados na BrasilAPI…";

        var progresso = new Progress<(int Feitos, int Total)>(p =>
        {
            Andamento = (double)p.Feitos / p.Total;
            AndamentoTexto = $"Traduzindo {p.Feitos} de {p.Total}…";
        });

        var r = await _importador.ImportarAsync(ano, Idioma.Codigo, progresso);

        AndamentoTexto = string.Empty;
        ResultadoTexto = $"{r.Adicionadas} cartas novas" +
            (r.JaExistiam > 0 ? $", {r.JaExistiam} já estavam no baralho" : string.Empty) +
            (r.Falhas > 0 ? $", {r.Falhas} falharam (tente de novo mais tarde)" : string.Empty) + ".";
    });
}
