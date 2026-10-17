using Bunit;
using EstrelaClick.Frontend.AdminDashboard.Components.Pages;
using FluentAssertions;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EstrelaClick.Tests.UnitTests.UI.Pages;

public class AdminLoginTests : BunitContext
{
    [Fact]
    public void Login_DeveRenderizarFormularioComCamposUsuarioESenha()
    {
        // Act
        var cut = Render<Login>();

        // Assert
        cut.Markup.Should().Contain("Acesso Restrito");
        cut.Markup.Should().Contain("Gestão Fabril & Backoffice");
        cut.Markup.Should().Contain("E-mail do Administrador");
        
        var form = cut.Find("form");
        form.GetAttribute("action").Should().Be("/auth/admin-login");
        form.GetAttribute("method").Should().Be("post");

        var inputUsuario = cut.Find("input[name='usuario']");
        inputUsuario.Should().NotBeNull();
        inputUsuario.GetAttribute("type").Should().Be("email");
        inputUsuario.GetAttribute("placeholder").Should().Be("admin@estrelaclick.com.br");

        var inputSenha = cut.Find("input[name='senha']");
        inputSenha.Should().NotBeNull();
        inputSenha.GetAttribute("type").Should().Be("password");

        var btnSubmit = cut.Find("button[type='submit']");
        btnSubmit.TextContent.Should().Contain("Entrar no Painel");
    }

    [Fact]
    public void Login_QuandoQueryStringErrorCredenciaisInvalidas_DeveExibirMensagemErro()
    {
        // Arrange
        var nav = Services.GetRequiredService<NavigationManager>();
        nav.NavigateTo("http://localhost/login?Error=credenciais_invalidas");

        // Act
        var cut = Render<Login>();

        // Assert
        cut.Markup.Should().Contain("Usuário ou senha incorretos.");
    }

    [Fact]
    public void Login_QuandoQueryStringErrorCamposObrigatorios_DeveExibirMensagemErro()
    {
        // Arrange
        var nav = Services.GetRequiredService<NavigationManager>();
        nav.NavigateTo("http://localhost/login?Error=campos_obrigatorios");

        // Act
        var cut = Render<Login>();

        // Assert
        cut.Markup.Should().Contain("Informe o e-mail e a senha para acessar o painel.");
    }

    [Fact]
    public void Login_QuandoQueryStringErrorConfiguracaoInvalida_DeveExibirMensagemErro()
    {
        // Arrange
        var nav = Services.GetRequiredService<NavigationManager>();
        nav.NavigateTo("http://localhost/login?Error=configuracao_invalida");

        // Act
        var cut = Render<Login>();

        // Assert
        cut.Markup.Should().Contain("Credenciais de administrador master não configuradas no ambiente.");
    }

    [Fact]
    public void Login_ComReturnUrl_DeveIncluirNoInputHidden()
    {
        // Arrange
        var nav = Services.GetRequiredService<NavigationManager>();
        nav.NavigateTo("http://localhost/login?ReturnUrl=%2Flotes");

        // Act
        var cut = Render<Login>();

        // Assert
        var inputReturnUrl = cut.Find("input[name='returnUrl']");
        inputReturnUrl.GetAttribute("value").Should().Be("/lotes");
    }
}
