using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;
using ScoreCast.Web.Components.Helpers;

namespace ScoreCast.Web.Components.Reusable;

public partial class AlertServiceDialog
{
    [Parameter] public required ScoreCastAlert Alert { get; set; }
    [Parameter] public string? Title { get; set; }

    [CascadingParameter] public IMudDialogInstance? Dialog { get; set; }

    private Color GetColor() => Alert.Severity switch
    {
        Severity.Error => Color.Error,
        Severity.Warning => Color.Warning,
        _ => Color.Primary
    };

    private void CloseDialog() => Dialog?.Close(DialogResult.Ok(true));

    private void CloseIfEnter(KeyboardEventArgs e)
    {
        if (e.Key is "Tab" or "Enter")
            CloseDialog();
    }
}
