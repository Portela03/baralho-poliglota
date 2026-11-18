using BaralhoPoliglota.ViewModels;

namespace BaralhoPoliglota.Views;

public partial class ResultadoPage : ContentPage
{
    public ResultadoPage(ResultadoViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
