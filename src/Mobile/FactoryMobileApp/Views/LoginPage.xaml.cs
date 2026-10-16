using EstrelaClick.Shared.SharedKernel.Mobile.ViewModels;

namespace EstrelaClick.Mobile.FactoryMobileApp.Views;

public partial class LoginPage : ContentPage
{
    private readonly LoginViewModel _viewModel;

    public LoginPage(LoginViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    private async void OnEntrarClicked(object? sender, EventArgs e)
    {
        var success = await _viewModel.AutenticarAsync();
        if (success)
        {
            await Shell.Current.GoToAsync("//BancadaPage");
        }
    }
}
