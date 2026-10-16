using Microsoft.Extensions.Logging;

namespace EstrelaClick.Mobile.FactoryMobileApp;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

#if DEBUG
		builder.Logging.AddDebug();
#endif

#if ANDROID
		builder.Services.AddSingleton<EstrelaClick.Shared.SharedKernel.Nfc.INfcService, Platforms.Android.Services.AndroidNfcService>();
		builder.Services.AddSingleton<EstrelaClick.Shared.SharedKernel.Mobile.ISensoryFeedbackService, Platforms.Android.Services.AndroidSensoryFeedbackService>();
#else
		builder.Services.AddSingleton<EstrelaClick.Shared.SharedKernel.Nfc.INfcService, Services.DummyNfcService>();
		builder.Services.AddSingleton<EstrelaClick.Shared.SharedKernel.Mobile.ISensoryFeedbackService, Services.DummySensoryFeedbackService>();
#endif

		// Sessão e Cliente de API de Fábrica
		builder.Services.AddSingleton<EstrelaClick.Shared.SharedKernel.Mobile.IOperatorSessionService, EstrelaClick.Shared.SharedKernel.Mobile.OperatorSessionService>();
		builder.Services.AddSingleton<EstrelaClick.Shared.SharedKernel.Mobile.IFactoryApiClient>(sp =>
		{
#if ANDROID
			var baseUri = new Uri("http://10.0.2.2:5000");
#else
			var baseUri = new Uri("http://localhost:5000");
#endif
			var httpClient = new HttpClient { BaseAddress = baseUri, Timeout = TimeSpan.FromSeconds(15) };
			return new EstrelaClick.Shared.SharedKernel.Mobile.FactoryApiClient(httpClient, sp.GetRequiredService<EstrelaClick.Shared.SharedKernel.Mobile.IOperatorSessionService>());
		});

		// ViewModels
		builder.Services.AddTransient<EstrelaClick.Shared.SharedKernel.Mobile.ViewModels.LoginViewModel>();
		builder.Services.AddTransient<EstrelaClick.Shared.SharedKernel.Mobile.ViewModels.BancadaViewModel>(sp =>
		{
			return new EstrelaClick.Shared.SharedKernel.Mobile.ViewModels.BancadaViewModel(
				sp.GetRequiredService<EstrelaClick.Shared.SharedKernel.Mobile.IFactoryApiClient>(),
				sp.GetRequiredService<EstrelaClick.Shared.SharedKernel.Mobile.IOperatorSessionService>(),
				sp.GetRequiredService<EstrelaClick.Shared.SharedKernel.Nfc.INfcService>(),
				sp.GetRequiredService<EstrelaClick.Shared.SharedKernel.Mobile.ISensoryFeedbackService>());
		});

		// Views
		builder.Services.AddTransient<Views.LoginPage>();
		builder.Services.AddTransient<Views.BancadaPage>();

		return builder.Build();
	}
}
