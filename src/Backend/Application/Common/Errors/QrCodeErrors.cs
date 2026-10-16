using EstrelaClick.Shared.SharedKernel.Results;

namespace EstrelaClick.Backend.Application.Common.Errors;

public static class QrCodeErrors
{
    public static readonly Error LoteObrigatorio = new("qrcode.lote.obrigatorio", "Número de lote é obrigatório.");
    public static readonly Error LoteNaoEncontrado = new("qrcode.lote.nao_encontrado", "Nenhuma placa encontrada para o número de lote informado.");
    public static readonly Error LoteVazio = new("qrcode.lote.vazio", "O lote especificado não possui placas geradas.");
    public static readonly Error ConteudoInvalido = new("qrcode.conteudo.invalido", "Conteúdo para geração do QR Code é inválido ou vazio.");
    public static readonly Error FalhaGeracao = new("qrcode.geracao.falha", "Ocorreu uma falha ao gerar o arquivo de QR Code.");
    public static readonly Error FalhaExportacaoZip = new("qrcode.exportacao_zip.falha", "Ocorreu uma falha ao empacotar o arquivo zip de exportação do lote.");
}
