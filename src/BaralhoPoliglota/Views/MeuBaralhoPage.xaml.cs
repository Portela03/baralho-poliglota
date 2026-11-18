using BaralhoPoliglota.ViewModels;

namespace BaralhoPoliglota.Views;

public partial class MeuBaralhoPage : ContentPage
{
    private readonly MeuBaralhoViewModel _viewModel;

    public MeuBaralhoPage(MeuBaralhoViewModel viewModel)
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
