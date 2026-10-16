using EstrelaClick.Shared.SharedKernel.Mobile;

namespace EstrelaClick.Mobile.FactoryMobileApp.Services;

public sealed class DummySensoryFeedbackService : ISensoryFeedbackService
{
    public Task PlaySuccessFeedbackAsync()
    {
        try
        {
            if (OperatingSystem.IsWindows())
            {
                Console.Beep(2000, 150);
            }
        }
        catch
        {
            // Fallback silencioso em plataformas sem áudio de console
        }

        return Task.CompletedTask;
    }

    public Task PlayFailureFeedbackAsync()
    {
        try
        {
            if (OperatingSystem.IsWindows())
            {
                Console.Beep(800, 250);
            }
        }
        catch
        {
            // Fallback silencioso
        }

        return Task.CompletedTask;
    }
}
