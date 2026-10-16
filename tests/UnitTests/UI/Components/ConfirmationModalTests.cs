using System;
using System.Threading.Tasks;
using Bunit;
using EstrelaClick.Frontend.Shared.UI.Components.Modals;
using FluentAssertions;
using Xunit;

namespace EstrelaClick.Tests.UnitTests.UI.Components;

public class ConfirmationModalTests : BunitContext
{
    [Fact]
    public void Render_QuandoIsOpenEhFalso_NaoDeveRenderizarDialogo()
    {
        // Act
        var cut = Render<ConfirmationModal>(
            builder => builder.Add(p => p.IsOpen, false)
                              .Add(p => p.Title, "Confirmar Ação"));

        // Assert
        cut.FindAll("div.modal-backdrop").Should().BeEmpty();
    }

    [Fact]
    public void Render_QuandoIsOpenEhVerdadeiro_DeveRenderizarTituloEMensagem()
    {
        // Act
        var cut = Render<ConfirmationModal>(
            builder => builder.Add(p => p.IsOpen, true)
                              .Add(p => p.Title, "Atenção Operador")
                              .Add(p => p.Message, "Deseja realmente bloquear esta placa?"));

        // Assert
        cut.Find("div.modal-backdrop").Should().NotBeNull();
        cut.Find("h3.modal-title").TextContent.Should().Be("Atenção Operador");
        cut.Find("p.modal-message").TextContent.Should().Be("Deseja realmente bloquear esta placa?");
    }

    [Fact]
    public async Task Click_BotaoConfirmar_DeveDispararEventoOnConfirm()
    {
        // Arrange
        var confirmou = false;
        var cut = Render<ConfirmationModal>(
            builder => builder.Add(p => p.IsOpen, true)
                              .Add(p => p.OnConfirm, () => { confirmou = true; }));

        // Act
        await cut.Find("button.btn-confirm").ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

        // Assert
        confirmou.Should().BeTrue();
    }

    [Fact]
    public async Task Click_BotaoCancelar_DeveDispararEventoOnCancel()
    {
        // Arrange
        var cancelou = false;
        var cut = Render<ConfirmationModal>(
            builder => builder.Add(p => p.IsOpen, true)
                              .Add(p => p.OnCancel, () => { cancelou = true; }));

        // Act
        await cut.Find("button.btn-cancel").ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

        // Assert
        cancelou.Should().BeTrue();
    }

    [Fact]
    public void Render_QuandoVarianteEhDanger_DeveAplicarClasseDanger()
    {
        // Act
        var cut = Render<ConfirmationModal>(
            builder => builder.Add(p => p.IsOpen, true)
                              .Add(p => p.IsDestructive, true));

        // Assert
        cut.Find("button.btn-confirm").ClassList.Should().Contain("btn-danger");
    }
}
