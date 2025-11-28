using OneOf;
using Severity = MudBlazor.Severity;

namespace ScoreCast.Web.Components.Helpers;

public interface IAlertService
{
    public IEnumerable<ScoreCastAlert> Alerts { get; }
    public MaxWidth Width { get; set; }

    public void Add(OneOf<string, MarkupString, RenderFragment> content, Severity severity, bool overwrite = true,
        string? key = null);

    public void ClearAndAdd(OneOf<string, MarkupString, RenderFragment> content, Severity severity, bool overwrite = true,
        string? key = null);

    public Task ShowDialogForException(Exception ex, Severity severity,
        DialogOptions? options = null);

    public Task ShowDialog(OneOf<string, MarkupString, RenderFragment> content, Severity severity,
        DialogOptions? options = null, string? title = null);

    public void Remove(string key);
    public void ClearErrors();
    public void Clear(Severity severity);
    public void Clear();
    public bool HasError();
}
