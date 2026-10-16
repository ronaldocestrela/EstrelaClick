using EstrelaClick.Backend.Application.Common.Errors;
using EstrelaClick.Backend.Application.Ports.Inbound;
using Microsoft.AspNetCore.Mvc;

namespace EstrelaClick.Backend.Api.Endpoints;

public static class QrCodeEndpoints
{
    public static IEndpointRouteBuilder MapQrCodeEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/placas")
            .WithTags("QR Code e Exportação");

        group.MapGet("/{slug}/qrcode.png", async (
            string slug,
            IGerarQrCodePlacaUseCase useCase,
            CancellationToken cancellationToken) =>
        {
            var result = await useCase.GerarPngPorSlugAsync(slug, cancellationToken);

            if (result.IsSuccess && result.Value is not null)
            {
                return Results.File(
                    fileContents: result.Value.Conteudo,
                    contentType: result.Value.ContentType,
                    fileDownloadName: result.Value.NomeArquivo);
            }

            if (result.Error == PlacaErrors.SlugInvalido)
            {
                return Results.BadRequest(new ProblemDetails
                {
                    Title = "Slug inválido",
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
        .WithName("GerarQrCodePng")
        .WithSummary("Gera o QR Code da placa em alta definição PNG (300 DPI)")
        .WithDescription("Renderiza imagem PNG de 300 DPI contendo o chunk físico pHYs para birôs gráficos e envio para impressão.")
        .Produces(StatusCodes.Status200OK, contentType: "image/png")
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapGet("/{slug}/qrcode.svg", async (
            string slug,
            IGerarQrCodePlacaUseCase useCase,
            CancellationToken cancellationToken) =>
        {
            var result = await useCase.GerarSvgPorSlugAsync(slug, cancellationToken);

            if (result.IsSuccess && result.Value is not null)
            {
                return Results.File(
                    fileContents: result.Value.Conteudo,
                    contentType: result.Value.ContentType,
                    fileDownloadName: result.Value.NomeArquivo);
            }

            if (result.Error == PlacaErrors.SlugInvalido)
            {
                return Results.BadRequest(new ProblemDetails
                {
                    Title = "Slug inválido",
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
        .WithName("GerarQrCodeSvg")
        .WithSummary("Gera o QR Code da placa em formato vetorial SVG")
        .WithDescription("Renderiza vetor SVG puro para gravação e corte a laser de placas de acrílico ou madeira.")
        .Produces(StatusCodes.Status200OK, contentType: "image/svg+xml")
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapGet("/lotes/{numeroLote}/exportar-zip", async (
            string numeroLote,
            IExportarLoteQrCodeZipUseCase useCase,
            CancellationToken cancellationToken) =>
        {
            var result = await useCase.ExecutarAsync(numeroLote, cancellationToken);

            if (result.IsSuccess && result.Value is not null)
            {
                return Results.File(
                    fileContents: result.Value.Conteudo,
                    contentType: "application/zip",
                    fileDownloadName: result.Value.NomeArquivo);
            }

            if (result.Error == QrCodeErrors.LoteObrigatorio)
            {
                return Results.BadRequest(new ProblemDetails
                {
                    Title = "Número de lote obrigatório",
                    Detail = result.Error.Description,
                    Status = StatusCodes.Status400BadRequest,
                    Extensions = { ["errorCode"] = result.Error.Code }
                });
            }

            if (result.Error == QrCodeErrors.LoteNaoEncontrado || result.Error == QrCodeErrors.LoteVazio)
            {
                return Results.NotFound(new ProblemDetails
                {
                    Title = "Lote não encontrado ou vazio",
                    Detail = result.Error.Description,
                    Status = StatusCodes.Status404NotFound,
                    Extensions = { ["errorCode"] = result.Error.Code }
                });
            }

            return Results.Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Falha na exportação do lote",
                detail: result.Error.Description,
                extensions: new Dictionary<string, object?> { ["errorCode"] = result.Error.Code });
        })
        .WithName("ExportarLoteQrCodesZip")
        .WithSummary("Exporta pacote compactado (.zip) com PNGs (300 DPI), SVGs e manifest.csv por lote")
        .WithDescription("Gera um pacote .zip com diretórios segregados 'png/' e 'svg/' e manifesto de conferência para birôs e fábricas.")
        .Produces(StatusCodes.Status200OK, contentType: "application/zip")
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status500InternalServerError);

        return app;
    }
}
