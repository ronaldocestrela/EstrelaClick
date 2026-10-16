namespace EstrelaClick.Backend.Application.DTOs;

public sealed record LoteExportadoZipDto(
    string NomeArquivo,
    byte[] Conteudo,
    int TotalPlacas);
