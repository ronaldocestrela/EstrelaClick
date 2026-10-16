namespace EstrelaClick.Shared.SharedKernel.Mobile;

public interface ISensoryFeedbackService
{
    Task PlaySuccessFeedbackAsync();
    Task PlayFailureFeedbackAsync();
}
