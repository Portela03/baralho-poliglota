using BaralhoPoliglota.Views;

namespace BaralhoPoliglota;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        // Modo 3: navegação em pilha (push/pop). Estas telas não aparecem no menu nem nas abas.
        Routing.RegisterRoute(Rotas.Detalhe, typeof(DetalhePalavraPage));
        Routing.RegisterRoute(Rotas.Resultado, typeof(ResultadoPage));
    }
}
