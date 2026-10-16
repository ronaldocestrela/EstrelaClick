using EstrelaClick.Shared.SharedKernel.Mobile;
using FluentAssertions;
using Xunit;

namespace EstrelaClick.Tests.UnitTests.Mobile;

public class OperatorSessionServiceTests
{
    [Fact]
    public void QuandoInicializado_DeveConterEstadoNaoAutenticado()
    {
        // Arrange & Act
        var session = new OperatorSessionService();

        // Assert
        session.IsAuthenticated.Should().BeFalse();
        session.OperadorId.Should().BeNull();
        session.OperadorNome.Should().BeNull();
        session.Token.Should().BeNull();
        session.QuantidadeGravadaSessao.Should().Be(0);
    }

    [Fact]
    public void IniciarSessao_ComDadosValidos_DeveAtualizarPropriedades()
    {
        // Arrange
        var session = new OperatorSessionService();
        var expiracao = DateTime.UtcNow.AddHours(8);

        // Act
        session.IniciarSessao("OP01", "Lucas Operador", "token-jwt-123", expiracao);

        // Assert
        session.IsAuthenticated.Should().BeTrue();
        session.OperadorId.Should().Be("OP01");
        session.OperadorNome.Should().Be("Lucas Operador");
        session.Token.Should().Be("token-jwt-123");
        session.ExpiracaoUtc.Should().Be(expiracao);
        session.QuantidadeGravadaSessao.Should().Be(0);
    }

    [Fact]
    public void IncrementarGravadas_DeveAumentarContador()
    {
        // Arrange
        var session = new OperatorSessionService();
        session.IniciarSessao("OP01", "Lucas Operador", "token-jwt-123", DateTime.UtcNow.AddHours(8));

        // Act
        session.IncrementarGravadas();
        session.IncrementarGravadas();

        // Assert
        session.QuantidadeGravadaSessao.Should().Be(2);
    }

    [Fact]
    public void EncerrarSessao_DeveLimparEstadoEZerarContador()
    {
        // Arrange
        var session = new OperatorSessionService();
        session.IniciarSessao("OP01", "Lucas Operador", "token-jwt-123", DateTime.UtcNow.AddHours(8));
        session.IncrementarGravadas();

        // Act
        session.EncerrarSessao();

        // Assert
        session.IsAuthenticated.Should().BeFalse();
        session.OperadorId.Should().BeNull();
        session.OperadorNome.Should().BeNull();
        session.Token.Should().BeNull();
        session.QuantidadeGravadaSessao.Should().Be(0);
    }
}
