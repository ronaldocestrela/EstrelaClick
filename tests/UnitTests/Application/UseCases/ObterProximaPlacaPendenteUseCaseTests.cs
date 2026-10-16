using EstrelaClick.Backend.Application.Common.Errors;
using EstrelaClick.Backend.Application.DTOs;
using EstrelaClick.Backend.Application.Ports.Outbound;
using EstrelaClick.Backend.Application.UseCases;
using EstrelaClick.Backend.Domain.Entities;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using NSubstitute;

namespace EstrelaClick.Tests.UnitTests.Application.UseCases;

public class ObterProximaPlacaPendenteUseCaseTests
{
    private readonly IPlacaRepository _placaRepository;
    private readonly IConfiguration _configuration;
    private readonly ObterProximaPlacaPendenteUseCase _useCase;

    public ObterProximaPlacaPendenteUseCaseTests()
    {
        _placaRepository = Substitute.For<IPlacaRepository>();
        var inMemorySettings = new Dictionary<string, string?>
        {
            {"App:BaseRedirectUrl", "https://redirect.estrelaclick.com.br"}
        };
        _configuration = new ConfigurationBuilder().AddInMemoryCollection(inMemorySettings).Build();
        _useCase = new ObterProximaPlacaPendenteUseCase(_placaRepository, _configuration);
    }

    [Fact]
    public async Task ExecutarAsync_DeveRetornarFilaVazia_QuandoNaoHouverPlacaDisponivel()
    {
        _placaRepository
            .ObterProximaPendenteComReservaAsync(Arg.Any<string?>(), Arg.Any<TimeSpan>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns((Placa?)null);

        var query = new ObterProximaPlacaPendenteQuery("LOTE-01", "operador-01");
        var result = await _useCase.ExecutarAsync(query);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(PlacaErrors.FilaVazia);
    }

    [Fact]
    public async Task ExecutarAsync_DeveRetornarPlacaPendenteComUrlRedirecionamento_QuandoHouverPlaca()
    {
        var slug = Guid.NewGuid().ToString("N");
        var placa = Placa.Criar(slug, "LOTE-01").Value!;
        var momentoUtc = DateTime.UtcNow;
        placa.ReservarParaGravacao(TimeSpan.FromSeconds(90), "operador-01", momentoUtc);

        _placaRepository
            .ObterProximaPendenteComReservaAsync("LOTE-01", Arg.Any<TimeSpan>(), "operador-01", Arg.Any<CancellationToken>())
            .Returns(placa);

        var query = new ObterProximaPlacaPendenteQuery("LOTE-01", "operador-01");
        var result = await _useCase.ExecutarAsync(query);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Slug.Should().Be(slug);
        result.Value.NumeroLote.Should().Be("LOTE-01");
        result.Value.UrlRedirecionamento.Should().Be($"https://redirect.estrelaclick.com.br/r/{slug}");
        result.Value.OperadorReservaId.Should().Be("operador-01");
    }
}
