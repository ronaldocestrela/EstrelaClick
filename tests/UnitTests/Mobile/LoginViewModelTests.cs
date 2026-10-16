using EstrelaClick.Shared.SharedKernel.Mobile;
using EstrelaClick.Shared.SharedKernel.Mobile.ViewModels;
using EstrelaClick.Shared.SharedKernel.Results;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace EstrelaClick.Tests.UnitTests.Mobile;

public class LoginViewModelTests
{
    private readonly IFactoryApiClient _apiClientMock = Substitute.For<IFactoryApiClient>();
    private readonly IOperatorSessionService _sessionService = new OperatorSessionService();

    [Fact]
    public async Task AutenticarAsync_ComCamposVazios_DeveDefinirMensagemDeErroENaoChamarApi()
    {
        // Arrange
        var vm = new LoginViewModel(_apiClientMock, _sessionService);
        vm.Login = "";
        vm.Senha = "";

        // Act
        var result = await vm.AutenticarAsync();

        // Assert
        result.Should().BeFalse();
        vm.ErrorMessage.Should().NotBeNullOrWhiteSpace();
        vm.IsSuccess.Should().BeFalse();
        await _apiClientMock.DidNotReceiveWithAnyArgs().LoginAsync(default!, default!);
    }

    [Fact]
    public async Task AutenticarAsync_QuandoCredenciaisInvalidas_DeveExibirErroENaoIniciarSessao()
    {
        // Arrange
        _apiClientMock.LoginAsync("OP01", "senha-errada", Arg.Any<CancellationToken>())
            .Returns(Result<OperadorLoginResult>.Failure(MobileErrors.CredenciaisInvalidas));

        var vm = new LoginViewModel(_apiClientMock, _sessionService);
        vm.Login = "OP01";
        vm.Senha = "senha-errada";

        // Act
        var result = await vm.AutenticarAsync();

        // Assert
        result.Should().BeFalse();
        vm.ErrorMessage.Should().Be(MobileErrors.CredenciaisInvalidas.Description);
        vm.IsSuccess.Should().BeFalse();
        _sessionService.IsAuthenticated.Should().BeFalse();
    }

    [Fact]
    public async Task AutenticarAsync_QuandoSucesso_DeveIniciarSessaoEDefinirIsSuccess()
    {
        // Arrange
        var expiracao = DateTime.UtcNow.AddHours(8);
        var loginResult = new OperadorLoginResult("jwt-token-123", "OP01", "Marcos Operador", expiracao);

        _apiClientMock.LoginAsync("OP01", "123456", Arg.Any<CancellationToken>())
            .Returns(Result<OperadorLoginResult>.Success(loginResult));

        var vm = new LoginViewModel(_apiClientMock, _sessionService);
        vm.Login = "OP01";
        vm.Senha = "123456";

        // Act
        var result = await vm.AutenticarAsync();

        // Assert
        result.Should().BeTrue();
        vm.IsSuccess.Should().BeTrue();
        vm.ErrorMessage.Should().BeNull();
        _sessionService.IsAuthenticated.Should().BeTrue();
        _sessionService.OperadorId.Should().Be("OP01");
        _sessionService.OperadorNome.Should().Be("Marcos Operador");
        _sessionService.Token.Should().Be("jwt-token-123");
    }
}
