using Abp;
using System.Threading.Tasks;

namespace NextWave.Erp.Authorization.Users.DataCleaners;

public interface IUserDataCleaner
{
    Task CleanUserData(UserIdentifier userIdentifier);
}

