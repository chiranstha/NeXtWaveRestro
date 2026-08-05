using System.Threading.Tasks;
using Abp.Domain.Policies;

namespace NextWave.Erp.Authorization.Users;

public interface IUserPolicy : IPolicy
{
    Task CheckMaxUserCountAsync(int tenantId);
}

