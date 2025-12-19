namespace ScoreCast.Web.Components.Helpers;

public class LoadingService(INotifyService notify) : ILoadingService
{
    private bool _isLoading;
    private string? _loadingMessage;

    public bool IsLoading
    {
        get => _isLoading;
        set
        {
            _isLoading = value;
            notify.Notify();
        }
    }

    public string? LoadingMessage
    {
        get => _loadingMessage;
        set
        {
            _loadingMessage = value;
            notify.Notify();
        }
    }

    public async Task While(Func<Task> action, string? message = null)
    {
        IsLoading = true;
        LoadingMessage = message;
        try
        {
            await action();
        }
        finally
        {
            IsLoading = false;
            LoadingMessage = null;
        }
    }
}
