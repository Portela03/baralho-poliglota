using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;

namespace BaralhoPoliglota.ViewModels;

[QueryProperty(nameof(PontosTexto), "pontos")]
[QueryProperty(nameof(AcertosTexto), "acertos")]
[QueryProperty(nameof(TotalTexto), "total")]
[QueryProperty(nameof(SequenciaTexto), "sequencia")]
public partial class ResultadoViewModel : BaseViewModel
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Pontos))]
    private string _pontosTexto = "0";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Acertos), nameof(Resumo), nameof(Titulo))]
    private string _acertosTexto = "0";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Total), nameof(Resumo), nameof(Titulo))]
    private string _totalTexto = "0";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MelhorSequencia))]
    private string _sequenciaTexto = "0";

    public int Pontos => Ler(PontosTexto);
    public int Acertos => Ler(AcertosTexto);
    public int Total => Ler(TotalTexto);
    public int MelhorSequencia => Ler(SequenciaTexto);

    public string Resumo => $"{Acertos} de {Total} respostas certas";

    public string Titulo => Total == 0 ? "Fim de jogo" : TituloPara((double)Acertos / Total);

    private static string TituloPara(double taxa) => taxa switch
    {
        >= 0.9 => "Perfeito! 🏆",
        >= 0.6 => "Mandou bem! 🎉",
        >= 0.3 => "Está evoluindo! 📚",
        _ => "Vamos treinar mais! 💪",
    };

    private static int Ler(string texto) => int.TryParse(texto, out var n) ? n : 0;

    [RelayCommand]
    private async Task JogarNovamenteAsync()
    {
        await Shell.Current.GoToAsync("..");
        WeakReferenceMessenger.Default.Send(new JogarNovamenteMessage());
    }

    [RelayCommand]
    private Task VoltarAsync() => Shell.Current.GoToAsync("..");
}
