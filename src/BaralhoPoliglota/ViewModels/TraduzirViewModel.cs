using BaralhoPoliglota.Core.Models;
using BaralhoPoliglota.Core.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BaralhoPoliglota.ViewModels;

public partial class TraduzirViewModel : BaseViewModel
{
    private const string PrefOrigem = "idioma_origem";
    private const string PrefDestino = "idioma_destino";

    private readonly ITraducaoService _traducao;
    private readonly IDicionarioService _dicionario;
    private readonly IPalavraRepository _repositorio;

    public TraduzirViewModel(
        ITraducaoService traducao,
        IDicionarioService dicionario,
        IPalavraRepository repositorio)
    {
        _traducao = traducao;
        _dicionario = dicionario;
        _repositorio = repositorio;

        _idiomaOrigem = Idioma.Buscar(Preferences.Get(PrefOrigem, "en")) ?? Idioma.Suportados[1];
        _idiomaDestino = Idioma.Buscar(Preferences.Get(PrefDestino, "pt")) ?? Idioma.Suportados[0];
    }

    public IReadOnlyList<Idioma> Idiomas => Idioma.Suportados;

    [ObservableProperty]
    private string _texto = string.Empty;

    [ObservableProperty]
    private Idioma _idiomaOrigem;

    [ObservableProperty]
    private Idioma _idiomaDestino;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TemResultado))]
    [NotifyPropertyChangedFor(nameof(TemAlternativas))]
    private ResultadoTraducao? _resultado;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TemDefinicao))]
    [NotifyPropertyChangedFor(nameof(SinonimosTexto))]
    private DefinicaoDicionario? _definicao;

    [ObservableProperty]
    private string? _notaDefinicao;

    public bool TemResultado => Resultado is not null;
    public bool TemDefinicao => Definicao is not null;

    public bool TemAlternativas => Resultado is { Alternativas.Count: > 0 };

    public string? SinonimosTexto =>
        Definicao is { Sinonimos.Count: > 0 } d ? "Sinônimos: " + string.Join(", ", d.Sinonimos) : null;

    partial void OnIdiomaOrigemChanged(Idioma value)
    {
        Preferences.Set(PrefOrigem, value.Codigo);
        LimparResultado();
    }

    partial void OnIdiomaDestinoChanged(Idioma value)
    {
        Preferences.Set(PrefDestino, value.Codigo);
        LimparResultado();
    }

    private void LimparResultado()
    {
        Resultado = null;
        Definicao = null;
        NotaDefinicao = null;
    }

    /// <summary>O MyMemory é colaborativo: permite ao usuário escolher a sugestão certa antes de guardar.</summary>
    [RelayCommand]
    private void EscolherTraducao(string traducao)
    {
        if (Resultado is null)
            return;

        Resultado = Resultado.ComPrincipal(traducao);
    }

    [RelayCommand]
    private void TrocarIdiomas() => (IdiomaOrigem, IdiomaDestino) = (IdiomaDestino, IdiomaOrigem);

    [RelayCommand]
    private Task TraduzirAsync() => ExecutarAsync(async () =>
    {
        LimparResultado();

        var texto = Texto.Trim();
        if (texto.Length == 0)
        {
            Mensagem = "Digite uma palavra para traduzir.";
            return;
        }

        if (IdiomaOrigem.Codigo == IdiomaDestino.Codigo)
        {
            Mensagem = "Escolha idiomas diferentes para traduzir.";
            return;
        }

        var resultado = await _traducao.TraduzirAsync(texto, IdiomaOrigem.Codigo, IdiomaDestino.Codigo);
        Resultado = resultado;

        // O dicionário só existe em inglês: usa o lado inglês da tradução, se houver.
        var emIngles = IdiomaOrigem.Codigo == "en" ? texto
            : IdiomaDestino.Codigo == "en" ? resultado.Traducao
            : null;

        if (emIngles is null)
        {
            NotaDefinicao = "Definições estão disponíveis apenas quando um dos idiomas é o inglês.";
            return;
        }

        try
        {
            Definicao = await _dicionario.BuscarAsync(emIngles);
            NotaDefinicao = Definicao is null ? $"Definição de “{emIngles}” não encontrada." : null;
        }
        catch (ServicoIndisponivelException)
        {
            NotaDefinicao = "Não foi possível carregar a definição agora.";
        }
    });

    [RelayCommand]
    private Task GuardarAsync() => ExecutarAsync(async () =>
    {
        if (Resultado is null)
            return;

        var palavra = PalavraSalva.Criar(
            Texto,
            Resultado.Traducao,
            IdiomaOrigem.Codigo,
            IdiomaDestino.Codigo,
            Resultado.Alternativas,
            Definicao);

        if (await _repositorio.AdicionarAsync(palavra))
            Aviso = $"“{palavra.Texto}” foi guardada no seu baralho.";
        else
            Mensagem = $"“{palavra.Texto}” já está no seu baralho.";
    });
}
