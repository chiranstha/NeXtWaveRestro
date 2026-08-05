using Abp.Runtime.Caching;
using NextWave.Erp.Authorization.Accounts;
using NextWave.Erp.Authorization.PasswordlessLogin;
using NextWave.Erp.Authorization.Accounts.Dto;
using System.Threading.Tasks;
using Abp.UI;
using Shouldly;
using Xunit;

namespace NextWave.Erp.Tests.Authorization.PasswordlessLogin
{
    public class AccountAppService_VerifyPasswordlessLoginCode_Tests : PasswordlessLognTestBase
    {
        private readonly IAccountAppService _accountAppService;
        private readonly ICacheManager _cacheManager;

        public AccountAppService_VerifyPasswordlessLoginCode_Tests()
        {
            _accountAppService = Resolve<IAccountAppService>();
            _cacheManager = Resolve<ICacheManager>();

            SetMockCode();
        }

        [Fact]
        public async Task Verify_Valid_Passwordless_Login_Code()
        {
            await CreateAndSetUser();

            var cacheItem = new PasswordlessLoginCodeCacheItem
            {
                Code = MockCode
            };

            var input = new VerifyPasswordlessLoginCodeInput
            {
                ProviderValue = ProviderKeyEmail,
                Code = MockCode
            };

            await _cacheManager.GetPasswordlessVerificationCodeCache().SetAsync(
                GetPasswordlessLoginCodeCacheKey(AbpSession.TenantId, ProviderKeyEmail),
                cacheItem
            );

            await _accountAppService.VerifyPasswordlessLoginCode(input);
        }

        [Fact]
        public async Task Verify_Invalid_Passwordless_Login_Code()
        {
            await CreateAndSetUser();

            var cacheItem = new PasswordlessLoginCodeCacheItem
            {
                Code = MockCode
            };

            var input = new VerifyPasswordlessLoginCodeInput
            {
                ProviderValue = ProviderKeyEmail,
                Code = "123456"
            };

            await _cacheManager.GetPasswordlessVerificationCodeCache().SetAsync(
                GetPasswordlessLoginCodeCacheKey(AbpSession.TenantId, ProviderKeyEmail),
                cacheItem
            );

            var exception = await Assert.ThrowsAsync<UserFriendlyException>(async () =>
            {
                await _accountAppService.VerifyPasswordlessLoginCode(input);
            });
            exception.Message.ShouldBe("Wrong verification code!");
        }
    }
}