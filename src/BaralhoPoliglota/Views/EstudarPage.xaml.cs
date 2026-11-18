using BaralhoPoliglota.ViewModels;

namespace BaralhoPoliglota.Views;

public partial class EstudarPage : ContentPage
{
    private readonly EstudarViewModel _viewModel;

    public EstudarPage(EstudarViewModel viewModel)
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
