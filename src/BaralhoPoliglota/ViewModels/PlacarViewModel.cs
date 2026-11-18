using System.Collections.ObjectModel;
using BaralhoPoliglota.Core.Models;
using BaralhoPoliglota.Core.Services;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BaralhoPoliglota.ViewModels;

public partial class PlacarViewModel : BaseViewModel
{
    private readonly IPartidaRepository _repositorio;

    public PlacarViewModel(IPartidaRepository repositorio) => _repositorio = repositorio;

    public ObservableCollection<ResultadoPartida> Historico { get; } = [];

    [ObservableProperty]
    private string _partidasTexto = "0";

    [ObservableProperty]
    private string _melhorTexto = "–";

    [ObservableProperty]
    private string _mediaTexto = "–";

    [ObservableProperty]
    private string _taxaTexto = "–";

    [ObservableProperty]
    private bool _vazio = true;

    public async Task CarregarAsync()
    {
        var partidas = await _repositorio.ListarAsync();

        Historico.Clear();
        foreach (var partida in partidas)
            Historico.Add(partida);

        Vazio = partidas.Count == 0;
        PartidasTexto = partidas.Count.ToString();

        if (partidas.Count == 0)
        {
            MelhorTexto = MediaTexto = TaxaTexto = "–";
            return;
        }

        MelhorTexto = partidas.Max(p => p.Pontos).ToString();
        MediaTexto = partidas.Average(p => p.Pontos).ToString("0.#");

        var rodadas = partidas.Sum(p => p.TotalRodadas);
        TaxaTexto = rodadas == 0 ? "–" : $"{(double)partidas.Sum(p => p.Acertos) / rodadas:P0}";
    }
}
