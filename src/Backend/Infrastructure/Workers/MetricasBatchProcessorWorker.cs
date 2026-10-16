using EstrelaClick.Backend.Application.DTOs;
using EstrelaClick.Backend.Domain.Entities;
using EstrelaClick.Backend.Infrastructure.Persistence;
using EstrelaClick.Backend.Infrastructure.Telemetry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EstrelaClick.Backend.Infrastructure.Workers;

public sealed class MetricasBatchProcessorWorker : BackgroundService
{
    private const int BatchSize = 100;
    private static readonly TimeSpan FlushInterval = TimeSpan.FromSeconds(2);

    private readonly ChannelMetricaAcessoQueue _queue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<MetricasBatchProcessorWorker> _logger;

    public MetricasBatchProcessorWorker(
        ChannelMetricaAcessoQueue queue,
        IServiceScopeFactory scopeFactory,
        ILogger<MetricasBatchProcessorWorker> logger)
    {
        _queue = queue;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Iniciando MetricasBatchProcessorWorker com limite de lote de {BatchSize} itens e intervalo de {Interval}s.",
            BatchSize, FlushInterval.TotalSeconds);

        var batch = new List<MetricaAcessoEvent>(BatchSize);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Espera pelo primeiro item do lote
                if (await _queue.Reader.WaitToReadAsync(stoppingToken))
                {
                    var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
                    timeoutCts.CancelAfter(FlushInterval);

                    try
                    {
                        while (batch.Count < BatchSize && _queue.Reader.TryRead(out var item))
                        {
                            batch.Add(item);
                        }

                        // Se ainda não atingiu o tamanho do lote, espera com timeout para coletar mais itens
                        while (batch.Count < BatchSize)
                        {
                            if (await _queue.Reader.WaitToReadAsync(timeoutCts.Token))
                            {
                                while (batch.Count < BatchSize && _queue.Reader.TryRead(out var item))
                                {
                                    batch.Add(item);
                                }
                            }
                            else
                            {
                                break;
                            }
                        }
                    }
                    catch (OperationCanceledException) when (!stoppingToken.IsCancellationRequested)
                    {
                        // Timeout atingido, procede com o flush do que foi acumulado
                    }

                    if (batch.Count > 0)
                    {
                        await SalvarLoteAsync(batch, stoppingToken);
                        batch.Clear();
                    }
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro inesperado no loop de processamento em lote de métricas.");
                await Task.Delay(1000, stoppingToken);
            }
        }

        // Drena qualquer resíduo antes de desligar
        while (_queue.Reader.TryRead(out var remaining))
        {
            batch.Add(remaining);
            if (batch.Count >= BatchSize)
            {
                await SalvarLoteAsync(batch, CancellationToken.None);
                batch.Clear();
            }
        }

        if (batch.Count > 0)
        {
            await SalvarLoteAsync(batch, CancellationToken.None);
        }

        _logger.LogInformation("MetricasBatchProcessorWorker finalizado.");
    }

    private async Task SalvarLoteAsync(List<MetricaAcessoEvent> eventos, CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<EstrelaClickDbContext>();

            var entidades = new List<AcessoMetrica>(eventos.Count);
            foreach (var ev in eventos)
            {
                var result = AcessoMetrica.Criar(ev.PlacaId, ev.UserAgent, ev.IpHash, ev.OrigemReferer);
                if (result.IsSuccess && result.Value is not null)
                {
                    entidades.Add(result.Value);
                }
            }

            if (entidades.Count > 0)
            {
                await dbContext.AcessosMetricas.AddRangeAsync(entidades, cancellationToken);
                await dbContext.SaveChangesAsync(cancellationToken);
                _logger.LogDebug("Despejo de {Count} métricas de acesso persistido no banco de dados.", entidades.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao persistir lote de {Count} métricas de acesso no banco de dados.", eventos.Count);
        }
    }
}
