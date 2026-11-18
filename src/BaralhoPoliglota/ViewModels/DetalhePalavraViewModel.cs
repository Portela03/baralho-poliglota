using BaralhoPoliglota.Core.Models;
using BaralhoPoliglota.Core.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BaralhoPoliglota.ViewModels;

[QueryProperty(nameof(Id), "id")]
public partial class DetalhePalavraViewModel : BaseViewModel
{
    private readonly IPalavraRepository _repositorio;

    public DetalhePalavraViewModel(IPalavraRepository repositorio) => _repositorio = repositorio;

    [ObservableProperty]
    private string? _id;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TemDefinicao))]
    [NotifyPropertyChangedFor(nameof(TemAudio))]
    [NotifyPropertyChangedFor(nameof(TemExemplo))]
    [NotifyPropertyChangedFor(nameof(TextoFavorito))]
    [NotifyPropertyChangedFor(nameof(TituloDefinicao))]
    [NotifyPropertyChangedFor(nameof(AlternativasTexto))]
    private PalavraSalva? _palavra;

    public bool TemDefinicao => Palavra?.TemDefinicao ?? false;
    public bool TemAudio => Palavra?.TemAudio ?? false;
    public bool TemExemplo => !string.IsNullOrWhiteSpace(Palavra?.Exemplo);
    public string TextoFavorito => Palavra?.Favorita == true ? "★ Favorita" : "☆ Favoritar";

    public string TituloDefinicao => Palavra?.DefinicaoDe is { } de
        ? $"Definição de “{de}” (inglês)"
        : "Definição";

    public string? AlternativasTexto =>
        Palavra is { Alternativas.Count: > 0 } p ? "Também: " + string.Join(", ", p.Alternativas) : null;

    async partial void OnIdChanged(string? value)
    {
        if (!Guid.TryParse(value, out var guid))
            return;

        try
        {
            Palavra = await _repositorio.ObterAsync(guid);
            if (Palavra is null)
                Mensagem = "Essa palavra não está mais no baralho.";
        }
        catch (IOException ex)
        {
            Mensagem = $"Não foi possível abrir a palavra: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task OuvirAsync()
    {
        if (Palavra?.UrlAudio is not { } url || !Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return;

        try
        {
            await Launcher.Default.OpenAsync(uri);
        }
        catch (Exception ex) when (ex is FeatureNotSupportedException or InvalidOperationException)
        {
            Mensagem = "Não foi possível abrir o áudio neste dispositivo.";
        }
    }

    [RelayCommand]
    private async Task FavoritarAsync()
    {
        if (Palavra is null)
            return;

        Palavra.Favorita = !Palavra.Favorita;
        await _repositorio.AtualizarAsync(Palavra);
        OnPropertyChanged(nameof(TextoFavorito));
    }

    [RelayCommand]
    private async Task RemoverAsync()
    {
        if (Palavra is null)
            return;

        var confirmou = await Shell.Current.DisplayAlertAsync(
            "Excluir palavra", $"Remover “{Palavra.Texto}” do baralho?", "Excluir", "Cancelar");
        if (!confirmou)
            return;

        await _repositorio.RemoverAsync(Palavra.Id);
        await Shell.Current.GoToAsync("..");
    }
}
