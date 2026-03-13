namespace ScoreCast.Web.Components.Helpers;

public class NotifyService : INotifyService
{
    private readonly List<IAlertablePage> _pages = [];

    public void Notify()
    {
        foreach (var page in _pages) page.AlertChanged();
    }

    public void Register(IAlertablePage page) => _pages.Add(page);
}
