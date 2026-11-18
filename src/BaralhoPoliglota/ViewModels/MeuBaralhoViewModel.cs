using System.Collections.ObjectModel;
using BaralhoPoliglota.Core.Models;
using BaralhoPoliglota.Core.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BaralhoPoliglota.ViewModels;

public partial class MeuBaralhoViewModel : BaseViewModel
{
    private readonly IPalavraRepository _repositorio;
    private List<PalavraSalva> _todas = [];

    public MeuBaralhoViewModel(IPalavraRepository repositorio) => _repositorio = repositorio;

    public ObservableCollection<PalavraSalva> Palavras { get; } = [];

    [ObservableProperty]
    private string _busca = string.Empty;

    [ObservableProperty]
    private string _resumo = string.Empty;

    [ObservableProperty]
    private bool _vazio;

    partial void OnBuscaChanged(string value) => Filtrar();

    public async Task CarregarAsync()
    {
        _todas = (await _repositorio.ListarAsync()).ToList();
        Filtrar();
    }

    private void Filtrar()
    {
        var termo = Normalizador.Normalizar(Busca);

        var itens = _todas
            .Where(p => termo.Length == 0
                        || Normalizador.Normalizar(p.Texto).Contains(termo)
                        || Normalizador.Normalizar(p.Traducao).Contains(termo))
            .OrderByDescending(p => p.Favorita)
            .ThenByDescending(p => p.SalvaEm)
            .ToList();

        Palavras.Clear();
        foreach (var item in itens)
            Palavras.Add(item);

        Vazio = Palavras.Count == 0;
        Resumo = _todas.Count switch
        {
            0 => "Nenhuma palavra guardada",
            1 => "1 palavra no baralho",
            var n => $"{n} palavras no baralho",
        };
    }

    [RelayCommand]
    private Task AbrirAsync(PalavraSalva palavra) =>
        Shell.Current.GoToAsync($"{Rotas.Detalhe}?id={palavra.Id}");

    [RelayCommand]
    private async Task FavoritarAsync(PalavraSalva palavra)
    {
        palavra.Favorita = !palavra.Favorita;
        await _repositorio.AtualizarAsync(palavra);
        Filtrar();
    }

    [RelayCommand]
    private async Task ExcluirAsync(PalavraSalva palavra)
    {
        var confirmou = await Shell.Current.DisplayAlertAsync(
            "Excluir palavra", $"Remover “{palavra.Texto}” do baralho?", "Excluir", "Cancelar");
        if (!confirmou)
            return;

        await _repositorio.RemoverAsync(palavra.Id);
        _todas.RemoveAll(p => p.Id == palavra.Id);
        Filtrar();
    }
}
