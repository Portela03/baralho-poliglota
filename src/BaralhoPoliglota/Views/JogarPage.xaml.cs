using BaralhoPoliglota.ViewModels;

namespace BaralhoPoliglota.Views;

public partial class JogarPage : ContentPage
{
    private readonly JogarViewModel _viewModel;

    public JogarPage(JogarViewModel viewModel)
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
