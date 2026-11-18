using BaralhoPoliglota.Core.Jogo;
using BaralhoPoliglota.Core.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;

namespace BaralhoPoliglota.ViewModels;

/// <summary>Enviada pela tela de resultado para começar outra partida.</summary>
public sealed record JogarNovamenteMessage;

public partial class JogarViewModel : BaseViewModel
{
    private readonly IPalavraRepository _palavras;
    private readonly IPartidaRepository _partidas;
    private readonly IBaralhoService _baralho;

    private MotorDoJogo? _motor;
    private Pergunta? _pergunta;

    public JogarViewModel(IPalavraRepository palavras, IPartidaRepository partidas, IBaralhoService baralho)
    {
        _palavras = palavras;
        _partidas = partidas;
        _baralho = baralho;

        WeakReferenceMessenger.Default.Register<JogarViewModel, JogarNovamenteMessage>(
            this, static (r, _) => r.IniciarCommand.Execute(null));
    }

    // --- Tela inicial ---------------------------------------------------------------------------

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(NaoJogando))]
    private bool _jogando;

    [ObservableProperty]
    private string _textoIntro = string.Empty;

    [ObservableProperty]
    private bool _podeIniciar;

    public bool NaoJogando => !Jogando;

    // --- Partida --------------------------------------------------------------------------------

    [ObservableProperty]
    private string _rodadaTexto = string.Empty;

    [ObservableProperty]
    private string _pontosTexto = string.Empty;

    [ObservableProperty]
    private string _sequenciaTexto = string.Empty;

    [ObservableProperty]
    private string? _urlCarta;

    [ObservableProperty]
    private string _descricaoCarta = string.Empty;

    [ObservableProperty]
    private string _tipoTexto = string.Empty;

    [ObservableProperty]
    private string _enunciado = string.Empty;

    [ObservableProperty]
    private IReadOnlyList<string> _opcoes = [];

    [ObservableProperty]
    private bool _ehMultipla;

    [ObservableProperty]
    private bool _ehDigitada;

    [ObservableProperty]
    private string _respostaDigitada = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(NaoRespondida))]
    [NotifyPropertyChangedFor(nameof(PodeResponderDigitada))]
    private bool _respondida;

    [ObservableProperty]
    private string? _feedback;

    [ObservableProperty]
    private bool _acertou;

    [ObservableProperty]
    private bool _falhaAoSacar;

    [ObservableProperty]
    private string _textoProxima = "Próxima carta";

    public bool NaoRespondida => !Respondida;
    public bool PodeResponderDigitada => EhDigitada && !Respondida;

    partial void OnEhDigitadaChanged(bool value) => OnPropertyChanged(nameof(PodeResponderDigitada));

    public async Task CarregarAsync()
    {
        if (Jogando)
            return;

        var total = (await _palavras.ListarAsync()).Count;
        PodeIniciar = total >= MotorDoJogo.MinimoDePalavras;
        TextoIntro = PodeIniciar
            ? $"Você tem {total} palavras no baralho. Cada partida tem {MotorDoJogo.RodadasPorPartida} rodadas: " +
              "as cartas sorteadas definem a pergunta e os pontos."
            : $"Guarde pelo menos {MotorDoJogo.MinimoDePalavras} palavras na aba Traduzir para jogar " +
              $"(você tem {total}).";
    }

    [RelayCommand]
    private Task IniciarAsync() => ExecutarAsync(async () =>
    {
        var palavras = await _palavras.ListarAsync();
        if (palavras.Count < MotorDoJogo.MinimoDePalavras)
        {
            Mensagem = $"Guarde pelo menos {MotorDoJogo.MinimoDePalavras} palavras para jogar.";
            return;
        }

        _motor = new MotorDoJogo(_baralho, palavras);
        Jogando = true;
        AtualizarPlacar();
        await SacarAsync();
    });

    [RelayCommand]
    private Task TentarNovamenteAsync() => ExecutarAsync(SacarAsync);

    private async Task SacarAsync()
    {
        if (_motor is null)
            return;

        FalhaAoSacar = false;
        try
        {
            Mostrar(await _motor.ProximaPerguntaAsync());
        }
        catch (ServicoIndisponivelException)
        {
            FalhaAoSacar = true;
            throw;
        }
    }

    private void Mostrar(Pergunta pergunta)
    {
        _pergunta = pergunta;

        UrlCarta = pergunta.Carta.UrlImagem;
        DescricaoCarta = pergunta.Carta.Descricao;
        Enunciado = pergunta.Enunciado;
        EhMultipla = pergunta.Tipo == TipoPergunta.MultiplaEscolha;
        EhDigitada = pergunta.Tipo == TipoPergunta.Digitada;
        TipoTexto = EhMultipla
            ? $"Carta vermelha ({pergunta.Carta.SimboloNaipe}): escolha a tradução"
            : $"Carta preta ({pergunta.Carta.SimboloNaipe}): digite a tradução";
        Opcoes = pergunta is PerguntaMultiplaEscolha m ? m.Opcoes : [];
        RespostaDigitada = string.Empty;
        Feedback = null;
        Respondida = false;
        TextoProxima = "Próxima carta";
        AtualizarPlacar(rodadaAtual: _motor!.Partida.RodadasJogadas + 1);
    }

    private void AtualizarPlacar(int? rodadaAtual = null)
    {
        var partida = _motor?.Partida;
        if (partida is null)
            return;

        RodadaTexto = $"Rodada {Math.Min(rodadaAtual ?? partida.RodadasJogadas, partida.TotalRodadas)}/{partida.TotalRodadas}";
        PontosTexto = $"{partida.Pontos} pts";
        SequenciaTexto = partida.SequenciaAtual >= 2 ? $"🔥 {partida.SequenciaAtual} seguidas" : string.Empty;
    }

    [RelayCommand]
    private void ResponderOpcao(string opcao) => Registrar(opcao);

    [RelayCommand]
    private void ResponderDigitada()
    {
        if (string.IsNullOrWhiteSpace(RespostaDigitada))
        {
            Aviso = "Digite sua resposta primeiro.";
            return;
        }

        Aviso = null;
        Registrar(RespostaDigitada);
    }

    private void Registrar(string resposta)
    {
        if (_motor is null || _pergunta is null || Respondida)
            return;

        Acertou = _pergunta.Verificar(resposta);
        var ganho = _motor.Responder(_pergunta, resposta);

        Feedback = Acertou
            ? $"Correto! +{ganho} pontos"
            : $"Não foi dessa vez. A resposta era “{_pergunta.RespostaCorreta}”.";
        Respondida = true;
        TextoProxima = _motor.Partida.Finalizada ? "Ver resultado" : "Próxima carta";
        AtualizarPlacar(rodadaAtual: _motor.Partida.RodadasJogadas);
    }

    [RelayCommand]
    private async Task ProximaAsync()
    {
        if (_motor is null)
            return;

        if (!_motor.Partida.Finalizada)
        {
            await ExecutarAsync(SacarAsync);
            return;
        }

        await FinalizarAsync();
    }

    private async Task FinalizarAsync()
    {
        var partida = _motor!.Partida;
        var resultado = partida.ParaResultado();
        await _partidas.AdicionarAsync(resultado);

        _motor = null;
        _pergunta = null;
        Jogando = false;
        await CarregarAsync();

        await Shell.Current.GoToAsync(
            $"{Rotas.Resultado}?pontos={resultado.Pontos}&acertos={resultado.Acertos}" +
            $"&total={resultado.TotalRodadas}&sequencia={resultado.MelhorSequencia}");
    }

    [RelayCommand]
    private async Task DesistirAsync()
    {
        var confirmou = await Shell.Current.DisplayAlertAsync(
            "Encerrar partida", "Sair agora? Esta partida não será registrada no placar.", "Sair", "Continuar");
        if (!confirmou)
            return;

        _motor = null;
        _pergunta = null;
        Jogando = false;
        Mensagem = null;
        await CarregarAsync();
    }
}
