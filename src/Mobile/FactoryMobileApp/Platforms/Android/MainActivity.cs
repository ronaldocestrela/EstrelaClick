using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Nfc;
using Android.Nfc.Tech;
using Android.OS;
using EstrelaClick.Mobile.FactoryMobileApp.Platforms.Android.Services;
using EstrelaClick.Shared.SharedKernel.Nfc;

namespace EstrelaClick.Mobile.FactoryMobileApp;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    private NfcAdapter? _nfcAdapter;
    private PendingIntent? _pendingIntent;
    private IntentFilter[]? _intentFilters;
    private string[][]? _techLists;

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        _nfcAdapter = NfcAdapter.GetDefaultAdapter(this);

        var intent = new Intent(this, typeof(MainActivity)).AddFlags(ActivityFlags.SingleTop);
        var flags = PendingIntentFlags.UpdateCurrent;
        if (Build.VERSION.SdkInt >= BuildVersionCodes.S)
        {
#pragma warning disable CA1416
            flags |= PendingIntentFlags.Mutable;
#pragma warning restore CA1416
        }

        _pendingIntent = PendingIntent.GetActivity(this, 0, intent, flags);

        var ndefFilter = new IntentFilter(NfcAdapter.ActionNdefDiscovered);
        ndefFilter.AddCategory(Intent.CategoryDefault);
        try
        {
            ndefFilter.AddDataType("*/*");
        }
        catch (IntentFilter.MalformedMimeTypeException)
        {
        }

        var techFilter = new IntentFilter(NfcAdapter.ActionTechDiscovered);
        var tagFilter = new IntentFilter(NfcAdapter.ActionTagDiscovered);

        _intentFilters = [ndefFilter, techFilter, tagFilter];

        _techLists =
        [
            [typeof(Ndef).FullName!],
            [typeof(NdefFormatable).FullName!],
            [typeof(MifareUltralight).FullName!],
            [typeof(NfcA).FullName!]
        ];
    }

    protected override void OnResume()
    {
        base.OnResume();

        if (_nfcAdapter != null && _nfcAdapter.IsEnabled && _pendingIntent != null)
        {
            _nfcAdapter.EnableForegroundDispatch(this, _pendingIntent, _intentFilters, _techLists);
        }
    }

    protected override void OnPause()
    {
        base.OnPause();
        _nfcAdapter?.DisableForegroundDispatch(this);
    }

    protected override void OnNewIntent(Intent? intent)
    {
        base.OnNewIntent(intent);

        if (intent == null)
        {
            return;
        }

        if (intent.Action == NfcAdapter.ActionTagDiscovered ||
            intent.Action == NfcAdapter.ActionTechDiscovered ||
            intent.Action == NfcAdapter.ActionNdefDiscovered)
        {
            Tag? tag;
            if (Build.VERSION.SdkInt >= BuildVersionCodes.Tiramisu)
            {
#pragma warning disable CA1416
                tag = intent.GetParcelableExtra(NfcAdapter.ExtraTag, Java.Lang.Class.FromType(typeof(Tag))) as Tag;
#pragma warning restore CA1416
            }
            else
            {
#pragma warning disable CS0618, CA1422
                tag = intent.GetParcelableExtra(NfcAdapter.ExtraTag) as Tag;
#pragma warning restore CS0618, CA1422
            }

            if (tag != null)
            {
                var nfcService = IPlatformApplication.Current?.Services.GetService<INfcService>() as AndroidNfcService;
                nfcService?.HandleTagDiscovered(tag);
            }
        }
    }
}
