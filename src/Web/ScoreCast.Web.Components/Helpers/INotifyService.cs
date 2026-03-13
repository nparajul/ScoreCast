namespace ScoreCast.Web.Components.Helpers;

public interface INotifyService
{
    void Notify();
    void Register(IAlertablePage page);
}
