using EstrelaClick.Backend.Application.Common.Errors;
using EstrelaClick.Backend.Application.Ports.Inbound;
using EstrelaClick.Shared.SharedKernel.Admin;
using Microsoft.AspNetCore.Mvc;

namespace EstrelaClick.Backend.Api.Endpoints;

public static class AdminEndpoints
{
    public static IEndpointRouteBuilder MapAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin")
            .WithTags("Administração Fabril")
            .RequireAuthorization("FactoryAuthPolicy");

        group.MapGet("/metricas-fabrica", async (
            IObterMetricasFabricaUseCase useCase,
            CancellationToken cancellationToken) =>
        {
            var result = await useCase.ExecutarAsync(cancellationToken);
            return Results.Ok(result.Value);
        })
        .WithName("ObterMetricasFabrica")
        .WithSummary("Retorna os KPIs consolidados da fábrica")
        .WithDescription("Totaliza placas criadas, em gravação, gravadas em estoque, vinculadas a clientes e total de lotes.")
        .Produces<MetricasFabricaDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapGet("/lotes", async (
            IListarLotesAdminUseCase useCase,
            CancellationToken cancellationToken) =>
        {
            var result = await useCase.ExecutarAsync(cancellationToken);
            return Results.Ok(result.Value);
        })
        .WithName("ListarLotesAdmin")
        .WithSummary("Lista todos os lotes com progresso de gravação e vinculação")
        .Produces<IReadOnlyList<LoteItemDto>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapGet("/placas/auditoria", async (
            [FromQuery] string termo,
            IConsultarAuditoriaPlacaUseCase useCase,
            CancellationToken cancellationToken) =>
        {
            var result = await useCase.ExecutarAsync(termo, cancellationToken);
            if (result.IsSuccess && result.Value is not null)
            {
                return Results.Ok(result.Value);
            }

            if (result.Error == PlacaErrors.SlugInvalido)
            {
                return Results.BadRequest(new ProblemDetails
                {
                    Title = "Termo de busca inválido",
                    Detail = result.Error.Description,
                    Status = StatusCodes.Status400BadRequest,
                    Extensions = { ["errorCode"] = result.Error.Code }
                });
            }

            return Results.NotFound(new ProblemDetails
            {
                Title = "Placa não encontrada",
                Detail = result.Error.Description,
                Status = StatusCodes.Status404NotFound,
                Extensions = { ["errorCode"] = result.Error.Code }
            });
        })
        .WithName("ConsultarAuditoriaPlaca")
        .WithSummary("Audita o ciclo de vida de uma placa por Slug ou Chip UID NFC")
        .Produces<PlacaAuditoriaDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapGet("/empresas", async (
            IListarEmpresasAdminUseCase useCase,
            CancellationToken cancellationToken) =>
        {
            var result = await useCase.ExecutarAsync(cancellationToken);
            return Results.Ok(result.Value);
        })
        .WithName("ListarEmpresasAdmin")
        .WithSummary("Lista todos os clientes lojistas cadastrados")
        .Produces<IReadOnlyList<EmpresaDto>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapPost("/empresas", async (
            [FromBody] CriarEmpresaRequest request,
            ICadastrarEmpresaAdminUseCase useCase,
            CancellationToken cancellationToken) =>
        {
            var result = await useCase.ExecutarAsync(request, cancellationToken);
            if (result.IsSuccess && result.Value is not null)
            {
                return Results.Created($"/api/admin/empresas/{result.Value.Id}", result.Value);
            }

            return Results.BadRequest(new ProblemDetails
            {
                Title = "Dados de cliente inválidos",
                Detail = result.Error.Description,
                Status = StatusCodes.Status400BadRequest,
                Extensions = { ["errorCode"] = result.Error.Code }
            });
        })
        .WithName("CadastrarEmpresaAdmin")
        .WithSummary("Cadastra um novo cliente lojista")
        .Produces<EmpresaDto>(StatusCodes.Status201Created)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized);

        return app;
    }
}
