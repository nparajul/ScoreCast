using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Routing;
using OneOf;
using ScoreCast.Web.Components.Reusable;
using Severity = MudBlazor.Severity;

namespace ScoreCast.Web.Components.Helpers;

public sealed partial class AlertService : IAlertService, IDisposable
{
    private readonly Dictionary<Severity, List<ScoreCastAlert>> _alerts = [];
    private readonly IDialogService Dialog;
    private readonly NavigationManager Navigation;
    private readonly INotifyService Notify;

    private MaxWidth _width = MaxWidth.Large;

    public AlertService(INotifyService notify, IDialogService dialog, NavigationManager navigation)
    {
        Notify = notify;
        Dialog = dialog;
        Navigation = navigation;
        Navigation.LocationChanged += LocationChanged;
    }

    public IEnumerable<ScoreCastAlert> Alerts => _alerts.Values.SelectMany(x => x);

    public MaxWidth Width
    {
        get => _width;
        set
        {
            _width = value;
            Notify.Notify();
        }
    }

    public void Add(OneOf<string, MarkupString, RenderFragment> content, Severity severity, bool overwrite = true,
        string? key = null)
    {
        var alert = GetScoreCastAlert(content, severity, key);

        _alerts[severity] = overwrite || !_alerts.TryGetValue(severity, out var existing)
            ? [alert]
            : [.. existing!, alert];

        if (overwrite && severity == Severity.Success)
            ClearErrors();

        Notify.Notify();
    }

    public void ClearAndAdd(OneOf<string, MarkupString, RenderFragment> content, Severity severity, bool overwrite = true,
        string? key = null)
    {
        Clear();
        Add(content, severity, overwrite, key);
    }

    public async Task ShowDialogForException(Exception ex, Severity severity,
        DialogOptions? options = null)
    {
        options ??= new DialogOptions { CloseButton = true, MaxWidth = MaxWidth.False };
        var parameters = new DialogParameters
        {
            ["Exception"] = ex,
            ["Severity"] = severity
        };

        await Dialog.ShowAsync<ExceptionDialog>("An Exception Occured", parameters, options);
    }

    public async Task ShowDialog(OneOf<string, MarkupString, RenderFragment> content, Severity severity,
        DialogOptions? options = null, string? title = null)
    {
        options ??= new DialogOptions { CloseButton = true, MaxWidth = MaxWidth.Small };
        var alert = GetScoreCastAlert(content, severity);
        await Dialog.ShowAsync<AlertServiceDialog>(title, new DialogParameters { ["Alert"] = alert, ["Title"] = title }, options);
    }

    public void ClearErrors()
    {
        _alerts[Severity.Error] = _alerts[Severity.Warning] = [];
        Notify.Notify();
    }

    public void Clear(Severity severity)
    {
        if (_alerts.ContainsKey(severity))
        {
            _alerts[severity].Clear();
            Notify.Notify();
        }
    }

    public void Clear()
    {
        _alerts.Clear();
        Notify.Notify();
    }

    public bool HasError()
    {
        if (_alerts.TryGetValue(Severity.Error, out var errors) && errors.Count > 0)
            return true;
        if (_alerts.TryGetValue(Severity.Warning, out var warnings) && warnings.Count > 0)
            return true;
        return false;
    }

    public void Remove(string key)
    {
        if (key == null)
            return;

        var alert = Alerts.FirstOrDefault(x => x.Key == key);
        if (alert == null)
            return;

        _alerts[alert.Severity].Remove(alert);
        Notify.Notify();
    }

    public void Dispose()
    {
        Navigation.LocationChanged -= LocationChanged;
    }

    [GeneratedRegex(@"(<br\s*\/?\s*>|\r\n|\n|\r)")]
    private static partial Regex BreakRegex();

    private static ScoreCastAlert GetScoreCastAlert(OneOf<string, MarkupString, RenderFragment> content, Severity severity,
        string? key = null)
    {
        var isString = content.TryPickT0(out var str, out var nonStr);
        var alert = new ScoreCastAlert
        {
            Content = isString ? ParseString(str) : nonStr,
            Severity = severity,
            Key = key ?? content.GetHashCode().ToString()
        };
        return alert;
    }

    private static RenderFragment ParseString(string str)
    {
        var lines = BreakRegex().Split(str)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .ToArray();

        void RenderFragment(RenderTreeBuilder builder)
        {
            for (var i = 0; i < lines.Length; i++)
            {
                if (i > 0)
                {
                    builder.OpenElement((i - 1) * 2 + 1, "br");
                    builder.CloseElement();
                }

                builder.AddContent(i * 2, lines[i]);
            }
        }

        return RenderFragment;
    }

    private void LocationChanged(object? sender, LocationChangedEventArgs e)
    {
        Clear();
    }
}
