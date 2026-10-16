using EstrelaClick.Shared.SharedKernel.Mobile;
using EstrelaClick.Shared.SharedKernel.Mobile.ViewModels;
using EstrelaClick.Shared.SharedKernel.Nfc;
using EstrelaClick.Shared.SharedKernel.Results;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace EstrelaClick.Tests.UnitTests.Mobile;

public class BancadaGravacaoCicloTests
{
    private readonly IFactoryApiClient _apiClientMock = Substitute.For<IFactoryApiClient>();
    private readonly IOperatorSessionService _sessionService = new OperatorSessionService();
    private readonly INfcService _nfcServiceMock = Substitute.For<INfcService>();
    private readonly ISensoryFeedbackService _sensoryFeedbackMock = Substitute.For<ISensoryFeedbackService>();

    public BancadaGravacaoCicloTests()
    {
        _sessionService.IniciarSessao(
            operadorId: "op-123",
            operadorNome: "Operador Teste",
            token: "jwt-token-valido",
            expiracaoUtc: DateTime.UtcNow.AddHours(8));
    }

    private BancadaViewModel CriarViewModel()
    {
        return new BancadaViewModel(
            _apiClientMock,
            _sessionService,
            _nfcServiceMock,
            _sensoryFeedbackMock);
    }

    [Fact]
    public void BancadaViewModel_DeveIniciar_ComModoGravacaoContinuaAtivoPorPadrao()
    {
        // Act
        var vm = CriarViewModel();

        // Assert
        vm.ModoGravacaoContinua.Should().BeTrue("a esteira fabril opera em modo contínuo por padrão");
        vm.StatusMensagem.Should().Contain("Modo Gravação Contínua ATIVO");
    }

    [Fact]
    public async Task ProcessarTagAproximadaAsync_ComSucesso_EmModoContinua_DeveGravar_ConfirmarApi_EmitirBipe_E_AvancarFila()
    {
        // Arrange
        var vm = CriarViewModel();
        var placa1 = new PlacaAtivaInfo(Guid.NewGuid(), "abc1", "LOT-001", "https://estrelaclick.com.br/r/abc1", DateTime.UtcNow, DateTime.UtcNow.AddMinutes(5), null);
        var placa2 = new PlacaAtivaInfo(Guid.NewGuid(), "abc2", "LOT-001", "https://estrelaclick.com.br/r/abc2", DateTime.UtcNow, DateTime.UtcNow.AddMinutes(5), null);

        _apiClientMock.ObterProximaPendenteAsync(Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Result<PlacaAtivaInfo>.Success(placa1), Result<PlacaAtivaInfo>.Success(placa2));

        _nfcServiceMock.WriteUriRecordAsync(
                Arg.Is<string>(u => u == placa1.UrlRedirecionamento),
                Arg.Any<bool>(),
                Arg.Any<byte[]?>(),
                Arg.Any<byte[]?>(),
                Arg.Any<CancellationToken>())
            .Returns(Result<NfcWriteResult>.Success(new NfcWriteResult("04:A1:B2:C3:D4:E5:F6", 42, false)));

        _apiClientMock.ConfirmarGravacaoAsync(placa1.Slug, "04:A1:B2:C3:D4:E5:F6", Arg.Any<CancellationToken>())
            .Returns(Result<ConfirmacaoGravacaoResult>.Success(new ConfirmacaoGravacaoResult(placa1.Id, placa1.Slug, "04:A1:B2:C3:D4:E5:F6", DateTime.UtcNow)));

        // Carrega primeira placa
        await vm.CarregarProximaPlacaAsync();
        vm.PlacaAtivaSlug.Should().Be("abc1");

        // Act
        var result = await vm.ProcessarTagAproximadaAsync("04:A1:B2:C3:D4:E5:F6");

        // Assert
        result.IsSuccess.Should().BeTrue();
        await _sensoryFeedbackMock.Received(1).PlaySuccessFeedbackAsync();
        await _apiClientMock.Received(1).ConfirmarGravacaoAsync("abc1", "04:A1:B2:C3:D4:E5:F6", Arg.Any<CancellationToken>());
        vm.GravadasNaSessao.Should().Be(1);

        // Como está em modo contínuo, deve ter avançado automaticamente para placa2
        vm.PlacaAtivaSlug.Should().Be("abc2");
        vm.TemPlacaAtiva.Should().BeTrue();
    }

    [Fact]
    public async Task ProcessarTagAproximadaAsync_ComSucesso_EmModoManual_NaoDeveAvancarFilaAutomaticamente()
    {
        // Arrange
        var vm = CriarViewModel();
        vm.ModoGravacaoContinua = false;

        var placa1 = new PlacaAtivaInfo(Guid.NewGuid(), "xyz9", "LOT-002", "https://estrelaclick.com.br/r/xyz9", DateTime.UtcNow, DateTime.UtcNow.AddMinutes(5), null);

        _apiClientMock.ObterProximaPendenteAsync(Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Result<PlacaAtivaInfo>.Success(placa1));

        _nfcServiceMock.WriteUriRecordAsync(
                Arg.Any<string>(),
                Arg.Any<bool>(),
                Arg.Any<byte[]?>(),
                Arg.Any<byte[]?>(),
                Arg.Any<CancellationToken>())
            .Returns(Result<NfcWriteResult>.Success(new NfcWriteResult("04:11:22:33:44:55:66", 42, false)));

        _apiClientMock.ConfirmarGravacaoAsync("xyz9", "04:11:22:33:44:55:66", Arg.Any<CancellationToken>())
            .Returns(Result<ConfirmacaoGravacaoResult>.Success(new ConfirmacaoGravacaoResult(placa1.Id, placa1.Slug, "04:11:22:33:44:55:66", DateTime.UtcNow)));

        await vm.CarregarProximaPlacaAsync();

        // Act
        var result = await vm.ProcessarTagAproximadaAsync("04:11:22:33:44:55:66");

        // Assert
        result.IsSuccess.Should().BeTrue();
        await _sensoryFeedbackMock.Received(1).PlaySuccessFeedbackAsync();
        vm.GravadasNaSessao.Should().Be(1);
        vm.TemPlacaAtiva.Should().BeFalse("no modo manual a placa é concluída sem puxar automaticamente a próxima");
        await _apiClientMock.Received(1).ObterProximaPendenteAsync(Arg.Any<string?>(), Arg.Any<CancellationToken>()); // apenas o inicial
    }

    [Fact]
    public async Task ProcessarTagAproximadaAsync_QuandoGravacaoNfcFalhar_DeveAcionarFeedbackFalha_E_ManterPlacaAtiva()
    {
        // Arrange
        var vm = CriarViewModel();
        var placa = new PlacaAtivaInfo(Guid.NewGuid(), "err1", "LOT-001", "https://estrelaclick.com.br/r/err1", DateTime.UtcNow, DateTime.UtcNow.AddMinutes(5), null);

        _apiClientMock.ObterProximaPendenteAsync(Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Result<PlacaAtivaInfo>.Success(placa));

        _nfcServiceMock.WriteUriRecordAsync(
                Arg.Any<string>(),
                Arg.Any<bool>(),
                Arg.Any<byte[]?>(),
                Arg.Any<byte[]?>(),
                Arg.Any<CancellationToken>())
            .Returns(Result<NfcWriteResult>.Failure(NfcErrors.TagLost));

        await vm.CarregarProximaPlacaAsync();

        // Act
        var result = await vm.ProcessarTagAproximadaAsync("04:AA:BB:CC:DD:EE:FF");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(NfcErrors.TagLost.Code);
        await _sensoryFeedbackMock.Received(1).PlayFailureFeedbackAsync();
        await _sensoryFeedbackMock.DidNotReceive().PlaySuccessFeedbackAsync();
        await _apiClientMock.DidNotReceive().ConfirmarGravacaoAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        vm.GravadasNaSessao.Should().Be(0);
        vm.PlacaAtivaSlug.Should().Be("err1", "a placa permanece ativa para o operador tentar aproximar novamente");
    }

    [Fact]
    public async Task ProcessarTagAproximadaAsync_QuandoConfirmacaoApiFalhar_DeveAcionarFeedbackFalha()
    {
        // Arrange
        var vm = CriarViewModel();
        var placa = new PlacaAtivaInfo(Guid.NewGuid(), "err2", "LOT-001", "https://estrelaclick.com.br/r/err2", DateTime.UtcNow, DateTime.UtcNow.AddMinutes(5), null);

        _apiClientMock.ObterProximaPendenteAsync(Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Result<PlacaAtivaInfo>.Success(placa));

        _nfcServiceMock.WriteUriRecordAsync(
                Arg.Any<string>(),
                Arg.Any<bool>(),
                Arg.Any<byte[]?>(),
                Arg.Any<byte[]?>(),
                Arg.Any<CancellationToken>())
            .Returns(Result<NfcWriteResult>.Success(new NfcWriteResult("04:AA:BB:CC:DD:EE:FF", 42, false)));

        _apiClientMock.ConfirmarGravacaoAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result<ConfirmacaoGravacaoResult>.Failure(MobileErrors.FalhaComunicacao));

        await vm.CarregarProximaPlacaAsync();

        // Act
        var result = await vm.ProcessarTagAproximadaAsync("04:AA:BB:CC:DD:EE:FF");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(MobileErrors.FalhaComunicacao.Code);
        await _sensoryFeedbackMock.Received(1).PlayFailureFeedbackAsync();
        vm.GravadasNaSessao.Should().Be(0);
    }

    [Fact]
    public async Task ProcessarTagAproximadaAsync_SemPlacaAtiva_EFilaVazia_DeveRetornarFalha_E_FeedbackErro()
    {
        // Arrange
        var vm = CriarViewModel();
        _apiClientMock.ObterProximaPendenteAsync(Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Result<PlacaAtivaInfo>.Failure(MobileErrors.FilaVazia));

        // Act
        var result = await vm.ProcessarTagAproximadaAsync("04:AA:BB:CC:DD:EE:FF");

        // Assert
        result.IsFailure.Should().BeTrue();
        await _sensoryFeedbackMock.Received(1).PlayFailureFeedbackAsync();
        await _nfcServiceMock.DidNotReceive().WriteUriRecordAsync(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<byte[]?>(), Arg.Any<byte[]?>(), Arg.Any<CancellationToken>());
    }
}
