using EstrelaClick.Backend.Domain.Entities;
using FluentAssertions;

namespace EstrelaClick.Tests.UnitTests.Domain.Entities;

public class EmpresaTests
{
    [Fact]
    public void Criar_DeveRetornarEmpresaValida_QuandoDadosForemValidos()
    {
        var result = Empresa.Criar(
            razaoSocial: "Estrela Click LTDA",
            email: "contato@estrelaclick.com",
            telefone: "+55 11 99999-9999",
            identityUserId: "identity-user-123");

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.RazaoSocial.Should().Be("Estrela Click LTDA");
        result.Value.Email.Should().Be("contato@estrelaclick.com");
        result.Value.Telefone.Should().Be("+55 11 99999-9999");
        result.Value.IdentityUserId.Should().Be("identity-user-123");
        result.Value.StatusAtivo.Should().BeTrue();
        result.Value.DataCadastroUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Criar_DeveFalhar_QuandoRazaoSocialForInvalida(string razaoSocial)
    {
        var result = Empresa.Criar(razaoSocial, "contato@estrelaclick.com", "+55 11 99999-9999");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("empresa.razaoSocial.invalida");
    }

    [Theory]
    [InlineData("")]
    [InlineData("email-invalido")]
    [InlineData("usuario@")]
    public void Criar_DeveFalhar_QuandoEmailForInvalido(string email)
    {
        var result = Empresa.Criar("Estrela Click LTDA", email, "+55 11 99999-9999");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("empresa.email.invalido");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Criar_DeveFalhar_QuandoTelefoneForInvalido(string telefone)
    {
        var result = Empresa.Criar("Estrela Click LTDA", "contato@estrelaclick.com", telefone);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("empresa.telefone.invalido");
    }

    [Fact]
    public void Criar_DevePermitirIdentityUserIdNulo()
    {
        var result = Empresa.Criar("Estrela Click LTDA", "contato@estrelaclick.com", "+55 11 99999-9999");

        result.IsSuccess.Should().BeTrue();
        result.Value!.IdentityUserId.Should().BeNull();
    }
}
