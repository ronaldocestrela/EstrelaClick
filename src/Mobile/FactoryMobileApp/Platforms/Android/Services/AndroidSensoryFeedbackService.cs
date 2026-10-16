#if ANDROID
using Android.Content;
using Android.Media;
using Android.OS;
using EstrelaClick.Shared.SharedKernel.Mobile;
using Application = Android.App.Application;

namespace EstrelaClick.Mobile.FactoryMobileApp.Platforms.Android.Services;

#pragma warning disable CA1416, CA1422, CS0618

public sealed class AndroidSensoryFeedbackService : ISensoryFeedbackService, IDisposable
{
    private readonly ToneGenerator? _toneGenerator;
    private readonly Vibrator? _vibrator;

    public AndroidSensoryFeedbackService()
    {
        try
        {
            _toneGenerator = new ToneGenerator(global::Android.Media.Stream.Music, 100);
        }
        catch
        {
            _toneGenerator = null;
        }

        try
        {
            var context = Application.Context;
            if (Build.VERSION.SdkInt >= BuildVersionCodes.S)
            {
                var vibratorManager = context.GetSystemService(Context.VibratorManagerService) as VibratorManager;
                _vibrator = vibratorManager?.DefaultVibrator;
            }
            else
            {
#pragma warning disable CS0618
                _vibrator = context.GetSystemService(Context.VibratorService) as Vibrator;
#pragma warning restore CS0618
            }
        }
        catch
        {
            _vibrator = null;
        }
    }

    public Task PlaySuccessFeedbackAsync()
    {
        try
        {
            // Som curto e agudo de leitor de código de barras industrial
            _toneGenerator?.StartTone(Tone.PropBeep, 120);

            // Pulso tátil firme e curto (70ms)
            if (_vibrator != null && _vibrator.HasVibrator)
            {
                if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
                {
                    _vibrator.Vibrate(VibrationEffect.CreateOneShot(70, VibrationEffect.DefaultAmplitude));
                }
                else
                {
#pragma warning disable CS0618
                    _vibrator.Vibrate(70);
#pragma warning restore CS0618
                }
            }
        }
        catch
        {
            // Ignora falhas de áudio/vibração sem impactar o fluxo industrial
        }

        return Task.CompletedTask;
    }

    public Task PlayFailureFeedbackAsync()
    {
        try
        {
            // Alerta sonoro grave duplo
            _toneGenerator?.StartTone(Tone.PropNack, 250);

            // Vibração dupla de alerta (100ms vibra, 80ms pausa, 150ms vibra)
            if (_vibrator != null && _vibrator.HasVibrator)
            {
                if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
                {
                    long[] pattern = [0, 100, 80, 150];
                    _vibrator.Vibrate(VibrationEffect.CreateWaveform(pattern, -1));
                }
                else
                {
#pragma warning disable CS0618
                    _vibrator.Vibrate(250);
#pragma warning restore CS0618
                }
            }
        }
        catch
        {
            // Silencioso se der erro de dispositivo
        }

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        try
        {
            _toneGenerator?.Release();
        }
        catch
        {
        }
    }
}
#endif
