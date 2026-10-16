using System.Text;
using EstrelaClick.Shared.SharedKernel.Results;

namespace EstrelaClick.Shared.SharedKernel.Nfc;

public readonly record struct NdefUriPayload(byte PrefixCode, string SubPath);

public static class NdefUriHelper
{
    public const byte PrefixNone = 0x00;
    public const byte PrefixHttpWww = 0x01;
    public const byte PrefixHttpsWww = 0x02;
    public const byte PrefixHttp = 0x03;
    public const byte PrefixHttps = 0x04;

    /// <summary>
    /// Senha padrão da fábrica para proteção contra regravação indevida (4 bytes).
    /// </summary>
    public static readonly byte[] DefaultFactoryPassword = [0x45, 0x53, 0x54, 0x52]; // ASCII 'ESTR'

    /// <summary>
    /// Password Acknowledge (PACK) padrão da fábrica para retorno na autenticação NTAG (2 bytes).
    /// </summary>
    public static readonly byte[] DefaultFactoryPack = [0x43, 0x4C]; // ASCII 'CL'

    public static Result<NdefUriPayload> EncodeUri(string? uriString)
    {
        if (string.IsNullOrWhiteSpace(uriString))
        {
            return Result<NdefUriPayload>.Failure(NfcErrors.InvalidUri);
        }

        string trimmed = uriString.Trim();

        if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var parsedUri) ||
            (parsedUri.Scheme != Uri.UriSchemeHttp && parsedUri.Scheme != Uri.UriSchemeHttps))
        {
            return Result<NdefUriPayload>.Failure(NfcErrors.InvalidUri);
        }

        if (trimmed.StartsWith("https://www.", StringComparison.OrdinalIgnoreCase))
        {
            return Result<NdefUriPayload>.Success(new NdefUriPayload(PrefixHttpsWww, trimmed[12..]));
        }

        if (trimmed.StartsWith("http://www.", StringComparison.OrdinalIgnoreCase))
        {
            return Result<NdefUriPayload>.Success(new NdefUriPayload(PrefixHttpWww, trimmed[11..]));
        }

        if (trimmed.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return Result<NdefUriPayload>.Success(new NdefUriPayload(PrefixHttps, trimmed[8..]));
        }

        if (trimmed.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
        {
            return Result<NdefUriPayload>.Success(new NdefUriPayload(PrefixHttp, trimmed[7..]));
        }

        return Result<NdefUriPayload>.Failure(NfcErrors.InvalidUri);
    }

    public static Result<string> FormatUid(byte[]? uid)
    {
        if (uid == null || uid.Length == 0)
        {
            return Result<string>.Failure(NfcErrors.InvalidUid);
        }

        string formatted = string.Join(":", uid.Select(b => b.ToString("X2")));
        return Result<string>.Success(formatted);
    }

    public static Result<int> CalculateRequiredBytes(string uriString)
    {
        var encodeResult = EncodeUri(uriString);
        if (encodeResult.IsFailure)
        {
            return Result<int>.Failure(encodeResult.Error);
        }

        int subPathBytes = Encoding.UTF8.GetByteCount(encodeResult.Value.SubPath);
        int payloadBytes = 1 + subPathBytes; // 1 byte prefixo + subpath

        // NDEF Short Record Overhead: Header(1) + TypeLength(1) + PayloadLength(1) + Type(1 = 'U')
        int headerOverhead = payloadBytes <= 255 ? 4 : 7;
        int totalBytes = headerOverhead + payloadBytes;

        return Result<int>.Success(totalBytes);
    }
}
