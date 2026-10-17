using Bunit;
using EstrelaClick.Frontend.AdminDashboard.Components;
using EstrelaClick.Frontend.AdminDashboard.Components.Layout;
using FluentAssertions;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EstrelaClick.Tests.UnitTests.UI.Pages;

public class AdminAuthComponentsTests : BunitContext
{
    [Fact]
    public void AuthLayout_DeveRenderizarConteudoFilhoEFooter()
    {
        // Act
        var cut = Render<AuthLayout>(p => p.Add(x => x.Body, builder =>
        {
            builder.AddMarkupContent(0, "<div id=\"teste-conteudo\">Painel de Teste</div>");
        }));

        // Assert
        cut.Markup.Should().Contain("teste-conteudo");
        cut.Markup.Should().Contain("Painel de Teste");
        cut.Markup.Should().Contain("EstrelaClick");
        cut.Markup.Should().Contain("Plataforma Industrial NFC / QR Code");
    }

    [Fact]
    public void RedirectToLogin_DeveNavegarParaLoginComReturnUrl()
    {
        // Arrange
        var nav = Services.GetRequiredService<NavigationManager>();
        nav.NavigateTo("http://localhost/lotes");

        // Act
        var cut = Render<RedirectToLogin>();

        // Assert
        nav.Uri.Should().Be("http://localhost/login?ReturnUrl=%2Flotes");
    }

    [Fact]
    public void RedirectToLogin_NaRaiz_DeveNavegarParaLoginComReturnUrlRaiz()
    {
        // Arrange
        var nav = Services.GetRequiredService<NavigationManager>();
        nav.NavigateTo("http://localhost/");

        // Act
        var cut = Render<RedirectToLogin>();

        // Assert
        nav.Uri.Should().Be("http://localhost/login?ReturnUrl=%2F");
    }
}
