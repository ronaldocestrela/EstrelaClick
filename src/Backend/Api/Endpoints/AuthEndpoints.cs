using EstrelaClick.Backend.Application.Common.Errors;
using EstrelaClick.Backend.Application.DTOs;
using EstrelaClick.Backend.Application.Ports.Inbound;
using Microsoft.AspNetCore.Mvc;

namespace EstrelaClick.Backend.Api.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth")
            .WithTags("Autenticação de Operador e Fábrica");

        group.MapPost("/operador-login", async (
            [FromBody] AutenticarOperadorCommand command,
            IAutenticarOperadorUseCase useCase,
            CancellationToken cancellationToken) =>
        {
            var result = await useCase.ExecutarAsync(command, cancellationToken);

            if (result.IsSuccess && result.Value is not null)
            {
                return Results.Ok(result.Value);
            }

            if (result.Error == AuthErrors.CredenciaisInvalidas || result.Error == AuthErrors.OperadorInativo)
            {
                return Results.Json(
                    new ProblemDetails
                    {
                        Title = "Acesso negado",
                        Detail = result.Error.Description,
                        Status = StatusCodes.Status401Unauthorized,
                        Extensions = { ["errorCode"] = result.Error.Code }
                    },
                    statusCode: StatusCodes.Status401Unauthorized);
            }

            return Results.BadRequest(new ProblemDetails
            {
                Title = "Requisição inválida",
                Detail = result.Error.Description,
                Status = StatusCodes.Status400BadRequest,
                Extensions = { ["errorCode"] = result.Error.Code }
            });
        })
        .WithName("AutenticarOperador")
        .WithSummary("Autentica o operador de bancada e emite token JWT")
        .WithDescription("Valida credenciais do operador e emite JWT assinado para consumo dos endpoints de fábrica no app mobile.")
        .Produces<AuthTokenResult>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized);

        return app;
    }
}
