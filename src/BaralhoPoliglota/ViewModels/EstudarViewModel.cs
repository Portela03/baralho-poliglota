using BaralhoPoliglota.Core.Estudo;
using BaralhoPoliglota.Core.Models;
using BaralhoPoliglota.Core.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BaralhoPoliglota.ViewModels;

/// <summary>Estudo estilo Anki: mostra a frente, revela o verso e agenda a próxima revisão.</summary>
public partial class EstudarViewModel : BaseViewModel
{
    private readonly IPalavraRepository _palavras;
    private SessaoDeEstudo? _sessao;
    private int _revisadasNaSessao;

    public EstudarViewModel(IPalavraRepository palavras) => _palavras = palavras;

    // --- Tela inicial ---------------------------------------------------------------------------

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(NaoEstudando))]
    private bool _estudando;

    public bool NaoEstudando => !Estudando;

    [ObservableProperty]
    private string _novasTexto = "0";

    [ObservableProperty]
    private string _paraHojeTexto = "0";

    [ObservableProperty]
    private string _maduraTexto = "0";

    [ObservableProperty]
    private string _textoIntro = string.Empty;

    [ObservableProperty]
    private string _textoIniciar = "Estudar agora";

    [ObservableProperty]
    private bool _podeEstudar;

    // --- Carta ----------------------------------------------------------------------------------

    [ObservableProperty]
    private string _contador = string.Empty;

    [ObservableProperty]
    private string _idiomaFrente = string.Empty;

    [ObservableProperty]
    private string _frente = string.Empty;

    [ObservableProperty]
    private string _verso = string.Empty;

    [ObservableProperty]
    private string _detalhes = string.Empty;

    [ObservableProperty]
    private bool _temDetalhes;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(NaoRevelada))]
    private bool _revelada;

    public bool NaoRevelada => !Revelada;

    // --- Resposta digitada ----------------------------------------------------------------------

    [ObservableProperty]
    private string _respostaDigitada = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MostrarAvaliacao), nameof(MostrarSoErrei))]
    private bool _acertou;

    [ObservableProperty]
    private string _feedback = string.Empty;

    /// <summary>Acertou: pode dizer o quanto foi fácil. Errou: só resta "Errei".</summary>
    public bool MostrarAvaliacao => Revelada && Acertou;
    public bool MostrarSoErrei => Revelada && !Acertou;

    partial void OnReveladaChanged(bool value)
    {
        OnPropertyChanged(nameof(MostrarAvaliacao));
        OnPropertyChanged(nameof(MostrarSoErrei));
    }

    // --- Placar da sessão -----------------------------------------------------------------------

    [ObservableProperty]
    private string _placar = string.Empty;

    [ObservableProperty]
    private string _textoErrei = string.Empty;

    [ObservableProperty]
    private string _textoDificil = string.Empty;

    [ObservableProperty]
    private string _textoBom = string.Empty;

    [ObservableProperty]
    private string _textoFacil = string.Empty;

    public async Task CarregarAsync()
    {
        if (Estudando)
            return;

        var palavras = await _palavras.ListarAsync();
        var progresso = Progresso.De(palavras.ToList(), DateTime.Now);

        NovasTexto = progresso.Novas.ToString();
        ParaHojeTexto = progresso.ParaHoje.ToString();
        MaduraTexto = progresso.Maduras.ToString();
        PodeEstudar = progresso.ParaHoje > 0;

        TextoIntro = progresso.Total == 0
            ? "Seu baralho está vazio. Traduza palavras na aba Traduzir (ou crie o baralho de feriados no menu)."
            : progresso.ParaHoje == 0
                ? "Nada para revisar agora. Volte mais tarde ou adicione novas palavras. 🎉"
                : $"Você tem {progresso.ParaHoje} cartas para hoje (até {SessaoDeEstudo.LimitePorSessao} por sessão).";
        TextoIniciar = progresso.ParaHoje > 0
            ? $"Estudar agora ({Math.Min(progresso.ParaHoje, SessaoDeEstudo.LimitePorSessao)})"
            : "Estudar agora";
    }

    [RelayCommand]
    private async Task IniciarAsync()
    {
        var palavras = await _palavras.ListarAsync();
        _sessao = new SessaoDeEstudo(palavras);
        _revisadasNaSessao = 0;

        if (_sessao.Concluida)
        {
            await CarregarAsync();
            return;
        }

        Estudando = true;
        Mostrar();
    }

    private void Mostrar()
    {
        var carta = _sessao?.Atual;
        if (carta is null)
            return;

        Revelada = false;
        RespostaDigitada = string.Empty;
        Feedback = string.Empty;
        Aviso = null;
        IdiomaFrente = Idioma.NomeDe(carta.IdiomaOrigem).ToUpperInvariant();
        Frente = carta.Texto;
        Verso = carta.Traducao;

        var linhas = new List<string>();
        if (carta.Alternativas.Count > 0)
            linhas.Add($"Também: {string.Join(", ", carta.Alternativas)}");
        if (!string.IsNullOrWhiteSpace(carta.Fonetica))
            linhas.Add(carta.Fonetica!);
        if (carta.TemDefinicao)
            linhas.Add(carta.Definicao!);
        if (!string.IsNullOrWhiteSpace(carta.Exemplo))
            linhas.Add(carta.Exemplo!);
        Detalhes = string.Join("\n", linhas);
        TemDetalhes = linhas.Count > 0;

        var agora = DateTime.Now;
        TextoErrei = $"Errei\n{Agendador.TextoDoIntervalo(0)}";
        TextoDificil = $"Difícil\n{Agendador.TextoDoIntervalo(carta.Prever(Avaliacao.Dificil, agora))}";
        TextoBom = $"Bom\n{Agendador.TextoDoIntervalo(carta.Prever(Avaliacao.Bom, agora))}";
        TextoFacil = $"Fácil\n{Agendador.TextoDoIntervalo(carta.Prever(Avaliacao.Facil, agora))}";

        AtualizarPlacar();
        Contador = $"{_sessao!.Restantes} restantes" + (carta.EhNova ? " · carta nova" : string.Empty);
    }

    private void AtualizarPlacar()
    {
        if (_sessao is null)
            return;

        var total = _sessao.Acertos + _sessao.Erros;
        var taxa = total == 0 ? string.Empty : $" ({(double)_sessao.Acertos / total:P0})";
        Placar = $"Acertos: {_sessao.Acertos} · Erros: {_sessao.Erros}{taxa}";
    }

    [RelayCommand]
    private void Verificar()
    {
        var carta = _sessao?.Atual;
        if (carta is null || Revelada)
            return;

        if (string.IsNullOrWhiteSpace(RespostaDigitada))
        {
            Aviso = "Digite sua resposta ou toque em “Não sei”.";
            return;
        }

        Aviso = null;
        Acertou = carta.AceitaResposta(RespostaDigitada);
        Feedback = Acertou ? "Correto! Como foi lembrar?" : $"Não foi dessa vez. A resposta era “{carta.Traducao}”.";
        Revelada = true;
    }

    [RelayCommand]
    private void NaoSei()
    {
        if (_sessao?.Atual is null || Revelada)
            return;

        Aviso = null;
        Acertou = false;
        Feedback = "Sem problema, a carta volta logo.";
        Revelada = true;
    }

    [RelayCommand]
    private async Task AvaliarAsync(string avaliacao)
    {
        if (_sessao is null || !Revelada || !Enum.TryParse<Avaliacao>(avaliacao, out var nota))
            return;

        // Quem errou na digitação só pode marcar "Errei".
        if (!Acertou && nota != Avaliacao.Errei)
            return;

        Revelada = false; // evita duplo toque enquanto grava
        var carta = _sessao.Avaliar(nota);
        _revisadasNaSessao++;
        AtualizarPlacar();
        await _palavras.AtualizarAsync(carta);

        if (!_sessao.Concluida)
        {
            Mostrar();
            return;
        }

        var (acertos, erros) = (_sessao.Acertos, _sessao.Erros);
        _sessao = null;
        Estudando = false;
        await CarregarAsync();
        await Shell.Current.GoToAsync($"{Rotas.Resultado}?revisadas={_revisadasNaSessao}&acertos={acertos}&erros={erros}");
    }

    [RelayCommand]
    private async Task EncerrarAsync()
    {
        var confirmou = await Shell.Current.DisplayAlertAsync(
            "Encerrar estudo", "O que você já avaliou fica salvo. Sair agora?", "Sair", "Continuar");
        if (!confirmou)
            return;

        _sessao = null;
        Estudando = false;
        await CarregarAsync();
    }
}
