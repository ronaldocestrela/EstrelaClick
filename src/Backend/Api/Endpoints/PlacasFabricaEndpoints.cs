using System.Security.Claims;
using EstrelaClick.Backend.Application.Common.Errors;
using EstrelaClick.Backend.Application.DTOs;
using EstrelaClick.Backend.Application.Ports.Inbound;
using Microsoft.AspNetCore.Mvc;

namespace EstrelaClick.Backend.Api.Endpoints;

public static class PlacasFabricaEndpoints
{
    public static IEndpointRouteBuilder MapPlacasFabricaEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/placas")
            .WithTags("Fábrica e Gravação Mobile")
            .RequireAuthorization("FactoryAuthPolicy");

        group.MapPost("/gerar-lote", async (
            [FromBody] GerarLotePlacasCommand command,
            IGerarLotePlacasUseCase useCase,
            CancellationToken cancellationToken) =>
        {
            var result = await useCase.ExecutarAsync(command, cancellationToken);

            if (result.IsSuccess && result.Value is not null)
            {
                return Results.Created($"/api/placas/lotes/{result.Value.NumeroLote}", result.Value);
            }

            if (result.Error == PlacaErrors.LoteJaExiste)
            {
                return Results.Conflict(new ProblemDetails
                {
                    Title = "Lote duplicado",
                    Detail = result.Error.Description,
                    Status = StatusCodes.Status409Conflict,
                    Extensions = { ["errorCode"] = result.Error.Code }
                });
            }

            return Results.BadRequest(new ProblemDetails
            {
                Title = "Requisição inválida",
                Detail = result.Error.Description,
                Status = StatusCodes.Status400BadRequest,
                Extensions = { ["errorCode"] = result.Error.Code }
            });
        })
        .WithName("GerarLotePlacas")
        .WithSummary("Gera um novo lote de placas com slugs aleatórios seguros")
        .WithDescription("Cria em lote placas com identificadores aleatórios seguros no formato GUID sem hífens para futura gravação fabril.")
        .Produces<LotePlacasCriadoDto>(StatusCodes.Status201Created)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapGet("/proxima-pendente", async (
            [FromQuery] string? numeroLote,
            ClaimsPrincipal user,
            IObterProximaPlacaPendenteUseCase useCase,
            CancellationToken cancellationToken) =>
        {
            var operadorId = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.Identity?.Name ?? "OperadorBancada";
            var query = new ObterProximaPlacaPendenteQuery(numeroLote, operadorId);

            var result = await useCase.ExecutarAsync(query, cancellationToken);

            if (result.IsSuccess && result.Value is not null)
            {
                return Results.Ok(result.Value);
            }

            return Results.NotFound(new ProblemDetails
            {
                Title = "Fila vazia",
                Detail = result.Error.Description,
                Status = StatusCodes.Status404NotFound,
                Extensions = { ["errorCode"] = result.Error.Code }
            });
        })
        .WithName("ObterProximaPlacaPendente")
        .WithSummary("Busca a próxima placa pendente na fila para o operador de bancada")
        .WithDescription("Aloca atomicamente a próxima placa elegível (FIFO) com reserva temporária (TTL de 90s), retornando a URL para gravação NDEF.")
        .Produces<PlacaPendenteDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapGet("/lotes/{numeroLote}/resumo", async (
            [FromRoute] string numeroLote,
            IObterResumoLoteUseCase useCase,
            CancellationToken cancellationToken) =>
        {
            var result = await useCase.ExecutarAsync(numeroLote, cancellationToken);

            if (result.IsSuccess && result.Value is not null)
            {
                return Results.Ok(result.Value);
            }

            if (result.Error == PlacaErrors.LoteNaoEncontrado)
            {
                return Results.NotFound(new ProblemDetails
                {
                    Title = "Lote não encontrado",
                    Detail = result.Error.Description,
                    Status = StatusCodes.Status404NotFound,
                    Extensions = { ["errorCode"] = result.Error.Code }
                });
            }

            return Results.BadRequest(new ProblemDetails
            {
                Title = "Requisição inválida",
                Detail = result.Error.Description,
                Status = StatusCodes.Status400BadRequest,
                Extensions = { ["errorCode"] = result.Error.Code }
            });
        })
        .WithName("ObterResumoLote")
        .WithSummary("Retorna o resumo de métricas do lote de placas")
        .WithDescription("Calcula o total de placas, quantidade gravada, pendente e vinculada para acompanhamento da bancada fabril.")
        .Produces<LoteResumoDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/confirmar-gravacao", async (
            [FromBody] ConfirmarGravacaoPlacaCommand command,
            ClaimsPrincipal user,
            IConfirmarGravacaoPlacaUseCase useCase,
            CancellationToken cancellationToken) =>
        {
            var operadorId = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.Identity?.Name;
            var commandComOperador = command with { OperadorId = operadorId };

            var result = await useCase.ExecutarAsync(commandComOperador, cancellationToken);

            if (result.IsSuccess && result.Value is not null)
            {
                return Results.Ok(result.Value);
            }

            if (result.Error == PlacaErrors.NaoEncontrada)
            {
                return Results.NotFound(new ProblemDetails
                {
                    Title = "Placa não encontrada",
                    Detail = result.Error.Description,
                    Status = StatusCodes.Status404NotFound,
                    Extensions = { ["errorCode"] = result.Error.Code }
                });
            }

            if (result.Error == PlacaErrors.StatusTransicaoInvalida || result.Error.Code == "placa.status.transicao.invalida")
            {
                return Results.Conflict(new ProblemDetails
                {
                    Title = "Transição de status inválida",
                    Detail = result.Error.Description,
                    Status = StatusCodes.Status409Conflict,
                    Extensions = { ["errorCode"] = result.Error.Code }
                });
            }

            return Results.BadRequest(new ProblemDetails
            {
                Title = "Requisição inválida",
                Detail = result.Error.Description,
                Status = StatusCodes.Status400BadRequest,
                Extensions = { ["errorCode"] = result.Error.Code }
            });
        })
        .WithName("ConfirmarGravacaoPlaca")
        .WithSummary("Registra a confirmação física de gravação NDEF de uma placa")
        .WithDescription("Associa o UID físico do chip NFC à placa e efetua a transição atômica de status para Gravada.")
        .Produces<PlacaGravadaDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict);

        return app;
    }
}
