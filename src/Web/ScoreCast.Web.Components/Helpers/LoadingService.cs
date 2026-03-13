namespace ScoreCast.Web.Components.Helpers;

public class LoadingService(INotifyService Notify) : ILoadingService
{
    private bool _isLoading;
    private string? _loadingMessage;

    public bool IsLoading
    {
        get => _isLoading;
        set
        {
            _isLoading = value;
            Notify.Notify();
        }
    }

    public string? LoadingMessage
    {
        get => _loadingMessage;
        set
        {
            _loadingMessage = value;
            Notify.Notify();
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
