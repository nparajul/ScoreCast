namespace ScoreCast.Web.Components.Reusable;

public partial class WelcomeDialog
{
    [CascadingParameter] public IMudDialogInstance Dialog { get; set; } = default!;

    private string? DisplayName { get; set; }

    private void Skip() => Dialog.Close(DialogResult.Ok<string?>(null));

    private void Save() => Dialog.Close(DialogResult.Ok(DisplayName?.Trim()));
}
