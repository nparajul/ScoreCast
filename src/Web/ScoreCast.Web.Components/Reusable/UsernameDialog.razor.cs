using ScoreCast.Models.V1.Requests.UserManagement;

namespace ScoreCast.Web.Components.Reusable;

public partial class UsernameDialog
{
    [CascadingParameter] public IMudDialogInstance Dialog { get; set; } = null!;
    [Inject] private IScoreCastApiClient Api { get; set; } = null!;

    private string? _username;
    private string? _error;
    private bool _submitting;

    private async Task Submit()
    {
        if (string.IsNullOrWhiteSpace(_username)) return;

        _error = null;
        _submitting = true;

        var result = await Api.SetUsernameAsync(
            new SetUsernameRequest { Username = _username }, CancellationToken.None);

        _submitting = false;

        if (result.Success)
        {
            Dialog.Close(DialogResult.Ok(true));
            return;
        }

        _error = result.Message ?? "Something went wrong";
    }
}
