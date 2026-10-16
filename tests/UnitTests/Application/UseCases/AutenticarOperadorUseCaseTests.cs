using EstrelaClick.Backend.Application.Common.Errors;
using EstrelaClick.Backend.Application.DTOs;
using EstrelaClick.Backend.Application.Ports.Outbound;
using EstrelaClick.Backend.Application.UseCases;
using EstrelaClick.Shared.SharedKernel.Results;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace EstrelaClick.Tests.UnitTests.Application.UseCases;

public class AutenticarOperadorUseCaseTests
{
    private readonly IOperadorAuthenticationService _authServiceMock = Substitute.For<IOperadorAuthenticationService>();
    private readonly ITokenService _tokenServiceMock = Substitute.For<ITokenService>();

    [Theory]
    [InlineData("", "123456")]
    [InlineData("   ", "123456")]
    [InlineData("operador", "")]
    [InlineData("operador", "   ")]
    public async Task ExecutarAsync_ComCamposVazios_DeveRetornarFalhaDadosObrigatorios(string login, string senha)
    {
        // Arrange
        var useCase = new AutenticarOperadorUseCase(_authServiceMock, _tokenServiceMock);
        var command = new AutenticarOperadorCommand(login, senha);

        // Act
        var result = await useCase.ExecutarAsync(command);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AuthErrors.DadosObrigatorios);
    }

    [Fact]
    public async Task ExecutarAsync_QuandoCredenciaisForemInvalidas_DeveRetornarFalhaCredenciaisInvalidas()
    {
        // Arrange
        _authServiceMock.ValidarCredenciaisAsync("OP01", "senha-errada", Arg.Any<CancellationToken>())
            .Returns(Result<OperadorInfo>.Failure(AuthErrors.CredenciaisInvalidas));

        var useCase = new AutenticarOperadorUseCase(_authServiceMock, _tokenServiceMock);
        var command = new AutenticarOperadorCommand("OP01", "senha-errada");

        // Act
        var result = await useCase.ExecutarAsync(command);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AuthErrors.CredenciaisInvalidas);
    }

    [Fact]
    public async Task ExecutarAsync_QuandoOperadorEstiverInativo_DeveRetornarFalhaOperadorInativo()
    {
        // Arrange
        var operadorInativo = new OperadorInfo("OP01", "Operador Teste", Ativo: false);
        _authServiceMock.ValidarCredenciaisAsync("OP01", "123456", Arg.Any<CancellationToken>())
            .Returns(Result<OperadorInfo>.Success(operadorInativo));

        var useCase = new AutenticarOperadorUseCase(_authServiceMock, _tokenServiceMock);
        var command = new AutenticarOperadorCommand("OP01", "123456");

        // Act
        var result = await useCase.ExecutarAsync(command);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AuthErrors.OperadorInativo);
    }

    [Fact]
    public async Task ExecutarAsync_QuandoCredenciaisForemValidas_DeveGerarTokenERetornarSucesso()
    {
        // Arrange
        var operador = new OperadorInfo("OP01", "Carlos Mecânico", Ativo: true);
        _authServiceMock.ValidarCredenciaisAsync("OP01", "123456", Arg.Any<CancellationToken>())
            .Returns(Result<OperadorInfo>.Success(operador));

        _tokenServiceMock.GerarTokenOperador("OP01", "Carlos Mecânico", Arg.Any<TimeSpan>())
            .Returns("token-jwt-super-valido");

        var useCase = new AutenticarOperadorUseCase(_authServiceMock, _tokenServiceMock);
        var command = new AutenticarOperadorCommand("OP01", "123456");

        // Act
        var result = await useCase.ExecutarAsync(command);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Token.Should().Be("token-jwt-super-valido");
        result.Value.OperadorId.Should().Be("OP01");
        result.Value.Nome.Should().Be("Carlos Mecânico");
        result.Value.ExpiracaoUtc.Should().BeAfter(DateTime.UtcNow);
    }
}
