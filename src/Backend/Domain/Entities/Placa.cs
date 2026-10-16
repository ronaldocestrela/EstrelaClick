using EstrelaClick.Shared.SharedKernel.Enums;
using EstrelaClick.Shared.SharedKernel.Results;

namespace EstrelaClick.Backend.Domain.Entities;

public sealed class Placa
{
    private Placa(Guid id, string slug, string numeroLote, DateTime dataCriacaoUtc)
    {
        Id = id;
        Slug = slug;
        NumeroLote = numeroLote;
        StatusPlaca = StatusPlaca.Criada;
        DataCriacaoUtc = dataCriacaoUtc;
    }

    public Guid Id { get; private set; }

    public string Slug { get; private set; }

    public string NumeroLote { get; private set; }

    public string? UrlDestino { get; private set; }

    public StatusPlaca StatusPlaca { get; private set; }

    public string? ChipUid { get; private set; }

    public DateTime DataCriacaoUtc { get; private set; }

    public DateTime? DataReservaUtc { get; private set; }

    public string? OperadorReservaId { get; private set; }

    public DateTime? DataGravacaoUtc { get; private set; }

    public DateTime? DataAtivacaoUtc { get; private set; }

    public Guid? EmpresaId { get; private set; }

    public bool FiltroSatisfacaoAtivo { get; private set; }

    public string? UrlSacInterno { get; private set; }

    public static Result<Placa> Criar(string slug) => Criar(slug, "PADRAO");

    public static Result<Placa> Criar(string slug, string numeroLote)
    {
        if (string.IsNullOrWhiteSpace(slug) || !Guid.TryParseExact(slug, "N", out _))
        {
            return Result<Placa>.Failure(new Error("placa.slug.invalido", "Slug deve ser um GUID sem hífens."));
        }

        if (string.IsNullOrWhiteSpace(numeroLote) || numeroLote.Trim().Length > 32)
        {
            return Result<Placa>.Failure(new Error("placa.numeroLote.invalido", "Número de lote é obrigatório e deve ter no máximo 32 caracteres."));
        }

        var placa = new Placa(Guid.NewGuid(), slug, numeroLote.Trim(), DateTime.UtcNow);

        return Result<Placa>.Success(placa);
    }

    public Result ReservarParaGravacao(TimeSpan ttlReserva, string? operadorId, DateTime momentoUtc)
    {
        if (StatusPlaca != StatusPlaca.Criada && StatusPlaca != StatusPlaca.EmGravacao)
        {
            return Result.Failure(new Error("placa.status.transicao.invalida", "A placa não pode ser reservada pois não está no status Criada ou EmGravacao."));
        }

        if (StatusPlaca == StatusPlaca.EmGravacao && DataReservaUtc.HasValue)
        {
            var expirou = DataReservaUtc.Value.Add(ttlReserva) < momentoUtc;
            if (!expirou)
            {
                return Result.Failure(new Error("placa.reserva.ativa", "A placa já está reservada por outro operador e a reserva ainda não expirou."));
            }
        }

        StatusPlaca = StatusPlaca.EmGravacao;
        DataReservaUtc = momentoUtc;
        OperadorReservaId = string.IsNullOrWhiteSpace(operadorId) ? null : operadorId.Trim();

        return Result.Success();
    }

    public Result LiberarReserva()
    {
        if (StatusPlaca != StatusPlaca.EmGravacao)
        {
            return Result.Failure(new Error("placa.status.transicao.invalida", "Apenas placas em gravação podem ter a reserva liberada."));
        }

        StatusPlaca = StatusPlaca.Criada;
        DataReservaUtc = null;
        OperadorReservaId = null;

        return Result.Success();
    }

    public Result RegistrarGravacao(string chipUid)
    {
        if (StatusPlaca != StatusPlaca.Criada && StatusPlaca != StatusPlaca.EmGravacao)
        {
            return Result.Failure(new Error("placa.status.transicao.invalida", "A placa precisa estar no status Criada ou EmGravacao para registrar gravação."));
        }

        if (string.IsNullOrWhiteSpace(chipUid))
        {
            return Result.Failure(new Error("placa.chipUid.invalido", "ChipUid é obrigatório para registrar gravação."));
        }

        ChipUid = chipUid.Trim();
        DataGravacaoUtc = DateTime.UtcNow;
        DataReservaUtc = null;
        OperadorReservaId = null;
        StatusPlaca = StatusPlaca.Gravada;

        return Result.Success();
    }

    public Result VincularEmpresa(Guid empresaId, string urlDestino)
    {
        if (StatusPlaca != StatusPlaca.Gravada)
        {
            return Result.Failure(new Error("placa.status.transicao.invalida", "A placa precisa estar no status Gravada para vincular empresa."));
        }

        if (empresaId == Guid.Empty)
        {
            return Result.Failure(new Error("placa.empresaId.invalido", "EmpresaId é obrigatório para vincular a placa."));
        }

        if (string.IsNullOrWhiteSpace(urlDestino)
            || !Uri.TryCreate(urlDestino.Trim(), UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            return Result.Failure(new Error("placa.urlDestino.invalida", "UrlDestino deve ser uma URL absoluta HTTP/HTTPS."));
        }

        EmpresaId = empresaId;
        UrlDestino = uri.ToString();
        DataAtivacaoUtc = DateTime.UtcNow;
        StatusPlaca = StatusPlaca.Vinculada;

        return Result.Success();
    }

    public Result AtualizarUrlDestino(string novaUrl)
    {
        if (StatusPlaca != StatusPlaca.Vinculada)
        {
            return Result.Failure(new Error("placa.status.transicao.invalida", "Apenas placas vinculadas podem ter a URL de destino alterada."));
        }

        if (string.IsNullOrWhiteSpace(novaUrl)
            || !Uri.TryCreate(novaUrl.Trim(), UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            return Result.Failure(new Error("placa.urlDestino.invalida", "UrlDestino deve ser uma URL absoluta HTTP/HTTPS."));
        }

        UrlDestino = uri.ToString();
        return Result.Success();
    }

    public Result ConfigurarFiltroSatisfacao(bool ativo, string? urlSacInterno)
    {
        if (StatusPlaca != StatusPlaca.Vinculada)
        {
            return Result.Failure(new Error("placa.status.transicao.invalida", "Apenas placas vinculadas podem configurar o filtro de satisfação."));
        }

        if (ativo)
        {
            if (string.IsNullOrWhiteSpace(urlSacInterno)
                || !Uri.TryCreate(urlSacInterno.Trim(), UriKind.Absolute, out var uri)
                || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            {
                return Result.Failure(new Error("placa.filtroSatisfacao.urlSacInvalida", "Url de SAC deve ser uma URL absoluta HTTP/HTTPS."));
            }

            FiltroSatisfacaoAtivo = true;
            UrlSacInterno = uri.ToString();
        }
        else
        {
            FiltroSatisfacaoAtivo = false;
            UrlSacInterno = null;
        }

        return Result.Success();
    }
}
