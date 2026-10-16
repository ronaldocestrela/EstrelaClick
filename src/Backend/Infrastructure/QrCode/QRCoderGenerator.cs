using System.Text;
using EstrelaClick.Backend.Application.Common.Errors;
using EstrelaClick.Backend.Application.Ports.Outbound;
using EstrelaClick.Shared.SharedKernel.Results;
using QRCoder;

namespace EstrelaClick.Backend.Infrastructure.QrCode;

public sealed class QRCoderGenerator : IQrCodeGenerator
{
    private const int PixelsPerMeter300Dpi = 11811; // 300 dpi = 11811 pixels por metro

    public Result<byte[]> GerarPng300Dpi(string payload, int pixelsPorModulo = 20)
    {
        if (string.IsNullOrWhiteSpace(payload))
        {
            return Result<byte[]>.Failure(QrCodeErrors.ConteudoInvalido);
        }

        try
        {
            using var generator = new QRCodeGenerator();
            using var data = generator.CreateQrCode(payload, QRCodeGenerator.ECCLevel.Q);
            var qrCode = new PngByteQRCode(data);
            var rawPng = qrCode.GetGraphic(pixelsPorModulo);

            var pngComPhys = InjetarChunkPhys300Dpi(rawPng);
            return Result<byte[]>.Success(pngComPhys);
        }
        catch
        {
            return Result<byte[]>.Failure(QrCodeErrors.FalhaGeracao);
        }
    }

    public Result<byte[]> GerarSvg(string payload)
    {
        if (string.IsNullOrWhiteSpace(payload))
        {
            return Result<byte[]>.Failure(QrCodeErrors.ConteudoInvalido);
        }

        try
        {
            using var generator = new QRCodeGenerator();
            using var data = generator.CreateQrCode(payload, QRCodeGenerator.ECCLevel.Q);
            var svgQr = new SvgQRCode(data);
            var svgText = svgQr.GetGraphic(pixelsPerModule: 10);
            var svgBytes = Encoding.UTF8.GetBytes(svgText);

            return Result<byte[]>.Success(svgBytes);
        }
        catch
        {
            return Result<byte[]>.Failure(QrCodeErrors.FalhaGeracao);
        }
    }

    private static byte[] InjetarChunkPhys300Dpi(byte[] pngBytes)
    {
        // Se já tiver pHYs ou for muito curto para um PNG válido, retorna o original
        if (pngBytes.Length < 33 || TemChunkPhys(pngBytes))
        {
            return pngBytes;
        }

        // O cabeçalho PNG tem 8 bytes.
        // O primeiro chunk é obrigatoriamente IHDR: 4 bytes length + 4 bytes "IHDR" + 13 bytes data + 4 bytes CRC = 25 bytes.
        // Inserimos o chunk pHYs imediatamente após o IHDR (offset 8 + 25 = 33).
        const int insertOffset = 33;

        var physChunk = CriarChunkPhys(PixelsPerMeter300Dpi, PixelsPerMeter300Dpi);

        var result = new byte[pngBytes.Length + physChunk.Length];
        Buffer.BlockCopy(pngBytes, 0, result, 0, insertOffset);
        Buffer.BlockCopy(physChunk, 0, result, insertOffset, physChunk.Length);
        Buffer.BlockCopy(pngBytes, insertOffset, result, insertOffset + physChunk.Length, pngBytes.Length - insertOffset);

        return result;
    }

    private static bool TemChunkPhys(byte[] pngBytes)
    {
        var physHeader = "pHYs"u8;
        for (int i = 0; i <= pngBytes.Length - physHeader.Length; i++)
        {
            if (pngBytes[i] == physHeader[0] &&
                pngBytes[i + 1] == physHeader[1] &&
                pngBytes[i + 2] == physHeader[2] &&
                pngBytes[i + 3] == physHeader[3])
            {
                return true;
            }
        }

        return false;
    }

    private static byte[] CriarChunkPhys(int ppmX, int ppmY)
    {
        // Chunk structure:
        // Length: 4 bytes (valor 9)
        // Type: 4 bytes ("pHYs")
        // Data: 9 bytes (4 ppmX + 4 ppmY + 1 unit)
        // CRC: 4 bytes
        var chunk = new byte[4 + 4 + 9 + 4];

        // Length = 9 (big-endian)
        chunk[0] = 0;
        chunk[1] = 0;
        chunk[2] = 0;
        chunk[3] = 9;

        // Type = "pHYs"
        chunk[4] = (byte)'p';
        chunk[5] = (byte)'H';
        chunk[6] = (byte)'Y';
        chunk[7] = (byte)'s';

        // Data: ppmX (big-endian)
        chunk[8] = (byte)((ppmX >> 24) & 0xFF);
        chunk[9] = (byte)((ppmX >> 16) & 0xFF);
        chunk[10] = (byte)((ppmX >> 8) & 0xFF);
        chunk[11] = (byte)(ppmX & 0xFF);

        // Data: ppmY (big-endian)
        chunk[12] = (byte)((ppmY >> 24) & 0xFF);
        chunk[13] = (byte)((ppmY >> 16) & 0xFF);
        chunk[14] = (byte)((ppmY >> 8) & 0xFF);
        chunk[15] = (byte)(ppmY & 0xFF);

        // Unit specifier: 1 = metro
        chunk[16] = 1;

        // Calcular CRC32 sobre Type + Data (13 bytes: indices 4 a 16)
        var crc = CalcularCrc32(chunk, 4, 13);
        chunk[17] = (byte)((crc >> 24) & 0xFF);
        chunk[18] = (byte)((crc >> 16) & 0xFF);
        chunk[19] = (byte)((crc >> 8) & 0xFF);
        chunk[20] = (byte)(crc & 0xFF);

        return chunk;
    }

    private static uint CalcularCrc32(byte[] buffer, int offset, int length)
    {
        uint crc = 0xFFFFFFFF;
        for (int i = offset; i < offset + length; i++)
        {
            var b = buffer[i];
            crc ^= b;
            for (int k = 0; k < 8; k++)
            {
                crc = (crc & 1) != 0 ? (crc >> 1) ^ 0xEDB88320 : crc >> 1;
            }
        }

        return ~crc;
    }
}
