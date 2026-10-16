using System.IO.Compression;
using System.Text;
using EstrelaClick.Backend.Application.Common.Errors;
using EstrelaClick.Backend.Application.Ports.Outbound;
using EstrelaClick.Shared.SharedKernel.Results;

namespace EstrelaClick.Backend.Infrastructure.QrCode;

public sealed class ZipLoteArchiveService : IZipArchiveService
{
    public Result<byte[]> CriarPacoteLote(
        string numeroLote,
        IReadOnlyList<(string Slug, string UrlRedirecionamento, byte[] PngConteudo, byte[] SvgConteudo)> itens)
    {
        if (itens is null || itens.Count == 0)
        {
            return Result<byte[]>.Failure(QrCodeErrors.LoteVazio);
        }

        try
        {
            using var memoryStream = new MemoryStream();
            using (var zipArchive = new ZipArchive(memoryStream, ZipArchiveMode.Create, leaveOpen: true))
            {
                var manifestBuilder = new StringBuilder();
                manifestBuilder.AppendLine("Slug;UrlRedirecionamento;NumeroLote");

                foreach (var item in itens)
                {
                    // 1. Arquivo PNG em png/
                    var pngEntry = zipArchive.CreateEntry($"png/{item.Slug}.png", CompressionLevel.Optimal);
                    using (var pngStream = pngEntry.Open())
                    {
                        pngStream.Write(item.PngConteudo, 0, item.PngConteudo.Length);
                    }

                    // 2. Arquivo SVG em svg/
                    var svgEntry = zipArchive.CreateEntry($"svg/{item.Slug}.svg", CompressionLevel.Optimal);
                    using (var svgStream = svgEntry.Open())
                    {
                        svgStream.Write(item.SvgConteudo, 0, item.SvgConteudo.Length);
                    }

                    // 3. Linha do manifesto
                    manifestBuilder.AppendLine($"{item.Slug};{item.UrlRedirecionamento};{numeroLote}");
                }

                // 4. Arquivo manifest.csv
                var manifestEntry = zipArchive.CreateEntry("manifest.csv", CompressionLevel.Optimal);
                using (var manifestStream = manifestEntry.Open())
                using (var writer = new StreamWriter(manifestStream, new UTF8Encoding(false)))
                {
                    writer.Write(manifestBuilder.ToString());
                }
            }

            return Result<byte[]>.Success(memoryStream.ToArray());
        }
        catch
        {
            return Result<byte[]>.Failure(QrCodeErrors.FalhaExportacaoZip);
        }
    }
}
