using EstrelaClick.Shared.SharedKernel.Results;

namespace EstrelaClick.Backend.Application.Ports.Outbound;

public interface IZipArchiveService
{
    Result<byte[]> CriarPacoteLote(
        string numeroLote,
        IReadOnlyList<(string Slug, string UrlRedirecionamento, byte[] PngConteudo, byte[] SvgConteudo)> itens);
}
