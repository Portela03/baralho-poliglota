using BaralhoPoliglota.ViewModels;

namespace BaralhoPoliglota.Views;

public partial class TraduzirPage : ContentPage
{
    public TraduzirPage(TraduzirViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
