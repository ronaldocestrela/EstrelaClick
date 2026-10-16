using EstrelaClick.Backend.Application.DTOs;

namespace EstrelaClick.Backend.Application.Ports.Outbound;

public interface IMetricaAcessoQueue
{
    bool TryEnqueue(MetricaAcessoEvent metrica);
}
