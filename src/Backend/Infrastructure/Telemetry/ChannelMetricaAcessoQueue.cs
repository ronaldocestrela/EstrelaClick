using System.Threading.Channels;
using EstrelaClick.Backend.Application.DTOs;
using EstrelaClick.Backend.Application.Ports.Outbound;

namespace EstrelaClick.Backend.Infrastructure.Telemetry;

public sealed class ChannelMetricaAcessoQueue : IMetricaAcessoQueue
{
    private readonly Channel<MetricaAcessoEvent> _channel;

    public ChannelMetricaAcessoQueue(int capacity = 50_000)
    {
        var options = new BoundedChannelOptions(capacity)
        {
            FullMode = BoundedChannelFullMode.DropWrite,
            SingleReader = true,
            SingleWriter = false
        };

        _channel = Channel.CreateBounded<MetricaAcessoEvent>(options);
    }

    public bool TryEnqueue(MetricaAcessoEvent metrica)
    {
        return _channel.Writer.TryWrite(metrica);
    }

    public ChannelReader<MetricaAcessoEvent> Reader => _channel.Reader;
}
