using BaralhoPoliglota.ViewModels;

namespace BaralhoPoliglota.Views;

public partial class PlacarPage : ContentPage
{
    private readonly PlacarViewModel _viewModel;

    public PlacarPage(PlacarViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.CarregarAsync();
    }
}
