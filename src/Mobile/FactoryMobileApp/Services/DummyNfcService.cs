using EstrelaClick.Shared.SharedKernel.Nfc;
using EstrelaClick.Shared.SharedKernel.Results;

namespace EstrelaClick.Mobile.FactoryMobileApp.Services;

public sealed class DummyNfcService : INfcService
{
    public bool IsNfcSupported => false;
    public bool IsNfcEnabled => false;

#pragma warning disable CS0067
    public event EventHandler<NfcTagDetectedEventArgs>? TagDetected;
#pragma warning restore CS0067

    public Result StartListening()
    {
        return Result.Failure(NfcErrors.NotSupported);
    }

    public Result StopListening()
    {
        return Result.Success();
    }

    public Task<Result<NfcWriteResult>> WriteUriRecordAsync(
        string uriString,
        bool lockTag = false,
        CancellationToken ct = default)
    {
        return Task.FromResult(Result<NfcWriteResult>.Failure(NfcErrors.NotSupported));
    }

    public Task<Result<NfcWriteResult>> WriteUriRecordAsync(
        string uriString,
        bool lockTag,
        byte[]? writePassword,
        byte[]? pack,
        CancellationToken ct = default)
    {
        return Task.FromResult(Result<NfcWriteResult>.Failure(NfcErrors.NotSupported));
    }
}
