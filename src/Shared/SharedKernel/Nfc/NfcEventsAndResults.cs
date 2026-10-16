namespace EstrelaClick.Shared.SharedKernel.Nfc;

public sealed class NfcTagDetectedEventArgs : EventArgs
{
    public NfcTagDetectedEventArgs(string uid, int maxTransceiveLength = 0, bool isReadOnly = false)
    {
        Uid = uid;
        MaxTransceiveLength = maxTransceiveLength;
        IsReadOnly = isReadOnly;
    }

    public string Uid { get; }
    public int MaxTransceiveLength { get; }
    public bool IsReadOnly { get; }
}

public readonly record struct NfcWriteResult(string Uid, int BytesWritten, bool IsLocked);
