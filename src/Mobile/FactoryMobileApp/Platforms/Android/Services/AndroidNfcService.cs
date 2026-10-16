#if ANDROID
using Android.App;
using Android.Content;
using Android.Nfc;
using Android.Nfc.Tech;
using Android.OS;
using EstrelaClick.Shared.SharedKernel.Nfc;
using EstrelaClick.Shared.SharedKernel.Results;
using Application = Android.App.Application;
using NfcFormatException = Android.Nfc.FormatException;
using Result = EstrelaClick.Shared.SharedKernel.Results.Result;

namespace EstrelaClick.Mobile.FactoryMobileApp.Platforms.Android.Services;

public sealed class AndroidNfcService : INfcService
{
    private Tag? _currentTag;
    private bool _isListening;

    public bool IsNfcSupported
    {
        get
        {
            var adapter = NfcAdapter.GetDefaultAdapter(Application.Context);
            return adapter != null;
        }
    }

    public bool IsNfcEnabled
    {
        get
        {
            var adapter = NfcAdapter.GetDefaultAdapter(Application.Context);
            return adapter != null && adapter.IsEnabled;
        }
    }

    public event EventHandler<NfcTagDetectedEventArgs>? TagDetected;

    public Result StartListening()
    {
        if (!IsNfcSupported)
        {
            return Result.Failure(NfcErrors.NotSupported);
        }

        if (!IsNfcEnabled)
        {
            return Result.Failure(NfcErrors.Disabled);
        }

        _isListening = true;
        return Result.Success();
    }

    public Result StopListening()
    {
        _isListening = false;
        _currentTag = null;
        return Result.Success();
    }

    public void HandleTagDiscovered(Tag tag)
    {
        if (!_isListening)
        {
            return;
        }

        _currentTag = tag;
        byte[]? idBytes = tag.GetId();
        var formatResult = NdefUriHelper.FormatUid(idBytes);
        string uid = formatResult.IsSuccess && formatResult.Value != null ? formatResult.Value : string.Empty;

        int maxTransceiveLength = 0;
        bool isReadOnly = false;

        var ndef = Ndef.Get(tag);
        if (ndef != null)
        {
            maxTransceiveLength = ndef.MaxSize;
            isReadOnly = !ndef.IsWritable;
        }

        TagDetected?.Invoke(this, new NfcTagDetectedEventArgs(uid, maxTransceiveLength, isReadOnly));
    }

    public Task<Result<NfcWriteResult>> WriteUriRecordAsync(
        string uriString,
        bool lockTag = false,
        CancellationToken ct = default)
    {
        return WriteUriRecordAsync(uriString, lockTag, writePassword: null, pack: null, ct);
    }

    public async Task<Result<NfcWriteResult>> WriteUriRecordAsync(
        string uriString,
        bool lockTag,
        byte[]? writePassword,
        byte[]? pack,
        CancellationToken ct = default)
    {
        if (!IsNfcSupported)
        {
            return Result<NfcWriteResult>.Failure(NfcErrors.NotSupported);
        }

        if (!IsNfcEnabled)
        {
            return Result<NfcWriteResult>.Failure(NfcErrors.Disabled);
        }

        var activeTag = _currentTag;
        if (activeTag == null)
        {
            return Result<NfcWriteResult>.Failure(NfcErrors.NoTagDetected);
        }

        var encodeResult = NdefUriHelper.EncodeUri(uriString);
        if (encodeResult.IsFailure)
        {
            return Result<NfcWriteResult>.Failure(encodeResult.Error);
        }

        var uidResult = NdefUriHelper.FormatUid(activeTag.GetId());
        string formattedUid = uidResult.IsSuccess && uidResult.Value != null ? uidResult.Value : "UNKNOWN";

        return await Task.Run(() =>
        {
            try
            {
                var record = NdefRecord.CreateUri(global::Android.Net.Uri.Parse(uriString));
                if (record == null)
                {
                    return Result<NfcWriteResult>.Failure(NfcErrors.InvalidUri);
                }

                var ndefMessage = new NdefMessage([record]);
                byte[] messageBytes = ndefMessage.ToByteArray() ?? [];

                var ndef = Ndef.Get(activeTag);
                if (ndef != null)
                {
                    ndef.Connect();

                    if (!ndef.IsWritable)
                    {
                        ndef.Close();
                        return Result<NfcWriteResult>.Failure(NfcErrors.TagReadOnly);
                    }

                    if (ndef.MaxSize < messageBytes.Length)
                    {
                        ndef.Close();
                        return Result<NfcWriteResult>.Failure(NfcErrors.CapacityExceeded);
                    }

                    ndef.WriteNdefMessage(ndefMessage);

                    bool isLocked = false;
                    if (lockTag && ndef.CanMakeReadOnly())
                    {
                        ndef.MakeReadOnly();
                        isLocked = true;
                    }

                    ndef.Close();

                    // Se especificada senha de regravação, aplica proteção por senha no chip NTAG
                    if (writePassword != null && writePassword.Length == 4)
                    {
                        TryApplyNtagPasswordProtection(activeTag, writePassword, pack);
                    }

                    return Result<NfcWriteResult>.Success(new NfcWriteResult(formattedUid, messageBytes.Length, isLocked));
                }

                var formatable = NdefFormatable.Get(activeTag);
                if (formatable != null)
                {
                    formatable.Connect();
                    if (lockTag)
                    {
                        formatable.FormatReadOnly(ndefMessage);
                    }
                    else
                    {
                        formatable.Format(ndefMessage);
                    }
                    formatable.Close();

                    if (writePassword != null && writePassword.Length == 4)
                    {
                        TryApplyNtagPasswordProtection(activeTag, writePassword, pack);
                    }

                    return Result<NfcWriteResult>.Success(new NfcWriteResult(formattedUid, messageBytes.Length, lockTag));
                }

                return Result<NfcWriteResult>.Failure(NfcErrors.NotSupported);
            }
            catch (TagLostException)
            {
                return Result<NfcWriteResult>.Failure(NfcErrors.TagLost);
            }
            catch (NfcFormatException)
            {
                return Result<NfcWriteResult>.Failure(NfcErrors.WriteFailed);
            }
            catch (Java.IO.IOException)
            {
                return Result<NfcWriteResult>.Failure(NfcErrors.WriteFailed);
            }
            catch (Exception)
            {
                return Result<NfcWriteResult>.Failure(NfcErrors.WriteFailed);
            }
        }, ct);
    }

    private static void TryApplyNtagPasswordProtection(Tag tag, byte[] password, byte[]? pack)
    {
        try
        {
            var ultralight = MifareUltralight.Get(tag);
            if (ultralight != null)
            {
                ultralight.Connect();
                try
                {
                    // NTAG213: PWD na página 43 (0x2B), PACK na página 44 (0x2C), ACCESS/AUTH0 na página 42 (0x2A)
                    byte[] pwdData = [password[0], password[1], password[2], password[3]];
                    byte[] packData = pack != null && pack.Length >= 2
                        ? [pack[0], pack[1], 0x00, 0x00]
                        : [0x00, 0x00, 0x00, 0x00];

                    ultralight.WritePage(43, pwdData);
                    ultralight.WritePage(44, packData);

                    // Byte 0 = AUTH0 (a partir de qual página exige auth - 0x04 para área de dados NDEF)
                    // Byte 1 = PROT (0x80 para proteger apenas WRITE, permitindo leitura livre)
                    byte[] accessConfig = [0x04, 0x80, 0x00, 0x00];
                    ultralight.WritePage(42, accessConfig);
                }
                catch
                {
                    // Fallback para modelos NTAG com endereçamento diferente de páginas
                }
                finally
                {
                    ultralight.Close();
                }
            }
        }
        catch
        {
            // Ignora se o hardware não suportar comandos ultralight
        }
    }
}
#endif
