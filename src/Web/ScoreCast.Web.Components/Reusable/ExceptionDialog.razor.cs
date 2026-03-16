using Microsoft.JSInterop;

namespace ScoreCast.Web.Components.Reusable;

public partial class ExceptionDialog
{
    [CascadingParameter] private IMudDialogInstance Dialog { get; set; } = null!;
    [Parameter] public required Exception Exception { get; set; }
    [Parameter] public Severity Severity { get; set; }
    [Inject] private ISnackbar Snackbar { get; set; } = null!;

    private bool ShowDetails { get; set; }

    private void Close() => Dialog.Close(DialogResult.Ok(true));

    private string GetTitle() => Severity switch
    {
        Severity.Error => "Something Went Wrong",
        Severity.Warning => "Warning",
        _ => "An Issue Occurred"
    };

    private string GetIcon() => Severity switch
    {
        Severity.Error => Icons.Material.Filled.ErrorOutline,
        Severity.Warning => Icons.Material.Filled.WarningAmber,
        _ => Icons.Material.Filled.Info
    };

    private Color GetColor() => Severity switch
    {
        Severity.Error => Color.Error,
        Severity.Warning => Color.Warning,
        _ => Color.Primary
    };

    private string GetUserFriendlyMessage()
    {
        var msg = Exception.Message;
        if (msg.Contains("status code does not indicate success"))
            return "Unable to reach the server. Please check your connection and try again.";
        if (msg.Contains("Failed to fetch") || msg.Contains("NetworkError"))
            return "A network error occurred. Please check your connection.";
        return msg;
    }

    private async Task CopyStackTrace()
    {
        if (!string.IsNullOrEmpty(Exception.StackTrace))
        {
            await CopyToClipboard(Exception.StackTrace);
            Snackbar.Add("Stack trace copied", Severity.Success);
        }
    }

    private async Task CopyAllDetails()
    {
        var details = $"Error: {Exception.GetType().FullName}\nMessage: {Exception.Message}\n\nStack Trace:\n{Exception.StackTrace}";
        if (Exception.InnerException is not null)
            details += $"\n\nInner Exception: {Exception.InnerException.Message}\n{Exception.InnerException.StackTrace}";
        await CopyToClipboard(details);
        Snackbar.Add("Exception details copied", Severity.Success);
    }

    private async Task CopyToClipboard(string text)
    {
        try { await Js.InvokeVoidAsync("navigator.clipboard.writeText", text); }
        catch { Snackbar.Add("Could not copy to clipboard", Severity.Warning); }
    }
}
