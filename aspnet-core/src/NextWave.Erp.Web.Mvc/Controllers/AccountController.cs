using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;
using Abp;
using Abp.AspNetCore.Mvc.Authorization;
using Abp.Authorization;
using Abp.Authorization.Users;
using Abp.Configuration;
using Abp.Configuration.Startup;
using Abp.Domain.Uow;
using Abp.Extensions;
using Abp.MultiTenancy;
using Abp.Net.Mail;
using Abp.Notifications;
using Abp.Runtime.Session;
using Abp.Timing;
using Abp.UI;
using Abp.Web.Models;
using Abp.Zero.Configuration;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Options;
using NextWave.Erp.Authentication.TwoFactor.Google;
using NextWave.Erp.Authorization;
using NextWave.Erp.Authorization.Accounts;
using NextWave.Erp.Authorization.Accounts.Dto;
using NextWave.Erp.Authorization.Delegation;
using NextWave.Erp.Authorization.Impersonation;
using NextWave.Erp.Authorization.Users;
using NextWave.Erp.Configuration;
using NextWave.Erp.Identity;
using NextWave.Erp.MultiTenancy;
using NextWave.Erp.Net.Sms;
using NextWave.Erp.Notifications;
using NextWave.Erp.Web.Models.Account;
using NextWave.Erp.Security;
using NextWave.Erp.Security.Recaptcha;
using NextWave.Erp.Sessions;
using NextWave.Erp.Url;
using NextWave.Erp.Web.Authentication.External;
using NextWave.Erp.Web.Security.Recaptcha;
using NextWave.Erp.Web.Session;
using NextWave.Erp.Web.Views.Shared.Components.TenantChange;
using Abp.CachedUniqueKeys;
using Abp.AspNetCore.Mvc.Caching;
using Abp.HtmlSanitizer;
using Abp.Localization;
using NextWave.Erp.Authorization.Users.Profile;
using NextWave.Erp.Authorization.Users.Profile.Dto;
using Abp.Runtime.Security;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using NextWave.Erp.Authentication.PasswordlessLogin;
using NextWave.Erp.Authorization.PasswordlessLogin;
using NextWave.Erp.Authorization.QrLogin;
using NextWave.Erp.Web.Authentication.JwtBearer;

namespace NextWave.Erp.Web.Controllers;

public class AccountController : ErpControllerBase
{
    private readonly UserManager _userManager;
    private readonly TenantManager _tenantManager;
    private readonly IMultiTenancyConfig _multiTenancyConfig;
    private readonly IUnitOfWorkManager _unitOfWorkManager;
    private readonly IWebUrlService _webUrlService;
    private readonly IAppUrlService _appUrlService;
    private readonly IAppNotifier _appNotifier;
    private readonly AbpLoginResultTypeHelper _abpLoginResultTypeHelper;
    private readonly IUserLinkManager _userLinkManager;
    private readonly LogInManager _logInManager;
    private readonly SignInManager _signInManager;
    private readonly IRecaptchaValidator _recaptchaValidator;
    private readonly IPerRequestSessionCache _sessionCache;
    private readonly ITenantCache _tenantCache;
    private readonly IAccountAppService _accountAppService;
    private readonly UserRegistrationManager _userRegistrationManager;
    private readonly IImpersonationManager _impersonationManager;
    private readonly ISmsSender _smsSender;
    private readonly IEmailSender _emailSender;
    private readonly IPasswordComplexitySettingStore _passwordComplexitySettingStore;
    private readonly IdentityOptions _identityOptions;
    private readonly ISessionAppService _sessionAppService;
    private readonly ExternalLoginInfoManagerFactory _externalLoginInfoManagerFactory;
    private readonly ISettingManager _settingManager;
    private readonly IUserDelegationManager _userDelegationManager;
    private readonly ICachedUniqueKeyPerUser _cachedUniqueKeyPerUser;
    private readonly IGetScriptsResponsePerUserConfiguration _getScriptsResponsePerUserConfiguration;
    private readonly IProfileAppService _profileAppService;
    private readonly IPasswordlessLoginManager _passwordlessLoginManager;
    private readonly IOptions<AsyncJwtBearerOptions> _jwtOptions;
    private readonly TokenAuthConfiguration _configuration;
    private readonly IQrLoginManager _qrLoginManager;

    public AccountController(
        UserManager userManager,
        IMultiTenancyConfig multiTenancyConfig,
        TenantManager tenantManager,
        IUnitOfWorkManager unitOfWorkManager,
        IAppNotifier appNotifier,
        IWebUrlService webUrlService,
        AbpLoginResultTypeHelper abpLoginResultTypeHelper,
        IUserLinkManager userLinkManager,
        LogInManager logInManager,
        SignInManager signInManager,
        IRecaptchaValidator recaptchaValidator,
        ITenantCache tenantCache,
        IAccountAppService accountAppService,
        UserRegistrationManager userRegistrationManager,
        IImpersonationManager impersonationManager,
        IAppUrlService appUrlService,
        IPerRequestSessionCache sessionCache,
        IEmailSender emailSender,
        ISmsSender smsSender,
        IPasswordComplexitySettingStore passwordComplexitySettingStore,
        IOptions<IdentityOptions> identityOptions,
        ISessionAppService sessionAppService,
        ExternalLoginInfoManagerFactory externalLoginInfoManagerFactory,
        ISettingManager settingManager,
        IUserDelegationManager userDelegationManager,
        ICachedUniqueKeyPerUser cachedUniqueKeyPerUser,
        IGetScriptsResponsePerUserConfiguration getScriptsResponsePerUserConfiguration,
        IProfileAppService profileAppService,
        IPasswordlessLoginManager passwordlessLoginManager,
        IOptions<AsyncJwtBearerOptions> jwtOptions,
        TokenAuthConfiguration configuration,
        IQrLoginManager qrLoginManager)
    {
        _userManager = userManager;
        _multiTenancyConfig = multiTenancyConfig;
        _tenantManager = tenantManager;
        _unitOfWorkManager = unitOfWorkManager;
        _webUrlService = webUrlService;
        _appNotifier = appNotifier;
        _abpLoginResultTypeHelper = abpLoginResultTypeHelper;
        _userLinkManager = userLinkManager;
        _logInManager = logInManager;
        _signInManager = signInManager;
        _recaptchaValidator = recaptchaValidator;
        _tenantCache = tenantCache;
        _accountAppService = accountAppService;
        _userRegistrationManager = userRegistrationManager;
        _impersonationManager = impersonationManager;
        _appUrlService = appUrlService;
        _sessionCache = sessionCache;
        _emailSender = emailSender;
        _smsSender = smsSender;
        _passwordComplexitySettingStore = passwordComplexitySettingStore;
        _identityOptions = identityOptions.Value;
        _sessionAppService = sessionAppService;
        _externalLoginInfoManagerFactory = externalLoginInfoManagerFactory;
        _settingManager = settingManager;
        _userDelegationManager = userDelegationManager;
        _cachedUniqueKeyPerUser = cachedUniqueKeyPerUser;
        _getScriptsResponsePerUserConfiguration = getScriptsResponsePerUserConfiguration;
        _profileAppService = profileAppService;
        _passwordlessLoginManager = passwordlessLoginManager;
        _jwtOptions = jwtOptions;
        _configuration = configuration;
        _qrLoginManager = qrLoginManager;
    }

    #region Login / Logout

    public async Task<ActionResult> Login(string userNameOrEmailAddress = "", string returnUrl = "",
        string successMessage = "", string ss = "")
    {
        if (!string.IsNullOrEmpty(ss) && ss.Equals("true", StringComparison.OrdinalIgnoreCase) &&
            AbpSession.UserId > 0)
        {
            var updateUserSignInTokenOutput = await _sessionAppService.UpdateUserSignInToken();
            returnUrl = AddSingleSignInParametersToReturnUrl(
                returnUrl, updateUserSignInTokenOutput.SignInToken,
                AbpSession.UserId.Value,
                AbpSession.TenantId
            );

            return Redirect(returnUrl);
        }

        ViewBag.ReturnUrl = NormalizeReturnUrl(returnUrl);
        ViewBag.IsMultiTenancyEnabled = _multiTenancyConfig.IsEnabled;
        ViewBag.SingleSignIn = ss;
        ViewBag.UseCaptcha = UseCaptchaOnLogin();

        return View(
            new LoginFormViewModel
            {
                IsSelfRegistrationEnabled = IsSelfRegistrationEnabled(),
                IsTenantSelfRegistrationEnabled = IsTenantSelfRegistrationEnabled(),
                IsPasswordlessLoginEnabled = await IsPasswordlessLoginEnabled(),
                IsQrLoginEnabled = await IsQrLoginEnabled(),
                SuccessMessage = successMessage,
                UserNameOrEmailAddress = userNameOrEmailAddress
            }
        );
    }

    [HttpPost]
    public virtual async Task<JsonResult> Login(LoginViewModel loginModel, string returnUrl = "",
        string returnUrlHash = "", string ss = "")
    {
        returnUrl = NormalizeReturnUrl(returnUrl);
        if (!string.IsNullOrWhiteSpace(returnUrlHash))
        {
            returnUrl = returnUrl + returnUrlHash;
        }

        if (UseCaptchaOnLogin())
        {
            await _recaptchaValidator.ValidateAsync(
                HttpContext.Request.Form[RecaptchaValidator.RecaptchaResponseKey]);
        }

        var loginResult = await GetLoginResultAsync(
            loginModel.UsernameOrEmailAddress,
            loginModel.Password,
            GetTenancyNameOrNull()
        );

        if (!string.IsNullOrEmpty(ss) && ss.Equals("true", StringComparison.OrdinalIgnoreCase) &&
            loginResult.Result == AbpLoginResultType.Success)
        {
            loginResult.User.SetSignInToken();
            returnUrl = AddSingleSignInParametersToReturnUrl(returnUrl, loginResult.User.SignInToken,
                loginResult.User.Id, loginResult.User.TenantId);
        }

        if (_settingManager.GetSettingValue<bool>(AppSettings.UserManagement.AllowOneConcurrentLoginPerUser))
        {
            await _userManager.UpdateSecurityStampAsync(loginResult.User);
        }

        if (loginResult.User.ShouldChangePasswordOnNextLogin)
        {
            loginResult.User.SetNewPasswordResetCode();

            var expireDate =
                Uri.EscapeDataString(Clock.Now.AddHours(1).ToString(ErpConsts.DateTimeOffsetFormat));

            var code =
                $"?userId={loginResult.User.Id}&resetCode={loginResult.User.PasswordResetCode}&expireDate={expireDate}";

            if (loginResult.User.TenantId.HasValue)
            {
                code += $"&tenantId={loginResult.User.TenantId.ToString()}";
            }

            var encryptedCode = SimpleStringCipher.Instance.Encrypt(code);

            return Json(new AjaxResponse
            {
                TargetUrl = Url.Action(
                    "ResetPassword",
                    new ResetPasswordViewModel
                    {
                        ReturnUrl = returnUrl,
                        c = encryptedCode,
                        SingleSignIn = ss
                    })
            });
        }

        var signInResult = await _signInManager.SignInOrTwoFactorAsync(loginResult, loginModel.RememberMe);
        if (signInResult.RequiresTwoFactor)
        {
            return Json(new AjaxResponse
            {
                TargetUrl = Url.Action(
                    "SendSecurityCode",
                    new
                    {
                        returnUrl = returnUrl,
                        rememberMe = loginModel.RememberMe
                    })
            });
        }

        Debug.Assert(signInResult.Succeeded);

        await UnitOfWorkManager.Current.SaveChangesAsync();

        return Json(new AjaxResponse { TargetUrl = returnUrl });
    }

    public async Task<ActionResult> Logout(string returnUrl = "")
    {
        await _signInManager.SignOutAsync();
        var userIdentifier = AbpSession.ToUserIdentifier();

        if (userIdentifier != null &&
            _settingManager.GetSettingValue<bool>(AppSettings.UserManagement.AllowOneConcurrentLoginPerUser))
        {
            var user = await _userManager.GetUserAsync(userIdentifier);
            await _userManager.UpdateSecurityStampAsync(user);
        }

        if (!string.IsNullOrEmpty(returnUrl))
        {
            returnUrl = NormalizeReturnUrl(returnUrl);
            return Redirect(returnUrl);
        }

        return RedirectToAction("Login");
    }

    private async Task SaveTwoFactorFailedLoginAttempt(Tenant tenant, User user)
    {
        var loginResult = new AbpLoginResult<Tenant, User>(AbpLoginResultType.FailedForOtherReason, tenant, user);
        loginResult.SetFailReason(GetLocalizableString("TwoFactorCodeVerificationFailed"));

        await _logInManager.SaveLoginAttemptAsync(
            loginResult,
            GetTenancyNameOrNull(),
            user.UserName
        );
    }

    private static ILocalizableString GetLocalizableString(string name)
    {
        return new LocalizableString(name, ErpConsts.LocalizationSourceName);
    }

    private async Task<AbpLoginResult<Tenant, User>> GetLoginResultAsync(string usernameOrEmailAddress,
        string password, string tenancyName)
    {
        var shouldLockout = await IsUserLockoutEnabled();
        var loginResult =
            await _logInManager.LoginAsync(usernameOrEmailAddress, password, tenancyName, shouldLockout);

        switch (loginResult.Result)
        {
            case AbpLoginResultType.Success:
                return loginResult;
            default:
                throw _abpLoginResultTypeHelper.CreateExceptionForFailedLoginAttempt(loginResult.Result,
                    usernameOrEmailAddress, tenancyName);
        }
    }

    private string AddSingleSignInParametersToReturnUrl(string returnUrl, string signInToken, long userId,
        int? tenantId)
    {
        returnUrl += (returnUrl.Contains("?") ? "&" : "?") +
                     "accessToken=" + signInToken +
                     "&userId=" + Convert.ToBase64String(Encoding.UTF8.GetBytes(userId.ToString()));
        if (tenantId.HasValue)
        {
            returnUrl += "&tenantId=" + Convert.ToBase64String(Encoding.UTF8.GetBytes(tenantId.Value.ToString()));
        }

        return returnUrl;
    }

    public ActionResult SessionLockScreen(string returnUrl)
    {
        returnUrl = NormalizeReturnUrl(returnUrl);

        ViewBag.ReturnUrl = returnUrl;
        ViewBag.UseCaptcha = UseCaptchaOnLogin();

        return View();
    }

    #endregion

    #region Two Factor Auth

    public async Task<ActionResult> SendSecurityCode(string returnUrl, bool rememberMe = false)
    {
        var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
        if (user == null)
        {
            return RedirectToAction("Login");
        }

        CheckCurrentTenant(await _signInManager.GetVerifiedTenantIdAsync());

        var userProviders = await _userManager.GetValidTwoFactorProvidersAsync(user);

        var factorOptions = userProviders.Select(
            userProvider =>
                new SelectListItem
                {
                    Text = userProvider,
                    Value = userProvider
                }).ToList();

        return View(
            new SendSecurityCodeViewModel
            {
                Providers = factorOptions,
                ReturnUrl = returnUrl,
                RememberMe = rememberMe
            }
        );
    }

    [HttpPost]
    public async Task<ActionResult> SendSecurityCode(SendSecurityCodeViewModel model)
    {
        var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
        if (user == null)
        {
            return RedirectToAction("Login");
        }

        CheckCurrentTenant(await _signInManager.GetVerifiedTenantIdAsync());

        if (model.SelectedProvider != GoogleAuthenticatorProvider.Name)
        {
            var code = await _userManager.GenerateTwoFactorTokenAsync(user, model.SelectedProvider);
            var message = L("EmailSecurityCodeBody", code);

            if (model.SelectedProvider == "Email")
            {
                await _emailSender.SendAsync(await _userManager.GetEmailAsync(user), L("EmailSecurityCodeSubject"),
                    message);
            }
            else if (model.SelectedProvider == "Phone")
            {
                await _smsSender.SendAsync(await _userManager.GetPhoneNumberAsync(user), message);
            }
        }

        return RedirectToAction(
            "VerifySecurityCode",
            new
            {
                provider = model.SelectedProvider,
                returnUrl = model.ReturnUrl,
                rememberMe = model.RememberMe
            }
        );
    }

    public async Task<ActionResult> VerifySecurityCode(string provider, string returnUrl, bool rememberMe)
    {
        var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
        if (user == null)
        {
            throw new UserFriendlyException(L("VerifySecurityCodeNotLoggedInErrorMessage"));
        }

        CheckCurrentTenant(await _signInManager.GetVerifiedTenantIdAsync());

        var isRememberBrowserEnabled = await IsRememberBrowserEnabledAsync();

        return View(
            new VerifySecurityCodeViewModel
            {
                Provider = provider,
                ReturnUrl = returnUrl,
                RememberMe = rememberMe,
                IsRememberBrowserEnabled = isRememberBrowserEnabled
            }
        );
    }

    [UnitOfWork(IsDisabled = true)]
    [HttpPost]
    public virtual async Task<JsonResult> VerifySecurityCode(VerifySecurityCodeViewModel model)
    {
        var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();

        if (user == null)
        {
            throw new UserFriendlyException(L("UserNotFound"));
        }

        Tenant tenant = null;

        if (user.TenantId.HasValue)
        {
            tenant = await _tenantManager.GetByIdAsync(user.TenantId.Value);
        }

        var signInResultResult = await _unitOfWorkManager.WithUnitOfWorkAsync(async () =>
        {
            model.ReturnUrl = NormalizeReturnUrl(model.ReturnUrl);

            CheckCurrentTenant(await _signInManager.GetVerifiedTenantIdAsync());

            var rememberClient = await IsRememberBrowserEnabledAsync() && model.RememberBrowser;

            var result = await _signInManager.TwoFactorSignInAsync(
                model.Provider,
                model.Code,
                model.RememberMe,
                rememberClient
            );

            if (model.Provider == GoogleAuthenticatorProvider.Name && !result.Succeeded)
            {
                result = await _signInManager.TwoFactorRecoveryCodeSignInAsync(model.Code);

                if (result.Succeeded)
                {
                    await _profileAppService.DisableGoogleAuthenticator(new VerifyAuthenticatorCodeInput
                    {
                        Code = model.Code
                    });
                }
            }

            return result;
        });

        if (signInResultResult.Succeeded)
        {
            await _logInManager.SaveLoginAttemptAsync(
                new AbpLoginResult<Tenant, User>(AbpLoginResultType.Success, tenant, user),
                GetTenancyNameOrNull(),
                user.UserName
            );

            return Json(new AjaxResponse { TargetUrl = model.ReturnUrl });
        }

        await SaveTwoFactorFailedLoginAttempt(tenant, user);

        if (signInResultResult.IsLockedOut)
        {
            throw new UserFriendlyException(L("UserLockedOutMessage"));
        }

        throw new UserFriendlyException(L("InvalidSecurityCode"));
    }

    private Task<bool> IsRememberBrowserEnabledAsync()
    {
        return SettingManager.GetSettingValueAsync<bool>(AbpZeroSettingNames.UserManagement.TwoFactorLogin
            .IsRememberBrowserEnabled);
    }

    #endregion

    #region Passwordless Login

    public async Task<ActionResult> PasswordlessLogin()
    {
        var providers = _passwordlessLoginManager.GetProviders();

        return View(new PasswordlessLoginFormViewModel
        {
            Providers = providers,
            IsEmailPasswordlessLoginEnabled = await IsEmailPasswordlessLoginEnabled(),
            IsSmsPasswordlessLoginEnabled = await IsSmsPasswordlessLoginEnabled(),
            SelectedProvider = await GetDefaultSelectedProvider()
        });
    }

    [HttpPost]
    public virtual async Task<ActionResult> PasswordlessLogin(PasswordlessLoginViewModel model)
    {
        if (model.SelectedProviderValue.IsNullOrEmpty() || !await IsPasswordlessLoginEnabled())
        {
            return RedirectToAction("Login");
        }

        if (!Enum.TryParse<PasswordlessLoginProviderType>(model.SelectedProvider, out var selectedProvider))
        {
            return RedirectToAction("Login");
        }

        await _accountAppService.SendPasswordlessLoginCode(new SendPasswordlessLoginCodeInput
        {
            ProviderValue = model.SelectedProviderValue,
            ProviderType = selectedProvider,
        });

        return RedirectToAction("VerifyPasswordlessCode", new PasswordlessLoginViewModel
        {
            SelectedProviderValue = model.SelectedProviderValue,
            SelectedProvider = model.SelectedProvider
        });
    }

    public virtual ActionResult VerifyPasswordlessCode(PasswordlessLoginViewModel model)
    {
        return View(new VerifyPasswordlessCodeViewModel
        {
            ProviderValue = model.SelectedProviderValue,
            ProviderType = model.SelectedProvider
        });
    }

    [HttpPost]
    [EnableRateLimiting("PasswordlessLoginLimiter")]
    public virtual async Task<JsonResult> VerifyPasswordlessCode(VerifyPasswordlessCodeViewModel model)
    {
        await _passwordlessLoginManager.VerifyPasswordlessLoginCode(
            AbpSession.TenantId,
            model.ProviderValue,
            model.Code
        );

        var user = await _passwordlessLoginManager.GetUserByPasswordlessProviderAndKeyAsync(
            model.ProviderType,
            model.ProviderValue
        );

        await _signInManager.SignInAsync(user, false);

        user.SetSignInToken();

        await _passwordlessLoginManager.RemovePasswordlessLoginCode(
            AbpSession.TenantId,
            model.ProviderValue
        );

        return Json(new AjaxResponse { TargetUrl = GetAppHomeUrl() });
    }

    [HttpPost]
    public async Task<ActionResult> LoginWithAccessToken([FromBody]LoginWithAccessTokenModel model)
    {
        foreach (var validator in _jwtOptions.Value.AsyncSecurityTokenValidators)
        {
            try
            {
                var validationParameters = new TokenValidationParameters
                {
                    ValidAudience = _configuration.Audience,
                    ValidIssuer = _configuration.Issuer,
                    IssuerSigningKey = _configuration.SecurityKey
                };
                
                var (principal, _) = await validator.ValidateToken(
                    model.AccessToken,
                    validationParameters
                );

                var userIdentifierClaim = principal.FindFirst(c => c.Type == AppConsts.UserIdentifier);
                
                if (userIdentifierClaim == null)
                {
                    throw new SecurityTokenException("invalid token");
                }
                
                var userIdentifier = UserIdentifier.Parse(userIdentifierClaim.Value);
                var user = await _qrLoginManager.GetUserByUserIdentifierClaimAsync(userIdentifier);
                
                await _signInManager.SignInAsync(user, false);

                user.SetSignInToken();
            }
            catch (Exception ex)
            {
                Logger.Debug(ex.ToString(), ex);
            }
        }
        
        return Json(new AjaxResponse { TargetUrl = GetAppHomeUrl() });
    }

    private async Task<string> GetDefaultSelectedProvider()
    {
        bool isEmailEnabled = await IsEmailPasswordlessLoginEnabled();
        bool isSmsEnabled = await IsSmsPasswordlessLoginEnabled();

        if (isEmailEnabled && !isSmsEnabled)
        {
            return "Email";
        }
        else if (!isEmailEnabled && isSmsEnabled)
        {
            return "Sms";
        }
        else
        {
            return null;
        }
    }

    private async Task<bool> IsQrLoginEnabled()
    {
        return await SettingManager.GetSettingValueAsync<bool>(AppSettings.UserManagement.IsQrLoginEnabled)
            && await SettingManager.GetSettingValueForApplicationAsync<bool>(AppSettings.UserManagement.IsQrLoginEnabled);
    }

    private async Task<bool> IsPasswordlessLoginEnabled()
    {
        return await IsEmailPasswordlessLoginEnabled() || await IsSmsPasswordlessLoginEnabled();
    }


    private async Task<bool> IsEmailPasswordlessLoginEnabled()
    {
        return await SettingManager.GetSettingValueAsync<bool>(AppSettings.UserManagement.PasswordlessLogin
            .IsEmailPasswordlessLoginEnabled);
    }

    private async Task<bool> IsSmsPasswordlessLoginEnabled()
    {
        return await SettingManager.GetSettingValueAsync<bool>(AppSettings.UserManagement.PasswordlessLogin
            .IsSmsPasswordlessLoginEnabled);
    }

    #endregion

    #region Register

    public async Task<ActionResult> Register(string returnUrl = "", string ss = "")
    {
        return RegisterView(new RegisterViewModel
        {
            PasswordComplexitySetting = await _passwordComplexitySettingStore.GetSettingsAsync(),
            ReturnUrl = returnUrl,
            SingleSignIn = ss
        });
    }

    private ActionResult RegisterView(RegisterViewModel model)
    {
        CheckSelfRegistrationIsEnabled();

        ViewBag.UseCaptcha = !model.IsExternalLogin && UseCaptchaOnRegistration();

        return View("Register", model);
    }

    [HttpPost]
    [UnitOfWork(IsolationLevel.ReadUncommitted)]
    [HtmlSanitizer]
    public async Task<ActionResult> Register(RegisterViewModel model)
    {
        try
        {
            if (!model.IsExternalLogin && UseCaptchaOnRegistration())
            {
                await _recaptchaValidator.ValidateAsync(
                    HttpContext.Request.Form[RecaptchaValidator.RecaptchaResponseKey]);
            }

            ExternalLoginInfo externalLoginInfo = null;
            if (model.IsExternalLogin)
            {
                externalLoginInfo = await _signInManager.GetExternalLoginInfoAsync();
                if (externalLoginInfo == null)
                {
                    throw new Exception("Can not external login!");
                }

                using (var providerManager =
                       _externalLoginInfoManagerFactory.GetExternalLoginInfoManager(externalLoginInfo
                           .LoginProvider))
                {
                    model.UserName =
                        providerManager.Object.GetUserNameFromClaims(externalLoginInfo.Principal.Claims.ToList());
                }

                model.Password = await _userManager.CreateRandomPassword();
            }
            else
            {
                if (model.UserName.IsNullOrEmpty() || model.Password.IsNullOrEmpty())
                {
                    throw new UserFriendlyException(L("FormIsNotValidMessage"));
                }
            }

            var user = await _userRegistrationManager.RegisterAsync(
                model.Name,
                model.Surname,
                model.EmailAddress,
                model.UserName,
                model.Password,
                false,
                _appUrlService.CreateEmailActivationUrlFormat(AbpSession.TenantId)
            );

            //Getting tenant-specific settings
            var isEmailConfirmationRequiredForLogin =
                await SettingManager.GetSettingValueAsync<bool>(AbpZeroSettingNames.UserManagement
                    .IsEmailConfirmationRequiredForLogin);

            if (model.IsExternalLogin)
            {
                Debug.Assert(externalLoginInfo != null);

                if (string.Equals(externalLoginInfo.Principal.FindFirstValue(ClaimTypes.Email), model.EmailAddress,
                        StringComparison.OrdinalIgnoreCase))
                {
                    user.IsEmailConfirmed = true;
                }

                user.Logins = new List<UserLogin>
                    {
                        new UserLogin
                        {
                            LoginProvider = externalLoginInfo.LoginProvider,
                            ProviderKey = externalLoginInfo.ProviderKey,
                            TenantId = user.TenantId
                        }
                    };
            }

            await _unitOfWorkManager.Current.SaveChangesAsync();

            Debug.Assert(user.TenantId != null);

            var tenant = await _tenantManager.GetByIdAsync(user.TenantId.Value);

            //Directly login if possible
            if (user.IsActive && (user.IsEmailConfirmed || !isEmailConfirmationRequiredForLogin))
            {
                AbpLoginResult<Tenant, User> loginResult;
                if (externalLoginInfo != null)
                {
                    loginResult = await _logInManager.LoginAsync(externalLoginInfo, tenant.TenancyName);
                }
                else
                {
                    loginResult = await GetLoginResultAsync(user.UserName, model.Password, tenant.TenancyName);
                }

                if (loginResult.Result == AbpLoginResultType.Success)
                {
                    await _signInManager.SignInAsync(loginResult.Identity, false);
                    if (!string.IsNullOrEmpty(model.SingleSignIn) &&
                        model.SingleSignIn.Equals("true", StringComparison.OrdinalIgnoreCase) &&
                        loginResult.Result == AbpLoginResultType.Success)
                    {
                        var returnUrl = NormalizeReturnUrl(model.ReturnUrl);
                        loginResult.User.SetSignInToken();
                        returnUrl = AddSingleSignInParametersToReturnUrl(returnUrl, loginResult.User.SignInToken,
                            loginResult.User.Id, loginResult.User.TenantId);
                        return Redirect(returnUrl);
                    }

                    return Redirect(GetAppHomeUrl());
                }

                Logger.Warn("New registered user could not be login. This should not be normally. login result: " +
                            loginResult.Result);
            }

            return View("RegisterResult", new RegisterResultViewModel
            {
                TenancyName = tenant.TenancyName,
                NameAndSurname = user.Name + " " + user.Surname,
                UserName = user.UserName,
                EmailAddress = user.EmailAddress,
                IsActive = user.IsActive,
                IsEmailConfirmationRequired = isEmailConfirmationRequiredForLogin
            });
        }
        catch (UserFriendlyException ex)
        {
            ViewBag.UseCaptcha = !model.IsExternalLogin && UseCaptchaOnRegistration();
            ViewBag.ErrorMessage = ex.Message;

            model.PasswordComplexitySetting = await _passwordComplexitySettingStore.GetSettingsAsync();

            return View("Register", model);
        }
    }

    private bool UseCaptchaOnRegistration()
    {
        if (!AbpSession.TenantId.HasValue)
        {
            //Host users can not register
            throw new InvalidOperationException();
        }

        return SettingManager.GetSettingValue<bool>(AppSettings.UserManagement.UseCaptchaOnRegistration);
    }

    private bool UseCaptchaOnLogin()
    {
        return SettingManager.GetSettingValue<bool>(AppSettings.UserManagement.UseCaptchaOnLogin);
    }

    private bool UseCaptchaOnResetPassword()
    {
        return SettingManager.GetSettingValue<bool>(AppSettings.UserManagement.UseCaptchaOnResetPassword);
    }

    private bool UseCaptchaOnEmailActivation()
    {
        return SettingManager.GetSettingValue<bool>(AppSettings.UserManagement.UseCaptchaOnEmailActivation);
    }

    private void CheckSelfRegistrationIsEnabled()
    {
        if (!IsSelfRegistrationEnabled())
        {
            throw new UserFriendlyException(L("SelfUserRegistrationIsDisabledMessage_Detail"));
        }
    }

    private bool IsSelfRegistrationEnabled()
    {
        if (!AbpSession.TenantId.HasValue)
        {
            return false; //No registration enabled for host users!
        }

        var response = SettingManager.GetSettingValue<bool>(AppSettings.UserManagement.AllowSelfRegistration);

        return response;
    }

    private bool IsTenantSelfRegistrationEnabled()
    {
        if (AbpSession.TenantId.HasValue)
        {
            return false;
        }

        var response = SettingManager.GetSettingValue<bool>(AppSettings.TenantManagement.AllowSelfRegistration);

        return response;
    }

    #endregion

    #region ForgotPassword / ResetPassword

    public ActionResult ForgotPassword()
    {
        return View();
    }

    [HttpPost]
    public async Task<JsonResult> SendPasswordResetLink(SendPasswordResetLinkViewModel model)
    {
        await _accountAppService.SendPasswordResetCode(
            new SendPasswordResetCodeInput
            {
                EmailAddress = model.EmailAddress
            });

        return Json(new AjaxResponse());
    }

    public async Task<ActionResult> ResetPassword(ResetPasswordViewModel model)
    {
        if (model.ExpireDate < Clock.Now)
        {
            throw new UserFriendlyException(L("PasswordResetLinkExpired"));
        }

        ViewBag.UseCaptcha = UseCaptchaOnResetPassword();

        await SwitchToTenantIfNeeded(model.TenantId);

        var user = await _userManager.GetUserByIdAsync(model.UserId);
        if (user == null || user.PasswordResetCode.IsNullOrEmpty() || user.PasswordResetCode != model.ResetCode)
        {
            throw new UserFriendlyException(L("InvalidPasswordResetCode"), L("InvalidPasswordResetCode_Detail"));
        }

        model.PasswordComplexitySetting = await _passwordComplexitySettingStore.GetSettingsAsync();

        return View(model);
    }

    [HttpPost]
    public async Task<ActionResult> ResetPassword(ResetPasswordInput input)
    {
        var output = await _accountAppService.ResetPassword(input);

        if (UseCaptchaOnResetPassword())
        {
            await _recaptchaValidator.ValidateAsync(
                HttpContext.Request.Form[RecaptchaValidator.RecaptchaResponseKey]);
        }

        if (output.CanLogin)
        {
            var user = await _userManager.GetUserByIdAsync(input.UserId);
            await _signInManager.SignInAsync(user, false);

            if (!string.IsNullOrEmpty(input.SingleSignIn) &&
                input.SingleSignIn.Equals("true", StringComparison.OrdinalIgnoreCase))
            {
                user.SetSignInToken();
                var returnUrl =
                    AddSingleSignInParametersToReturnUrl(input.ReturnUrl, user.SignInToken, user.Id, user.TenantId);
                return Redirect(returnUrl);
            }
        }

        return Redirect(NormalizeReturnUrl(input.ReturnUrl));
    }

    #endregion

    #region Email activation / confirmation

    public ActionResult EmailActivation()
    {
        ViewBag.UseCaptcha = UseCaptchaOnEmailActivation();
        return View();
    }

    [HttpPost]
    public virtual async Task<JsonResult> SendEmailActivationLink(SendEmailActivationLinkInput model)
    {
        if (UseCaptchaOnEmailActivation())
        {
            await _recaptchaValidator.ValidateAsync(
                HttpContext.Request.Form[RecaptchaValidator.RecaptchaResponseKey]);
        }

        await _accountAppService.SendEmailActivationLink(model);
        return Json(new AjaxResponse());
    }

    public virtual async Task<ActionResult> EmailConfirmation(EmailConfirmationViewModel input)
    {
        await SwitchToTenantIfNeeded(input.TenantId);
        await _accountAppService.ActivateEmail(input);
        return RedirectToAction(
            "Login",
            new
            {
                successMessage = L("YourEmailIsConfirmedMessage"),
                userNameOrEmailAddress = (await _userManager.GetUserByIdAsync(input.UserId)).UserName
            });
    }

    #endregion

    #region Email change request

    public virtual async Task<ActionResult> EmailChangeRequest(EmailChangeRequestViewModel input)
    {
        await SwitchToTenantIfNeeded(input.TenantId);

        await _accountAppService.ChangeEmail(input);

        return RedirectToAction(
            "Login",
            new
            {
                successMessage = L("YourEmailIsChangedMessage"),
                userNameOrEmailAddress = (await _userManager.GetUserByIdAsync(input.UserId)).UserName
            });
    }

    #endregion

    #region External Login

    [HttpPost]
    public ActionResult ExternalLogin(string provider, string returnUrl, string ss = "")
    {
        var redirectUrl = Url.Action(
            "ExternalLoginCallback",
            "Account",
            new
            {
                ReturnUrl = returnUrl,
                authSchema = provider,
                ss = ss
            });

        var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);

        return Challenge(properties, provider);
    }

    public virtual async Task<ActionResult> ExternalLoginCallback(string returnUrl, string remoteError = null,
        string ss = "")
    {
        returnUrl = NormalizeReturnUrl(returnUrl);

        if (remoteError != null)
        {
            Logger.Error("Remote Error in ExternalLoginCallback: " + remoteError);
            throw new UserFriendlyException(L("CouldNotCompleteLoginOperation"));
        }

        var externalLoginInfo = await _signInManager.GetExternalLoginInfoAsync();
        if (externalLoginInfo == null)
        {
            Logger.Warn("Could not get information from external login.");
            return RedirectToAction(nameof(Login));
        }

        var tenancyName = GetTenancyNameOrNull();
        var shouldLockout = await IsUserLockoutEnabled();
        var loginResult = await _logInManager.LoginAsync(externalLoginInfo, tenancyName);

        switch (loginResult.Result)
        {
            case AbpLoginResultType.Success:
                { 
                    await _signInManager.SignInAsync(loginResult.Identity, false);

                    if (!string.IsNullOrEmpty(ss) && ss.Equals("true", StringComparison.OrdinalIgnoreCase) &&
                        loginResult.Result == AbpLoginResultType.Success)
                    {
                        loginResult.User.SetSignInToken();
                        returnUrl = AddSingleSignInParametersToReturnUrl(returnUrl, loginResult.User.SignInToken,
                            loginResult.User.Id, loginResult.User.TenantId);
                    }

                    return Redirect(returnUrl);
                }
            case AbpLoginResultType.UnknownExternalLogin:
                return await RegisterForExternalLogin(externalLoginInfo);
            default:
                throw _abpLoginResultTypeHelper.CreateExceptionForFailedLoginAttempt(
                    loginResult.Result,
                    externalLoginInfo.Principal.FindFirstValue(ClaimTypes.Email) ?? externalLoginInfo.ProviderKey,
                    tenancyName
                );
        }
    }

    private async Task<ActionResult> RegisterForExternalLogin(ExternalLoginInfo externalLoginInfo)
    {
        var email = externalLoginInfo.Principal.FindFirstValue(ClaimTypes.Email);

        (string name, string surname) nameInfo;
        using (var providerManager =
               _externalLoginInfoManagerFactory.GetExternalLoginInfoManager(externalLoginInfo.LoginProvider))
        {
            nameInfo = providerManager.Object.GetNameAndSurnameFromClaims(
                externalLoginInfo.Principal.Claims.ToList(), _identityOptions);
        }

        var viewModel = new RegisterViewModel
        {
            EmailAddress = email,
            Name = nameInfo.name,
            Surname = nameInfo.surname,
            IsExternalLogin = true,
            ExternalLoginAuthSchema = externalLoginInfo.LoginProvider
        };

        if (nameInfo.name != null &&
            nameInfo.surname != null &&
            email != null)
        {
            return await Register(viewModel);
        }

        return RegisterView(viewModel);
    }

    #endregion

    #region Impersonation

    [AbpMvcAuthorize(AppPermissions.Pages_Administration_Users_Impersonation)]
    public virtual async Task<JsonResult> ImpersonateUser([FromBody] ImpersonateUserInput input)
    {
        var output = await _accountAppService.ImpersonateUser(input);

        await _signInManager.SignOutAsync();

        return Json(new AjaxResponse
        {
            TargetUrl = _webUrlService.GetSiteRootAddress(output.TenancyName) +
                        "Account/ImpersonateSignIn?tokenId=" + output.ImpersonationToken
        });
    }

    [AbpMvcAuthorize(AppPermissions.Pages_Tenants_Impersonation)]
    public virtual async Task<JsonResult> ImpersonateTenant([FromBody] ImpersonateTenantInput input)
    {
        var output = await _accountAppService.ImpersonateTenant(input);

        await _signInManager.SignOutAsync();

        return Json(new AjaxResponse
        {
            TargetUrl = _webUrlService.GetSiteRootAddress(output.TenancyName) +
                        "Account/ImpersonateSignIn?tokenId=" + output.ImpersonationToken
        });
    }

    public virtual async Task<ActionResult> ImpersonateSignIn(string tokenId)
    {
        await ClearGetScriptsResponsePerUserCache();

        var result = await _impersonationManager.GetImpersonatedUserAndIdentity(tokenId);
        await _signInManager.SignInAsync(result.Identity, false);
        return RedirectToAppHome();
    }

    [AbpMvcAuthorize]
    public virtual async Task<JsonResult> DelegatedImpersonate([FromBody] DelegatedImpersonateInput input)
    {
        var output = await _accountAppService.DelegatedImpersonate(new DelegatedImpersonateInput
        {
            UserDelegationId = input.UserDelegationId
        });

        await _signInManager.SignOutAsync();

        return Json(new AjaxResponse
        {
            TargetUrl = _webUrlService.GetSiteRootAddress(output.TenancyName) +
                        "Account/DelegatedImpersonateSignIn?userDelegationId=" + input.UserDelegationId +
                        "&tokenId=" + output.ImpersonationToken
        });
    }

    public virtual async Task<ActionResult> DelegatedImpersonateSignIn(long userDelegationId, string tokenId)
    {
        await ClearGetScriptsResponsePerUserCache();

        var userDelegation = await _userDelegationManager.GetAsync(userDelegationId);
        var result = await _impersonationManager.GetImpersonatedUserAndIdentity(tokenId);

        if (userDelegation.SourceUserId != result.User.Id)
        {
            throw new UserFriendlyException("User delegation error...");
        }

        await _signInManager.SignInWithClaimsAsync(result.User, new AuthenticationProperties
        {
            IsPersistent = true,
            ExpiresUtc = userDelegation.EndTime.ToUniversalTime()
        }, result.Identity.Claims);

        return RedirectToAppHome();
    }

    public virtual JsonResult IsImpersonatedLogin()
    {
        return Json(new AjaxResponse { Result = AbpSession.ImpersonatorUserId.HasValue });
    }

    public virtual async Task<JsonResult> BackToImpersonator()
    {
        var output = await _accountAppService.BackToImpersonator();

        await _signInManager.SignOutAsync();

        return Json(new AjaxResponse
        {
            TargetUrl = _webUrlService.GetSiteRootAddress(output.TenancyName) +
                        "Account/ImpersonateSignIn?tokenId=" + output.ImpersonationToken
        });
    }

    private async Task ClearGetScriptsResponsePerUserCache()
    {
        if (!_getScriptsResponsePerUserConfiguration.IsEnabled)
        {
            return;
        }

        await _cachedUniqueKeyPerUser.RemoveKeyAsync(GetScriptsResponsePerUserCache.CacheName);
    }

    #endregion

    #region Linked Account

    [AbpMvcAuthorize]
    public virtual async Task<JsonResult> SwitchToLinkedAccount([FromBody] SwitchToLinkedAccountInput model)
    {
        var output = await _accountAppService.SwitchToLinkedAccount(model);

        await _signInManager.SignOutAsync();

        return Json(new AjaxResponse
        {
            TargetUrl = _webUrlService.GetSiteRootAddress(output.TenancyName) +
                        "Account/SwitchToLinkedAccountSignIn?tokenId=" + output.SwitchAccountToken
        });
    }

    public virtual async Task<ActionResult> SwitchToLinkedAccountSignIn(string tokenId)
    {
        var result = await _userLinkManager.GetSwitchedUserAndIdentity(tokenId);

        await _signInManager.SignInAsync(result.Identity, false);
        return RedirectToAppHome();
    }

    #endregion

    #region Change Tenant

    public async Task<ActionResult> TenantChangeModal()
    {
        var loginInfo = await _sessionCache.GetCurrentLoginInformationsAsync();
        return View("/Views/Shared/Components/TenantChange/_ChangeModal.cshtml", new ChangeModalViewModel
        {
            TenancyName = loginInfo.Tenant?.TenancyName
        });
    }

    #endregion

    #region Common

    private string GetTenancyNameOrNull()
    {
        if (!AbpSession.TenantId.HasValue)
        {
            return null;
        }

        return _tenantCache.GetOrNull(AbpSession.TenantId.Value)?.TenancyName;
    }

    private void CheckCurrentTenant(int? tenantId)
    {
        if (AbpSession.TenantId != tenantId)
        {
            throw new Exception(
                $"Current tenant is different than given tenant. AbpSession.TenantId: {AbpSession.TenantId}, given tenantId: {tenantId}");
        }
    }

    private async Task SwitchToTenantIfNeeded(int? tenantId)
    {
        if (tenantId != AbpSession.TenantId)
        {
            if (_webUrlService.SupportsTenancyNameInUrl)
            {
                throw new InvalidOperationException($"Given tenantid ({tenantId}) does not match to tenant's URL!");
            }

            SetTenantIdCookie(tenantId);
            CurrentUnitOfWork.SetTenantId(tenantId);
            await _signInManager.SignOutAsync();
        }
    }
    private async Task<bool> IsUserLockoutEnabled()
    {
        return await SettingManager.GetSettingValueAsync<bool>(AbpZeroSettingNames.UserManagement.UserLockOut.IsEnabled);
    }
    #endregion

    #region Helpers

    public ActionResult RedirectToAppHome()
    {
        return RedirectToAction("Index", "Home", new { area = "AppAreaName" });
    }

    public string GetAppHomeUrl()
    {
        return Url.Action("Index", "Home", new { area = "AppAreaName" });
    }

    private string NormalizeReturnUrl(string returnUrl, Func<string> defaultValueBuilder = null)
    {
        if (defaultValueBuilder == null)
        {
            defaultValueBuilder = GetAppHomeUrl;
        }

        if (returnUrl.IsNullOrEmpty())
        {
            return defaultValueBuilder();
        }

        if (AbpSession.UserId.HasValue)
        {
            return defaultValueBuilder();
        }

        if (Url.IsLocalUrl(returnUrl) ||
            _webUrlService.GetRedirectAllowedExternalWebSites().Any(returnUrl.Contains))
        {
            return returnUrl;
        }

        return defaultValueBuilder();
    }

    #endregion

    #region Etc

    [AbpMvcAuthorize]
    public async Task<ActionResult> TestNotification(string message = "", string severity = "info")
    {
        if (message.IsNullOrEmpty())
        {
            message = "This is a test notification, created at " + Clock.Now;
        }

        await _appNotifier.SendMessageAsync(
            AbpSession.ToUserIdentifier(),
            message,
            severity.ToPascalCase().ToEnum<NotificationSeverity>()
        );

        return Content("Sent notification: " + message);
    }

    #endregion
}

