using System.Linq;
using System.Threading.Tasks;
using Abp.Configuration;
using Abp.Zero.Configuration;
using NextWave.Erp.Authorization.Users;
using Shouldly;
using Xunit;

namespace NextWave.Erp.Tests.Authorization.Users
{
    public class UserManager_Tests : UserAppServiceTestBase
    {
        private readonly ISettingManager _settingManager;
        private readonly UserManager _userManager;

        public UserManager_Tests()
        {
            _settingManager = Resolve<ISettingManager>();
            _userManager = Resolve<UserManager>();

            LoginAsDefaultTenantAdmin();
        }

        [Fact]
        public async Task Should_Create_User_With_Random_Password_For_Tenant()
        {
            await _settingManager.ChangeSettingForApplicationAsync(AbpZeroSettingNames.UserManagement.PasswordComplexity.RequireUppercase, "true");
            await _settingManager.ChangeSettingForApplicationAsync(AbpZeroSettingNames.UserManagement.PasswordComplexity.RequireNonAlphanumeric, "true");
            await _settingManager.ChangeSettingForApplicationAsync(AbpZeroSettingNames.UserManagement.PasswordComplexity.RequiredLength, "25");

            var randomPassword = await _userManager.CreateRandomPassword();

            randomPassword.Length.ShouldBeGreaterThanOrEqualTo(25);
            randomPassword.Any(char.IsUpper).ShouldBeTrue();
            randomPassword.Any(char.IsLetterOrDigit).ShouldBeTrue();
        }

        [Fact]
        public async Task Should_Resolve_Tenant_From_Email_Without_Tenant_Context()
        {
            AbpSession.TenantId = null;
            AbpSession.UserId = null;

            var resolved = await _userManager.TryResolveTenantLoginAsync("admin@defaulttenant.com");

            resolved.TenantId.ShouldBe(1);
            resolved.LoginIdentifier.ShouldBe("admin@defaulttenant.com");
        }

        [Fact]
        public async Task Should_Resolve_Email_Login_From_Unique_Phone_Number()
        {
            CreateTestUsers();
            UsingDbContext(context =>
            {
                var user = context.Users.Single(item => item.UserName == "jnash");
                user.PhoneNumber = "9800000001";
            });
            AbpSession.TenantId = null;
            AbpSession.UserId = null;

            var resolved = await _userManager.TryResolveTenantLoginAsync("9800000001");

            resolved.TenantId.ShouldBe(1);
            resolved.LoginIdentifier.ShouldBe("jnsh2000@testdomain.com");
        }
    }
}
