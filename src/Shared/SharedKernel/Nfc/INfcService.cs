using EstrelaClick.Shared.SharedKernel.Results;

namespace EstrelaClick.Shared.SharedKernel.Nfc;

public interface INfcService
{
    bool IsNfcSupported { get; }
    bool IsNfcEnabled { get; }

    event EventHandler<NfcTagDetectedEventArgs>? TagDetected;

    Result StartListening();
    Result StopListening();

    Task<Result<NfcWriteResult>> WriteUriRecordAsync(
        string uriString,
        bool lockTag = false,
        CancellationToken ct = default);

    Task<Result<NfcWriteResult>> WriteUriRecordAsync(
        string uriString,
        bool lockTag,
        byte[]? writePassword,
        byte[]? pack,
        CancellationToken ct = default);
}
