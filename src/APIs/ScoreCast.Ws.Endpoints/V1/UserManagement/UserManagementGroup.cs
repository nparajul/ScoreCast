namespace ScoreCast.Ws.Endpoints.V1.UserManagement;

public sealed class UserManagementGroup : Group
{
    public UserManagementGroup()
    {
        Configure("api/v1/users", ep => { });
    }
}
