using EstrelaClick.Frontend.AdminDashboard.Services;
using EstrelaClick.Shared.SharedKernel.Admin;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Xunit;

namespace EstrelaClick.Tests.UnitTests.UI.Services;

public class AdminAuthServiceTests
{
    private readonly AdminSecurityOptions _validOptions = new()
    {
        MasterUser = "admin@estrelaclick.com.br",
        MasterPassword = "AdminMaster2026!",
        SessionTimeoutMinutes = 480
    };

    [Fact]
    public void ValidarCredenciais_ComCredenciaisValidas_DeveRetornarSucessoComClaims()
    {
        // Arrange
        var service = new AdminAuthService(Options.Create(_validOptions));
        var request = new AdminLoginRequest("admin@estrelaclick.com.br", "AdminMaster2026!");

        // Act
        var result = service.ValidarCredenciais(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Usuario.Should().Be("admin@estrelaclick.com.br");
        result.Value.Role.Should().Be("MasterAdmin");
        result.Value.AutenticadoEmUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Theory]
    [InlineData("admin@estrelaclick.com.br", "senha_errada")]
    [InlineData("outro@estrelaclick.com.br", "AdminMaster2026!")]
    [InlineData("admin@estrelaclick.com.br", "adminmaster2026!")] // senha é case-sensitive
    public void ValidarCredenciais_ComCredenciaisIncorretas_DeveRetornarFalhaSemExcecao(string usuario, string senha)
    {
        // Arrange
        var service = new AdminAuthService(Options.Create(_validOptions));
        var request = new AdminLoginRequest(usuario, senha);

        // Act
        var result = service.ValidarCredenciais(request);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("admin.auth.credenciais_invalidas");
        result.Error.Description.Should().Contain("incorretos");
    }

    [Theory]
    [InlineData("", "AdminMaster2026!")]
    [InlineData("   ", "AdminMaster2026!")]
    [InlineData("admin@estrelaclick.com.br", "")]
    [InlineData("admin@estrelaclick.com.br", "   ")]
    public void ValidarCredenciais_ComCamposVazios_DeveRetornarErroValidacao(string usuario, string senha)
    {
        // Arrange
        var service = new AdminAuthService(Options.Create(_validOptions));
        var request = new AdminLoginRequest(usuario, senha);

        // Act
        var result = service.ValidarCredenciais(request);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("admin.auth.campos_obrigatorios");
    }

    [Fact]
    public void ValidarCredenciais_QuandoConfiguracaoMasterAusente_DeveRetornarErroConfiguracao()
    {
        // Arrange
        var invalidOptions = new AdminSecurityOptions
        {
            MasterUser = "",
            MasterPassword = ""
        };
        var service = new AdminAuthService(Options.Create(invalidOptions));
        var request = new AdminLoginRequest("admin@estrelaclick.com.br", "qualquer_coisa");

        // Act
        var result = service.ValidarCredenciais(request);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("admin.auth.configuracao_invalida");
    }

    [Fact]
    public void ValidarCredenciais_ComEspacosAoRedorDoUsuario_DeveTratarTrimSemFalhar()
    {
        // Arrange
        var service = new AdminAuthService(Options.Create(_validOptions));
        var request = new AdminLoginRequest("  admin@estrelaclick.com.br  ", "AdminMaster2026!");

        // Act
        var result = service.ValidarCredenciais(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Usuario.Should().Be("admin@estrelaclick.com.br");
    }

    [Fact]
    public void ValidarCredenciais_ComEmailEmCaixaAlta_DeveNormalizarComSucesso()
    {
        // Arrange
        var service = new AdminAuthService(Options.Create(_validOptions));
        var request = new AdminLoginRequest("ADMIN@ESTRELACLICK.COM.BR", "AdminMaster2026!");

        // Act
        var result = service.ValidarCredenciais(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Usuario.Should().Be("admin@estrelaclick.com.br");
    }
}
