using System.Diagnostics;
using EstrelaClick.Backend.Application.Common.Errors;
using EstrelaClick.Backend.Application.Ports.Inbound;
using EstrelaClick.Backend.Infrastructure.Security;
using Microsoft.AspNetCore.Mvc;

namespace EstrelaClick.Backend.Api.Endpoints;

public static class RedirecionamentoEndpoints
{
    public static IEndpointRouteBuilder MapRedirecionamentoEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/r/{slug}", async (
            string slug,
            HttpContext httpContext,
            IObterRedirecionamentoUseCase useCase,
            IIpHasher ipHasher) =>
        {
            var stopwatch = Stopwatch.StartNew();

            var userAgent = httpContext.Request.Headers.UserAgent.ToString();
            var referer = httpContext.Request.Headers.Referer.ToString();

            var clientIp = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault()
                ?? httpContext.Connection.RemoteIpAddress?.ToString();

            var ipHash = ipHasher.HashIp(clientIp);

            var result = await useCase.ExecutarAsync(
                slug: slug,
                userAgent: string.IsNullOrWhiteSpace(userAgent) ? null : userAgent,
                ipHash: ipHash,
                referer: string.IsNullOrWhiteSpace(referer) ? null : referer,
                cancellationToken: httpContext.RequestAborted);

            stopwatch.Stop();
            httpContext.Response.Headers.Append("Server-Timing", $"redirect;dur={stopwatch.ElapsedMilliseconds}");

            var clientBaseUrl = httpContext.RequestServices.GetRequiredService<IConfiguration>()["ClientDashboard:BaseUrl"] ?? "https://localhost:7198";

            if (result.IsSuccess && result.Value is not null)
            {
                if (result.Value.FiltroSatisfacaoAtivo)
                {
                    return Results.Redirect($"{clientBaseUrl}/feedback/{slug}", permanent: false);
                }

                return Results.Redirect(result.Value.UrlDestino, permanent: false);
            }

            if (result.Error == PlacaErrors.NaoAtivada)
            {
                return Results.Redirect($"{clientBaseUrl}/ativar/{slug}", permanent: false);
            }

            return Results.NotFound(new ProblemDetails
            {
                Title = "Placa não encontrada",
                Detail = result.Error.Description,
                Status = StatusCodes.Status404NotFound,
                Extensions = { ["errorCode"] = result.Error.Code }
            });
        })
        .WithName("RedirecionarPlaca")
        .WithSummary("Redirecionamento ultrarrápido de placa NFC / QR Code")
        .WithDescription("Resolve o slug com cache em memória (< 25ms), enfileira métricas assíncronas e redireciona (HTTP 302).")
        .Produces(StatusCodes.Status302Found)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
        .WithTags("Redirecionamento");

        return app;
    }
}
