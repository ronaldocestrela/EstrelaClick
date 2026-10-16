using EstrelaClick.Shared.SharedKernel.Mobile;
using EstrelaClick.Shared.SharedKernel.Nfc;
using EstrelaClick.Shared.SharedKernel.Results;

namespace EstrelaClick.Shared.SharedKernel.Mobile.ViewModels;

public sealed class BancadaViewModel : BaseViewModel
{
    private readonly IFactoryApiClient _apiClient;
    private readonly IOperatorSessionService _sessionService;
    private readonly INfcService? _nfcService;
    private readonly ISensoryFeedbackService? _sensoryFeedback;

    private string _numeroLote = string.Empty;
    private int _totalLote;
    private int _pendentesLote;
    private string? _placaAtivaSlug;
    private string? _placaAtivaUrl;
    private bool _temPlacaAtiva;
    private bool _modoGravacaoContinua = true;
    private string _statusMensagem = "Modo Gravação Contínua ATIVO. Aproxime a tag para gravar...";
    private string _statusCorHex = "#3B82F6"; // Azul primário
    private bool _isBusy;
    private string? _errorMessage;

    public BancadaViewModel(
        IFactoryApiClient apiClient,
        IOperatorSessionService sessionService,
        INfcService? nfcService = null,
        ISensoryFeedbackService? sensoryFeedback = null)
    {
        _apiClient = apiClient;
        _sessionService = sessionService;
        _nfcService = nfcService;
        _sensoryFeedback = sensoryFeedback;
    }

    public string? OperadorId => _sessionService.OperadorId;
    public string? OperadorNome => _sessionService.OperadorNome;
    public int GravadasNaSessao => _sessionService.QuantidadeGravadaSessao;

    public string NumeroLote
    {
        get => _numeroLote;
        set => SetProperty(ref _numeroLote, value);
    }

    public int TotalLote
    {
        get => _totalLote;
        private set => SetProperty(ref _totalLote, value);
    }

    public int PendentesLote
    {
        get => _pendentesLote;
        private set => SetProperty(ref _pendentesLote, value);
    }

    public string? PlacaAtivaSlug
    {
        get => _placaAtivaSlug;
        private set => SetProperty(ref _placaAtivaSlug, value);
    }

    public string? PlacaAtivaUrl
    {
        get => _placaAtivaUrl;
        private set => SetProperty(ref _placaAtivaUrl, value);
    }

    public bool TemPlacaAtiva
    {
        get => _temPlacaAtiva;
        private set => SetProperty(ref _temPlacaAtiva, value);
    }

    public bool ModoGravacaoContinua
    {
        get => _modoGravacaoContinua;
        set
        {
            if (SetProperty(ref _modoGravacaoContinua, value))
            {
                StatusMensagem = value
                    ? "Modo Gravação Contínua ATIVO. Aproxime a tag para gravar..."
                    : "Modo Manual ativo. Clique em 'Buscar Próxima' para gravar.";
                StatusCorHex = value ? "#3B82F6" : "#10B981";
            }
        }
    }

    public string StatusMensagem
    {
        get => _statusMensagem;
        private set => SetProperty(ref _statusMensagem, value);
    }

    public string StatusCorHex
    {
        get => _statusCorHex;
        private set => SetProperty(ref _statusCorHex, value);
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

    public void AlternarModoGravacaoContinua()
    {
        ModoGravacaoContinua = !ModoGravacaoContinua;
    }

    public async Task<bool> CarregarResumoLoteAsync(CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(NumeroLote))
        {
            return false;
        }

        try
        {
            var result = await _apiClient.ObterResumoLoteAsync(NumeroLote.Trim(), ct);
            if (result.IsSuccess && result.Value is not null)
            {
                TotalLote = result.Value.TotalPlacas;
                PendentesLote = result.Value.TotalPendentes;
                return true;
            }

            return false;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> CarregarProximaPlacaAsync(CancellationToken ct = default)
    {
        ErrorMessage = null;
        try
        {
            IsBusy = true;
            StatusMensagem = "Buscando próxima placa na fila...";
            StatusCorHex = "#3B82F6";

            var result = await _apiClient.ObterProximaPendenteAsync(
                string.IsNullOrWhiteSpace(NumeroLote) ? null : NumeroLote.Trim(),
                ct);

            if (result.IsFailure || result.Value is null)
            {
                ErrorMessage = result.Error.Description;
                StatusMensagem = result.Error.Description;
                StatusCorHex = "#EF4444";
                TemPlacaAtiva = false;
                PlacaAtivaSlug = null;
                PlacaAtivaUrl = null;
                return false;
            }

            var placa = result.Value;
            PlacaAtivaSlug = placa.Slug;
            PlacaAtivaUrl = placa.UrlRedirecionamento;
            TemPlacaAtiva = true;

            StatusMensagem = "Placa alocada. Aproxime a tag NFC no leitor para gravar.";
            StatusCorHex = "#10B981";

            if (!string.IsNullOrWhiteSpace(NumeroLote))
            {
                _ = CarregarResumoLoteAsync(ct);
            }

            return true;
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task<Result<ConfirmacaoGravacaoResult>> ProcessarTagAproximadaAsync(
        string? chipUid = null,
        CancellationToken ct = default)
    {
        if (IsBusy)
        {
            return Result<ConfirmacaoGravacaoResult>.Failure(MobileErrors.GravacaoEmAndamento);
        }

        if (!TemPlacaAtiva)
        {
            if (ModoGravacaoContinua)
            {
                bool carregou = await CarregarProximaPlacaAsync(ct);
                if (!carregou || !TemPlacaAtiva)
                {
                    if (_sensoryFeedback != null)
                    {
                        await _sensoryFeedback.PlayFailureFeedbackAsync();
                    }
                    return Result<ConfirmacaoGravacaoResult>.Failure(MobileErrors.FilaVazia);
                }
            }
            else
            {
                if (_sensoryFeedback != null)
                {
                    await _sensoryFeedback.PlayFailureFeedbackAsync();
                }
                return Result<ConfirmacaoGravacaoResult>.Failure(MobileErrors.SemPlacaAlocada);
            }
        }

        if (_nfcService == null)
        {
            return Result<ConfirmacaoGravacaoResult>.Failure(NfcErrors.NotSupported);
        }

        IsBusy = true;
        try
        {
            StatusMensagem = $"Gravando NDEF na tag NFC ({PlacaAtivaSlug})...";
            StatusCorHex = "#3B82F6";

            var writeResult = await _nfcService.WriteUriRecordAsync(
                PlacaAtivaUrl!,
                lockTag: false,
                writePassword: NdefUriHelper.DefaultFactoryPassword,
                pack: NdefUriHelper.DefaultFactoryPack,
                ct: ct);

            if (writeResult.IsFailure)
            {
                ErrorMessage = writeResult.Error.Description;
                StatusMensagem = $"Falha na gravação física: {writeResult.Error.Description}";
                StatusCorHex = "#EF4444";
                if (_sensoryFeedback != null)
                {
                    await _sensoryFeedback.PlayFailureFeedbackAsync();
                }
                return Result<ConfirmacaoGravacaoResult>.Failure(writeResult.Error);
            }

            string uidFinal = !string.IsNullOrWhiteSpace(writeResult.Value.Uid)
                ? writeResult.Value.Uid
                : (!string.IsNullOrWhiteSpace(chipUid) ? chipUid : "UNKNOWN");

            StatusMensagem = "Gravado! Confirmando na API da fábrica...";
            var confirmResult = await _apiClient.ConfirmarGravacaoAsync(PlacaAtivaSlug!, uidFinal, ct);

            if (confirmResult.IsFailure || confirmResult.Value is null)
            {
                ErrorMessage = confirmResult.Error.Description;
                StatusMensagem = $"Erro ao confirmar na API: {confirmResult.Error.Description}";
                StatusCorHex = "#EF4444";
                if (_sensoryFeedback != null)
                {
                    await _sensoryFeedback.PlayFailureFeedbackAsync();
                }
                return Result<ConfirmacaoGravacaoResult>.Failure(confirmResult.Error);
            }

            if (_sensoryFeedback != null)
            {
                await _sensoryFeedback.PlaySuccessFeedbackAsync();
            }

            RegistrarSucessoGravacao();

            if (ModoGravacaoContinua)
            {
                await CarregarProximaPlacaAsync(ct);
            }

            return Result<ConfirmacaoGravacaoResult>.Success(confirmResult.Value);
        }
        finally
        {
            IsBusy = false;
        }
    }

    public void RegistrarSucessoGravacao()
    {
        _sessionService.IncrementarGravadas();
        OnPropertyChanged(nameof(GravadasNaSessao));
        TemPlacaAtiva = false;
        PlacaAtivaSlug = null;
        PlacaAtivaUrl = null;
        StatusMensagem = "Gravação confirmada com sucesso!";
        StatusCorHex = "#10B981";
    }

    public void EncerrarSessao()
    {
        _sessionService.EncerrarSessao();
        TemPlacaAtiva = false;
        PlacaAtivaSlug = null;
        PlacaAtivaUrl = null;
        TotalLote = 0;
        PendentesLote = 0;
    }
}
