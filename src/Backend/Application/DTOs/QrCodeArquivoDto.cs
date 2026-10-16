namespace EstrelaClick.Backend.Application.DTOs;

public sealed record QrCodeArquivoDto(
    string NomeArquivo,
    byte[] Conteudo,
    string ContentType);
