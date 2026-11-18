using BaralhoPoliglota.ViewModels;

namespace BaralhoPoliglota.Views;

public partial class FeriadosPage : ContentPage
{
    public FeriadosPage(FeriadosViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
