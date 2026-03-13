using Microsoft.AspNetCore.Http;

namespace ScoreCast.Ws.Endpoints.V1.UserManagement;

public sealed class UserManagementGroup : Group
{
    public UserManagementGroup()
    {
        Configure("users",
            ep => { ep.Description(x=>x.WithTags("User Management")); });
    }
}
