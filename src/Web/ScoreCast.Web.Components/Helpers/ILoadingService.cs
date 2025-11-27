namespace ScoreCast.Web.Components.Helpers;

public interface ILoadingService
{
    bool IsLoading { get; set; }
    string? LoadingMessage { get; set; }
    Task While(Func<Task> action, string? message = null);
}
