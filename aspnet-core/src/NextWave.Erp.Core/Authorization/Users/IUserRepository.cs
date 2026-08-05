using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Abp.Domain.Repositories;

namespace NextWave.Erp.Authorization.Users;

public interface IUserRepository : IRepository<User, long>
{
    List<long> GetPasswordExpiredUserIds(DateTime passwordExpireDate);

    Task<User> FindByPhoneNumberAsync(string phoneNumber);

    void UpdateUsersToChangePasswordOnNextLogin(List<long> userIdsToUpdate);
}

