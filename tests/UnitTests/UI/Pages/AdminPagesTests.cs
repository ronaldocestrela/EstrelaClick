using Bunit;
using EstrelaClick.Frontend.AdminDashboard.Components.Modais;
using EstrelaClick.Frontend.AdminDashboard.Components.Pages;
using EstrelaClick.Frontend.AdminDashboard.Services;
using EstrelaClick.Shared.SharedKernel.Admin;
using EstrelaClick.Shared.SharedKernel.Enums;
using EstrelaClick.Shared.SharedKernel.Results;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Xunit;

namespace EstrelaClick.Tests.UnitTests.UI.Pages;

public class AdminPagesTests : BunitContext
{
    private readonly IAdminApiClient _apiClientMock = Substitute.For<IAdminApiClient>();

    public AdminPagesTests()
    {
        Services.AddSingleton(_apiClientMock);
    }

    [Fact]
    public void Home_DeveRenderizarMetricasELotesRecentes()
    {
        // Arrange
        var metricas = new MetricasFabricaDto(
            TotalPlacas: 1500,
            TotalCriadas: 500,
            TotalEmGravacao: 50,
            TotalGravadas: 650,
            TotalVinculadas: 300,
            TotalLotes: 12
        );
        _apiClientMock.ObterMetricasFabricaAsync(Arg.Any<CancellationToken>())
            .Returns(Result<MetricasFabricaDto>.Success(metricas));

        var lotes = new List<LoteItemDto>
        {
            new("LOTE-REC-01", 100, 20, 10, 50, 20, DateTime.UtcNow)
        };
        _apiClientMock.ListarLotesAsync(Arg.Any<CancellationToken>())
            .Returns(Result<IReadOnlyList<LoteItemDto>>.Success(lotes));
        _apiClientMock.ObterUrlDownloadZipLote(Arg.Any<string>())
            .Returns("http://localhost/api/placas/lotes/LOTE-REC-01/exportar-zip");

        // Act
        var cut = Render<Home>();

        // Assert
        cut.Markup.Should().Contain("Painel Fabril & Estoque");
        cut.Markup.Should().Contain("1.500");
        cut.Markup.Should().Contain("650");
        cut.Markup.Should().Contain("300");
        cut.Markup.Should().Contain("LOTE-REC-01");
    }

    [Fact]
    public void Lotes_DeveRenderizarTabelaELinkDownloadZip()
    {
        // Arrange
        var lotes = new List<LoteItemDto>
        {
            new("LOTE-EXP-2026", 200, 50, 10, 100, 40, DateTime.UtcNow)
        };
        _apiClientMock.ListarLotesAsync(Arg.Any<CancellationToken>())
            .Returns(Result<IReadOnlyList<LoteItemDto>>.Success(lotes));
        _apiClientMock.ObterUrlDownloadZipLote("LOTE-EXP-2026")
            .Returns("http://localhost/api/placas/lotes/LOTE-EXP-2026/exportar-zip");

        // Act
        var cut = Render<Lotes>();

        // Assert
        cut.Markup.Should().Contain("LOTE-EXP-2026");
        cut.Markup.Should().Contain("Download ZIP");
    }

    [Fact]
    public void Clientes_DeveRenderizarTabelaDeEmpresas()
    {
        // Arrange
        var empresas = new List<EmpresaDto>
        {
            new(Guid.NewGuid(), "Restaurante Estrela", "restaurante@estrela.com", "(11) 91111-2222", true, DateTime.UtcNow, 8)
        };
        _apiClientMock.ListarEmpresasAsync(Arg.Any<CancellationToken>())
            .Returns(Result<IReadOnlyList<EmpresaDto>>.Success(empresas));

        // Act
        var cut = Render<Clientes>();

        // Assert
        cut.Markup.Should().Contain("Restaurante Estrela");
        cut.Markup.Should().Contain("restaurante@estrela.com");
    }

    [Fact]
    public void Auditoria_QuandoBuscaRealizada_DeveExibirFichaDaPlaca()
    {
        // Arrange
        var slug = "a1b2c3d4e5f678901234567890123456";
        var auditoria = new PlacaAuditoriaDto(
            Guid.NewGuid(), slug, "LOTE-2026", StatusPlaca.Gravada,
            "04:EE:FF:11:22:33", "https://meunegocio.com", null, null, "OpFabrica",
            DateTime.UtcNow.AddDays(-2), DateTime.UtcNow.AddDays(-1), null, 77
        );
        _apiClientMock.ConsultarAuditoriaAsync(slug, Arg.Any<CancellationToken>())
            .Returns(Result<PlacaAuditoriaDto>.Success(auditoria));

        // Act
        var cut = Render<Auditoria>();
        cut.Find("input#termo-auditoria").Input(slug);
        cut.Find("form").Submit();

        // Assert
        cut.Markup.Should().Contain(slug);
        cut.Markup.Should().Contain("04:EE:FF:11:22:33");
        cut.Markup.Should().Contain("77");
    }

    [Fact]
    public async Task GerarLoteModal_QuandoConfirmado_DeveChamarGerarLoteERetornarCallback()
    {
        // Arrange
        var loteCriado = new LoteCriadoDto("LOTE-NOVO-1", 100, DateTime.UtcNow, new[] { "s1", "s2" });
        _apiClientMock.GerarLoteAsync("LOTE-NOVO-1", 100, Arg.Any<CancellationToken>())
            .Returns(Result<LoteCriadoDto>.Success(loteCriado));

        LoteCriadoDto? callbackRecebido = null;
        var cut = Render<GerarLoteModal>(p => p
            .Add(m => m.IsOpen, true)
            .Add(m => m.OnLoteCriado, dto => callbackRecebido = dto));

        // Act
        cut.Find("input#lote-prefixo").Input("LOTE-NOVO-1");
        cut.Find("input#lote-quantidade").Input("100");

        var btnConfirm = cut.Find("button.btn-confirm");
        await btnConfirm.ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

        // Assert
        callbackRecebido.Should().NotBeNull();
        callbackRecebido!.NumeroLote.Should().Be("LOTE-NOVO-1");
        callbackRecebido.QuantidadeCriada.Should().Be(100);
    }

    [Fact]
    public async Task CadastrarClienteModal_QuandoConfirmado_DeveChamarCadastrarEmpresa()
    {
        // Arrange
        var empresaCriada = new EmpresaDto(Guid.NewGuid(), "Padaria Central", "contato@central.com", "(11) 4444-5555", true, DateTime.UtcNow, 0);
        _apiClientMock.CadastrarEmpresaAsync(Arg.Any<CriarEmpresaRequest>(), Arg.Any<CancellationToken>())
            .Returns(Result<EmpresaDto>.Success(empresaCriada));

        EmpresaDto? callbackRecebido = null;
        var cut = Render<CadastrarClienteModal>(p => p
            .Add(m => m.IsOpen, true)
            .Add(m => m.OnClienteCadastrado, dto => callbackRecebido = dto));

        // Act
        cut.Find("input#cliente-razao-social").Input("Padaria Central");
        cut.Find("input#cliente-email").Input("contato@central.com");
        cut.Find("input#cliente-telefone").Input("(11) 4444-5555");

        var btnConfirm = cut.Find("button.btn-confirm");
        await btnConfirm.ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

        // Assert
        callbackRecebido.Should().NotBeNull();
        callbackRecebido!.RazaoSocial.Should().Be("Padaria Central");
    }
}
