using EstrelaClick.Backend.Application.DTOs;
using EstrelaClick.Backend.Infrastructure.Telemetry;
using FluentAssertions;
using Xunit;

namespace EstrelaClick.Tests.UnitTests.Infrastructure.Telemetry;

public sealed class ChannelMetricaAcessoQueueTests
{
    [Fact]
    public async Task TryEnqueue_QuandoHaCapacidade_DeveAdicionarItemParaLeitura()
    {
        // Arrange
        var queue = new ChannelMetricaAcessoQueue(capacity: 10);
        var metrica = new MetricaAcessoEvent(
            PlacaId: Guid.NewGuid(),
            DataHoraUtc: DateTime.UtcNow,
            UserAgent: "TestAgent",
            IpHash: "hash123",
            OrigemReferer: "https://origin.com");

        // Act
        var enqueued = queue.TryEnqueue(metrica);

        // Assert
        enqueued.Should().BeTrue();
        var item = await queue.Reader.ReadAsync();
        item.Should().Be(metrica);
    }

    [Fact]
    public async Task TryEnqueue_QuandoFilaEstaCheia_ComPoliticaDropWrite_NaoBloqueiaEDescartaNovoItem()
    {
        // Arrange
        var queue = new ChannelMetricaAcessoQueue(capacity: 1);
        var metrica1 = new MetricaAcessoEvent(Guid.NewGuid(), DateTime.UtcNow, "Agent1", "hash1", null);
        var metrica2 = new MetricaAcessoEvent(Guid.NewGuid(), DateTime.UtcNow, "Agent2", "hash2", null);

        // Act
        var first = queue.TryEnqueue(metrica1);
        var second = queue.TryEnqueue(metrica2);

        // Assert
        first.Should().BeTrue();
        second.Should().BeTrue(); // Com DropWrite, TryWrite aceita a operação descartando o novo item sem travar
        var itemLido = await queue.Reader.ReadAsync();
        itemLido.Should().Be(metrica1); // Garante que o primeiro foi preservado e o segundo descartado
        queue.Reader.TryRead(out _).Should().BeFalse(); // Nenhum item restante na fila
    }
}
