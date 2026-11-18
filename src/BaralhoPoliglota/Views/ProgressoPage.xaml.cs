using BaralhoPoliglota.ViewModels;

namespace BaralhoPoliglota.Views;

public partial class ProgressoPage : ContentPage
{
    private readonly ProgressoViewModel _viewModel;

    public ProgressoPage(ProgressoViewModel viewModel)
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
