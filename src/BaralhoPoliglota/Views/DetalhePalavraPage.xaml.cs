using BaralhoPoliglota.ViewModels;

namespace BaralhoPoliglota.Views;

public partial class DetalhePalavraPage : ContentPage
{
    public DetalhePalavraPage(DetalhePalavraViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
