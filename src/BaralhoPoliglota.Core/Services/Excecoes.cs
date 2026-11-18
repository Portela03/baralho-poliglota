namespace BaralhoPoliglota.Core.Services;

/// <summary>A API não respondeu como esperado (sem rede, timeout, erro 5xx ou resposta inválida).</summary>
public class ServicoIndisponivelException : Exception
{
    public ServicoIndisponivelException(string mensagem, Exception? inner = null) : base(mensagem, inner)
    {
    }
}

/// <summary>A cota gratuita da API de tradução acabou.</summary>
public sealed class CotaExcedidaException : ServicoIndisponivelException
{
    public CotaExcedidaException()
        : base("A cota gratuita do serviço de tradução acabou por hoje. Tente novamente amanhã.")
    {
    }
}
