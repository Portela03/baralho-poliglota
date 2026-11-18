using BaralhoPoliglota.Core.Estudo;
using BaralhoPoliglota.Core.Models;
using BaralhoPoliglota.Core.Services;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BaralhoPoliglota.ViewModels;

public partial class ProgressoViewModel : BaseViewModel
{
    private readonly IPalavraRepository _palavras;

    public ProgressoViewModel(IPalavraRepository palavras) => _palavras = palavras;

    [ObservableProperty] private string _totalTexto = "0";
    [ObservableProperty] private string _novasTexto = "0";
    [ObservableProperty] private string _paraHojeTexto = "0";
    [ObservableProperty] private string _aprendendoTexto = "0";
    [ObservableProperty] private string _maduraTexto = "0";
    [ObservableProperty] private string _revisoesTexto = "0";
    [ObservableProperty] private string _taxaTexto = "–";

    public async Task CarregarAsync()
    {
        var palavras = await _palavras.ListarAsync();
        var p = Progresso.De(palavras.ToList(), DateTime.Now);

        TotalTexto = p.Total.ToString();
        NovasTexto = p.Novas.ToString();
        ParaHojeTexto = p.ParaHoje.ToString();
        AprendendoTexto = p.Aprendendo.ToString();
        MaduraTexto = p.Maduras.ToString();
        RevisoesTexto = p.Revisoes.ToString();
        TaxaTexto = p.Revisoes == 0 ? "–" : $"{p.TaxaDeAcerto:P0}";
    }
}
