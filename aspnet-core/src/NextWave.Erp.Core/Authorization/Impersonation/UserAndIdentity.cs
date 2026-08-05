using System.Security.Claims;
using NextWave.Erp.Authorization.Users;

namespace NextWave.Erp.Authorization.Impersonation;

public class UserAndIdentity
{
    public User User { get; set; }

    public ClaimsIdentity Identity { get; set; }

    public UserAndIdentity(User user, ClaimsIdentity identity)
    {
        User = user;
        Identity = identity;
    }
}

