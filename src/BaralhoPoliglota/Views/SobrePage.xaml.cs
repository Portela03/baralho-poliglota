namespace BaralhoPoliglota.Views;

public partial class SobrePage : ContentPage
{
    public SobrePage()
    {
        InitializeComponent();
        VersaoLabel.Text = $"Versão {AppInfo.Current.VersionString}";
    }
}
