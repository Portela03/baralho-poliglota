using BaralhoPoliglota.Core.Services;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BaralhoPoliglota.ViewModels;

public abstract partial class BaseViewModel : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(NaoOcupado))]
    private bool _ocupado;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TemMensagem))]
    private string? _mensagem;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TemAviso))]
    private string? _aviso;

    public bool NaoOcupado => !Ocupado;
    public bool TemMensagem => !string.IsNullOrEmpty(Mensagem);
    public bool TemAviso => !string.IsNullOrEmpty(Aviso);

    /// <summary>
    /// Executa uma ação mostrando o indicador de carregamento e transformando falhas das APIs
    /// em mensagem na tela (em vez de derrubar o app).
    /// </summary>
    protected async Task ExecutarAsync(Func<Task> acao)
    {
        if (Ocupado)
            return;

        Ocupado = true;
        Mensagem = null;
        Aviso = null;

        try
        {
            await acao();
        }
        catch (ServicoIndisponivelException ex)
        {
            Mensagem = ex.Message;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            Mensagem = $"Algo deu errado: {ex.Message}";
        }
        finally
        {
            Ocupado = false;
        }
    }
}
