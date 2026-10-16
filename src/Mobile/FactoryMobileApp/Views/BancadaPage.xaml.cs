using EstrelaClick.Shared.SharedKernel.Mobile.ViewModels;
using EstrelaClick.Shared.SharedKernel.Nfc;

namespace EstrelaClick.Mobile.FactoryMobileApp.Views;

public partial class BancadaPage : ContentPage
{
    private readonly BancadaViewModel _viewModel;
    private readonly INfcService _nfcService;

    public BancadaPage(BancadaViewModel viewModel, INfcService nfcService)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _nfcService = nfcService;
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        _nfcService.TagDetected += OnNfcTagDetected;
        _nfcService.StartListening();

        // Se o modo contínuo estiver ativo e não houver placa selecionada, busca a próxima automaticamente
        if (_viewModel.ModoGravacaoContinua && !_viewModel.TemPlacaAtiva)
        {
            await _viewModel.CarregarProximaPlacaAsync();
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        _nfcService.StopListening();
        _nfcService.TagDetected -= OnNfcTagDetected;
    }

    private void OnNfcTagDetected(object? sender, NfcTagDetectedEventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            await _viewModel.ProcessarTagAproximadaAsync(e.Uid);
        });
    }

    private async void OnAtualizarLoteClicked(object? sender, EventArgs e)
    {
        await _viewModel.CarregarResumoLoteAsync();
    }

    private async void OnBuscarProximaClicked(object? sender, EventArgs e)
    {
        await _viewModel.CarregarProximaPlacaAsync();
    }

    private async void OnLogoutClicked(object? sender, EventArgs e)
    {
        _viewModel.EncerrarSessao();
        await Shell.Current.GoToAsync("//LoginPage");
    }
}
