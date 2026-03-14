using ScoreCast.Models.V1.Responses.UserManagement;

namespace ScoreCast.Web.Components.Helpers;

public sealed class RoleNavigationService(IScoreCastApiClient Api) : IRoleNavigationService
{
    public List<RoleResult> Roles { get; private set; } = [];
    public RoleResult? SelectedRole { get; private set; }
    public List<PageResult> Pages { get; private set; } = [];
    public event Action? OnChanged;

    public async Task LoadRolesAsync()
    {
        var response = await Api.GetMyRolesAsync(CancellationToken.None);
        if (response.Success && response.Data is not null)
            Roles = response.Data;

        if (Roles.Count > 0 && SelectedRole is null)
            await SelectRoleAsync(Roles[0]);
    }

    public async Task SelectRoleAsync(RoleResult role)
    {
        SelectedRole = role;
        var response = await Api.GetRolePagesAsync(role.Id, CancellationToken.None);
        if (response.Success && response.Data is not null)
            Pages = response.Data;

        OnChanged?.Invoke();
    }
}
