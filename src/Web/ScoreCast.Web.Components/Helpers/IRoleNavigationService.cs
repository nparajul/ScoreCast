using ScoreCast.Models.V1.Responses.UserManagement;

namespace ScoreCast.Web.Components.Helpers;

public interface IRoleNavigationService
{
    List<RoleResult> Roles { get; }
    RoleResult? SelectedRole { get; }
    List<PageResult> Pages { get; }
    event Action? OnChanged;
    Task LoadRolesAsync();
    Task SelectRoleAsync(RoleResult role);
}
