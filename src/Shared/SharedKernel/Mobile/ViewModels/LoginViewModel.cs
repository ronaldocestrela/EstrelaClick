using EstrelaClick.Shared.SharedKernel.Mobile;

namespace EstrelaClick.Shared.SharedKernel.Mobile.ViewModels;

public sealed class LoginViewModel : BaseViewModel
{
    private readonly IFactoryApiClient _apiClient;
    private readonly IOperatorSessionService _sessionService;

    private string _login = string.Empty;
    private string _senha = string.Empty;
    private bool _isBusy;
    private string? _errorMessage;
    private bool _isSuccess;

    public LoginViewModel(IFactoryApiClient apiClient, IOperatorSessionService sessionService)
    {
        _apiClient = apiClient;
        _sessionService = sessionService;
    }

    public string Login
    {
        get => _login;
        set => SetProperty(ref _login, value);
    }

    public string Senha
    {
        get => _senha;
        set => SetProperty(ref _senha, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set => SetProperty(ref _isBusy, value);
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        private set => SetProperty(ref _errorMessage, value);
    }

    public bool IsSuccess
    {
        get => _isSuccess;
        private set => SetProperty(ref _isSuccess, value);
    }

    public async Task<bool> AutenticarAsync(CancellationToken ct = default)
    {
        ErrorMessage = null;
        IsSuccess = false;

        if (string.IsNullOrWhiteSpace(Login) || string.IsNullOrWhiteSpace(Senha))
        {
            ErrorMessage = MobileErrors.DadosObrigatorios.Description;
            return false;
        }

        try
        {
            IsBusy = true;
            var result = await _apiClient.LoginAsync(Login.Trim(), Senha.Trim(), ct);

            if (result.IsFailure || result.Value is null)
            {
                ErrorMessage = result.Error.Description;
                return false;
            }

            var auth = result.Value;
            _sessionService.IniciarSessao(auth.OperadorId, auth.Nome, auth.Token, auth.ExpiracaoUtc);
            IsSuccess = true;
            return true;
        }
        finally
        {
            IsBusy = false;
        }
    }
}
