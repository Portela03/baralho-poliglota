using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BaralhoPoliglota.ViewModels;

/// <summary>Resumo mostrado ao terminar uma sessão de estudo (tela empilhada).</summary>
[QueryProperty(nameof(RevisadasTexto), "revisadas")]
[QueryProperty(nameof(AcertosTexto), "acertos")]
[QueryProperty(nameof(ErrosTexto), "erros")]
public partial class ResultadoViewModel : BaseViewModel
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Revisadas), nameof(Resumo))]
    private string _revisadasTexto = "0";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Acertos), nameof(Resumo))]
    private string _acertosTexto = "0";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Erros))]
    private string _errosTexto = "0";

    public int Revisadas => Ler(RevisadasTexto);
    public int Acertos => Ler(AcertosTexto);
    public int Erros => Ler(ErrosTexto);

    public string Resumo => Revisadas == 0
        ? "Nenhuma carta revisada."
        : $"{Acertos} de {Revisadas} respostas lembradas";

    public string Mensagem2 => "As cartas voltam no tempo certo: quanto mais você acerta, mais espaçadas ficam.";

    private static int Ler(string texto) => int.TryParse(texto, out var n) ? n : 0;

    [RelayCommand]
    private Task VoltarAsync() => Shell.Current.GoToAsync("..");
}
