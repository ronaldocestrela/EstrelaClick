using EstrelaClick.Backend.Application.DTOs;
using EstrelaClick.Backend.Application.Ports.Inbound;
using EstrelaClick.Backend.Application.Ports.Outbound;
using EstrelaClick.Shared.SharedKernel.Enums;
using EstrelaClick.Shared.SharedKernel.Results;
using Microsoft.AspNetCore.Mvc;

namespace EstrelaClick.Backend.Api.Endpoints;

public static class ClientEndpoints
{
    public static IEndpointRouteBuilder MapClientEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/client")
            .WithTags("Cliente / Lojista");

        group.MapGet("/onboarding/verificar/{slug}", async (
            string slug,
            IPlacaRepository placaRepository,
            CancellationToken cancellationToken) =>
        {
            if (string.IsNullOrWhiteSpace(slug) || !Guid.TryParseExact(slug, "N", out _))
            {
                return Results.BadRequest(new ProblemDetails
                {
                    Title = "Slug inválido",
                    Detail = "O formato do slug fornecido é inválido.",
                    Status = StatusCodes.Status400BadRequest
                });
            }

            var placa = await placaRepository.ObterPorSlugAsync(slug, cancellationToken);
            if (placa is null)
            {
                return Results.NotFound(new ProblemDetails
                {
                    Title = "Placa não encontrada",
                    Detail = "A placa solicitada não foi localizada no sistema.",
                    Status = StatusCodes.Status404NotFound
                });
            }

            var disponivelParaAtivacao = placa.StatusPlaca == StatusPlaca.Gravada;

            return Results.Ok(new
            {
                PlacaId = placa.Id,
                Slug = placa.Slug,
                StatusPlaca = placa.StatusPlaca.ToString(),
                DisponivelParaAtivacao = disponivelParaAtivacao,
                UrlDestinoAtual = placa.UrlDestino,
                FiltroSatisfacaoAtivo = placa.FiltroSatisfacaoAtivo,
                UrlSacInterno = placa.UrlSacInterno
            });
        })
        .WithName("VerificarPlacaOnboarding")
        .WithSummary("Verifica elegibilidade da placa para Onboarding")
        .Produces(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/onboarding/ativar", async (
            [FromBody] AtivarPlacaOnboardingRequest request,
            IAtivarPlacaOnboardingUseCase useCase,
            CancellationToken cancellationToken) =>
        {
            var result = await useCase.ExecutarAsync(request, cancellationToken);

            if (result.IsFailure)
            {
                return Results.UnprocessableEntity(new ProblemDetails
                {
                    Title = "Falha na ativação da placa",
                    Detail = result.Error.Description,
                    Status = StatusCodes.Status422UnprocessableEntity,
                    Extensions = { ["errorCode"] = result.Error.Code }
                });
            }

            return Results.Ok(result.Value);
        })
        .WithName("AtivarPlacaOnboarding")
        .WithSummary("Ativa placa virgem de fábrica e vincula à empresa do lojista")
        .Produces<PlacaAtivadaDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        group.MapGet("/empresas/{empresaId:guid}/placas", async (
            Guid empresaId,
            IListarPlacasClienteUseCase useCase,
            CancellationToken cancellationToken) =>
        {
            var result = await useCase.ExecutarAsync(empresaId, cancellationToken);

            if (result.IsFailure)
            {
                return Results.UnprocessableEntity(new ProblemDetails
                {
                    Title = "Erro ao listar placas",
                    Detail = result.Error.Description,
                    Status = StatusCodes.Status422UnprocessableEntity,
                    Extensions = { ["errorCode"] = result.Error.Code }
                });
            }

            return Results.Ok(result.Value);
        })
        .WithName("ListarPlacasCliente")
        .WithSummary("Lista todas as placas vinculadas à empresa informada")
        .Produces<IReadOnlyList<PlacaClienteDto>>(StatusCodes.Status200OK);

        group.MapPut("/empresas/{empresaId:guid}/placas/{placaId:guid}/url", async (
            Guid empresaId,
            Guid placaId,
            [FromBody] AtualizarUrlPlacaRequest request,
            IAtualizarUrlPlacaUseCase useCase,
            CancellationToken cancellationToken) =>
        {
            if (placaId != request.PlacaId)
            {
                return Results.BadRequest(new ProblemDetails
                {
                    Title = "Identificador divergente",
                    Detail = "O Id da placa na rota difere do corpo da requisição.",
                    Status = StatusCodes.Status400BadRequest
                });
            }

            var result = await useCase.ExecutarAsync(empresaId, request, cancellationToken);

            if (result.IsFailure)
            {
                var status = result.Error.Code.Contains("naoAutorizado", StringComparison.OrdinalIgnoreCase)
                    ? StatusCodes.Status403Forbidden
                    : StatusCodes.Status422UnprocessableEntity;

                return Results.Json(new ProblemDetails
                {
                    Title = "Erro ao atualizar URL de destino",
                    Detail = result.Error.Description,
                    Status = status,
                    Extensions = { ["errorCode"] = result.Error.Code }
                }, statusCode: status);
            }

            return Results.NoContent();
        })
        .WithName("AtualizarUrlPlacaCliente")
        .WithSummary("Atualiza a URL de destino da placa em tempo real")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        group.MapPut("/empresas/{empresaId:guid}/placas/{placaId:guid}/filtro-satisfacao", async (
            Guid empresaId,
            Guid placaId,
            [FromBody] ConfigurarFiltroSatisfacaoRequest request,
            IConfigurarFiltroSatisfacaoUseCase useCase,
            CancellationToken cancellationToken) =>
        {
            if (placaId != request.PlacaId)
            {
                return Results.BadRequest(new ProblemDetails
                {
                    Title = "Identificador divergente",
                    Detail = "O Id da placa na rota difere do corpo da requisição.",
                    Status = StatusCodes.Status400BadRequest
                });
            }

            var result = await useCase.ExecutarAsync(empresaId, request, cancellationToken);

            if (result.IsFailure)
            {
                var status = result.Error.Code.Contains("naoAutorizado", StringComparison.OrdinalIgnoreCase)
                    ? StatusCodes.Status403Forbidden
                    : StatusCodes.Status422UnprocessableEntity;

                return Results.Json(new ProblemDetails
                {
                    Title = "Erro ao configurar filtro de satisfação",
                    Detail = result.Error.Description,
                    Status = status,
                    Extensions = { ["errorCode"] = result.Error.Code }
                }, statusCode: status);
            }

            return Results.NoContent();
        })
        .WithName("ConfigurarFiltroSatisfacaoCliente")
        .WithSummary("Ativa ou desativa o filtro de satisfação da placa")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        group.MapGet("/empresas/{empresaId:guid}/analytics", async (
            Guid empresaId,
            [FromQuery] Guid? placaId,
            [FromQuery] int? dias,
            IObterAnalyticsClienteUseCase useCase,
            CancellationToken cancellationToken) =>
        {
            var result = await useCase.ExecutarAsync(empresaId, placaId, dias ?? 30, cancellationToken);

            if (result.IsFailure)
            {
                return Results.UnprocessableEntity(new ProblemDetails
                {
                    Title = "Erro ao consultar analytics",
                    Detail = result.Error.Description,
                    Status = StatusCodes.Status422UnprocessableEntity,
                    Extensions = { ["errorCode"] = result.Error.Code }
                });
            }

            return Results.Ok(result.Value);
        })
        .WithName("ObterAnalyticsCliente")
        .WithSummary("Retorna métricas agregadas de toques, série temporal e dispositivos")
        .Produces<AnalyticsClienteDto>(StatusCodes.Status200OK);

        return app;
    }
}
