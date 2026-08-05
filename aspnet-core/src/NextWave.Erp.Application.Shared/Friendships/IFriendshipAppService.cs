using System.Threading.Tasks;
using Abp.Application.Services;
using NextWave.Erp.Friendships.Dto;

namespace NextWave.Erp.Friendships;

public interface IFriendshipAppService : IApplicationService
{
    Task<FriendDto> CreateFriendshipRequest(CreateFriendshipRequestInput input);

    Task<FriendDto> CreateFriendshipWithDifferentTenant(CreateFriendshipWithDifferentTenantInput input);

    Task<FriendDto> CreateFriendshipForCurrentTenant(CreateFriendshipForCurrentTenantInput input);

    Task BlockUser(BlockUserInput input);

    Task UnblockUser(UnblockUserInput input);

    Task AcceptFriendshipRequest(AcceptFriendshipRequestInput input);

    Task RemoveFriend(RemoveFriendInput input);
}

