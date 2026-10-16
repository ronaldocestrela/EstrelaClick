using EstrelaClick.Shared.SharedKernel.Results;

namespace EstrelaClick.Backend.Domain.Entities;

public sealed class AcessoMetrica
{
    private AcessoMetrica(
        Guid id,
        Guid placaId,
        DateTime dataHoraUtc,
        string? userAgent,
        string ipHash,
        string? origemReferer)
    {
        Id = id;
        PlacaId = placaId;
        DataHoraUtc = dataHoraUtc;
        UserAgent = userAgent;
        IpHash = ipHash;
        OrigemReferer = origemReferer;
    }

    public Guid Id { get; private set; }

    public Guid PlacaId { get; private set; }

    public DateTime DataHoraUtc { get; private set; }

    public string? UserAgent { get; private set; }

    public string IpHash { get; private set; }

    public string? OrigemReferer { get; private set; }

    public static Result<AcessoMetrica> Criar(Guid placaId, string? userAgent, string ipHash, string? origemReferer)
    {
        if (placaId == Guid.Empty)
        {
            return Result<AcessoMetrica>.Failure(new Error("acessoMetrica.placaId.invalido", "PlacaId é obrigatório para registrar métrica."));
        }

        if (string.IsNullOrWhiteSpace(ipHash))
        {
            return Result<AcessoMetrica>.Failure(new Error("acessoMetrica.ipHash.invalido", "IpHash é obrigatório para registrar métrica."));
        }

        var metrica = new AcessoMetrica(
            id: Guid.NewGuid(),
            placaId: placaId,
            dataHoraUtc: DateTime.UtcNow,
            userAgent: string.IsNullOrWhiteSpace(userAgent) ? null : userAgent.Trim(),
            ipHash: ipHash.Trim(),
            origemReferer: string.IsNullOrWhiteSpace(origemReferer) ? null : origemReferer.Trim());

        return Result<AcessoMetrica>.Success(metrica);
    }
}
