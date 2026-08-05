(function () {
    $(function () {
        var _tenantSettingsService = abp.services.app.tenantSettings;
        var _initialTimeZone = $('#GeneralSettingsForm [name=Timezone]').val();
        var _usingDefaultTimeZone =
            $('#GeneralSettingsForm [name=TimezoneForComparison]').val() === abp.setting.values['Abp.Timing.TimeZone'];
        var _openIdConnectClaimsManager = new KeyValueListManager();
        var _wsFederationClaimsManager = new KeyValueListManager();
        var _initialEmailSettings = $('#EmailSmtpSettingsForm').serializeFormToObject();

        //Toggle form based registration options
        var _$selfRegistrationOptions = $('#FormBasedRegistrationSettingsForm').find(
            '#Setting_AllowSelfRegistration_Content'
        );

        var _$SessionTimeOutItems = $('#FormBasedRegistrationSettingsForm').find('.divSessionTimeOut');

        $('#Settings_UseHostDefaultEmailSettings').change(function () {
            if (this.checked) {
                $('.Settings_Email_Group').slideUp();
            } else {
                $('.Settings_Email_Group').slideDown();
            }
        });

        function toggleSelfRegistrationOptions() {
            if ($('#Setting_AllowSelfRegistration').is(':checked')) {
                _$selfRegistrationOptions.slideDown('fast');
            } else {
                _$selfRegistrationOptions.slideUp('fast');
            }
        }

        function toggleSessionTimeOutItems() {
            if ($('#Setting_IsSessionTimeOutEnabled').is(':checked')) {
                _$SessionTimeOutItems.slideDown('fast');
            } else {
                _$SessionTimeOutItems.slideUp('fast');
            }
        }

        function toggleEnableCheckingLastXPasswordWhenPasswordChange() {
          if ($('#Setting_EnableCheckingLastXPasswordWhenPasswordChange').is(':checked')) {
            $('#divCheckingLastXPasswordCount').slideDown('fast');
          } else {
            $('#divCheckingLastXPasswordCount').slideUp('fast');
          }
        }

        $('#Setting_EnableCheckingLastXPasswordWhenPasswordChange').change(function () {
          toggleEnableCheckingLastXPasswordWhenPasswordChange();
        });

        function toggleEnablePasswordExpiration() {
          if ($('#Setting_EnablePasswordExpiration').is(':checked')) {
            $('#divPasswordExpirationDayCount').slideDown('fast');
            $('#divPasswordResetCodeExpirationHours').slideDown('fast');
          } else {
            $('#divPasswordExpirationDayCount').slideUp('fast');
            $('#divPasswordResetCodeExpirationHours').slideUp('fast');
          }
        }

        $('#Setting_EnablePasswordExpiration').change(function () {
          toggleEnablePasswordExpiration();
        });

        $('#Setting_AllowSelfRegistration').change(function () {
            toggleSelfRegistrationOptions();
        });

        toggleSelfRegistrationOptions();
        toggleSessionTimeOutItems();
        toggleEnableCheckingLastXPasswordWhenPasswordChange();
        toggleEnablePasswordExpiration();

        //Toggle SMTP credentials
        var _$smtpCredentialFormGroups = $('#EmailSmtpSettingsForm')
            .find('input[name=SmtpDomain],input[name=SmtpUserName],input[name=SmtpPassword]')
            .closest('.form-group');

        function toggleSmtpCredentialFormGroups() {
            if ($('#Settings_SmtpUseAuthentication').is(':checked')) {
                _$smtpCredentialFormGroups.slideUp('fast');
            } else {
                _$smtpCredentialFormGroups.slideDown('fast');
            }
        }

        $('#Settings_SmtpUseAuthentication').change(function () {
            toggleSmtpCredentialFormGroups();
        });

        $('#Setting_IsSessionTimeOutEnabled').change(function () {
            toggleSessionTimeOutItems();
        });

        toggleSmtpCredentialFormGroups();

        //Toggle LDAP credentials
        var _$ldapCredentialFormGroups = $('#LdapSettingsForm')
            .find('input[name=Domain],input[name=UserName],input[name=Password]')
            .closest('.form-group');

        function toggleLdapCredentialFormGroups() {
            if ($('#Setting_LdapIsEnabled').is(':checked')) {
                _$ldapCredentialFormGroups.slideDown('fast');
            } else {
                _$ldapCredentialFormGroups.slideUp('fast');
            }
        }

        toggleLdapCredentialFormGroups();

        $('#Setting_LdapIsEnabled').change(function () {
            toggleLdapCredentialFormGroups();
        });

        //Toggle User lockout

        var _$userLockOutSettingsFormItems = $('#UserLockOutSettingsForm')
            .find('input')
            .not('#Setting_UserLockOut_IsEnabled')
            .closest('.form-group');

        function toggleUserLockOutSettingsFormItems() {
            if ($('#Setting_UserLockOut_IsEnabled').is(':checked')) {
                _$userLockOutSettingsFormItems.slideDown('fast');
            } else {
                _$userLockOutSettingsFormItems.slideUp('fast');
            }
        }

        toggleUserLockOutSettingsFormItems();

        $('#Setting_UserLockOut_IsEnabled').change(function () {
            toggleUserLockOutSettingsFormItems();
        });

        //Toggle two factor login

    var _$twoFactorLoginSettingsFormItems = $('#TwoFactorLoginSettingsForm')
      .find('input')
      .not('#Setting_TwoFactorLogin_IsEnabled')
      .closest('.form-group');

        function toggleTwoFactorLoginSettingsFormItems() {
            if ($('#Setting_TwoFactorLogin_IsEnabled').is(':checked')) {
                _$twoFactorLoginSettingsFormItems.slideDown('fast');
            } else {
                _$twoFactorLoginSettingsFormItems.slideUp('fast');
            }
        }

        toggleTwoFactorLoginSettingsFormItems();

        $('#Setting_TwoFactorLogin_IsEnabled').change(function () {
            toggleTwoFactorLoginSettingsFormItems();
        });

        //Security
        $('#Setting_PasswordComplexity_UseDefaultSettings').change(function (val) {
            if ($(this).prop('checked')) {
                $('#PasswordComplexitySettingsForm').hide('fast', function () {
                    $('#DefaultPasswordComplexitySettingsForm').show('fast');
                });
            } else {
                $('#DefaultPasswordComplexitySettingsForm').hide('fast', function () {
                    $('#PasswordComplexitySettingsForm').show('fast');
                });
            }
        });

        function getDefaultPasswordComplexitySettings() {
            //note: this is a fix for '$('#DefaultPasswordComplexitySettingsForm').serializeFormToObject()' always returns true for checkboxes if they are disabled.
            var $disabledDefaultPasswordInputs = $('#DefaultPasswordComplexitySettingsForm input:disabled');
            $disabledDefaultPasswordInputs.removeAttr('disabled');
            var defaultPasswordComplexitySettings = $('#DefaultPasswordComplexitySettingsForm').serializeFormToObject();
            $disabledDefaultPasswordInputs.attr('disabled', 'disabled');
            var $checkboxes = $('#DefaultPasswordComplexitySettingsForm input:checkbox');
            $checkboxes.closest('checkbox');
            $checkboxes.addClass('disabled');
            return defaultPasswordComplexitySettings;
        }

        $('.upload-on-change').on("change", function () {
            // trigger nearest form submit
            $(this).closest('form').submit();
        });

        var url = abp.appPath + 'TenantCustomization/UploadDarkLogo';
        const darkLogoId = '#SettingsDarkLogoUploadButton';

        var dropZone = new Dropzone(darkLogoId, {
            url: url,
            method: 'post',
            paramName: 'file',
            maxFilesize: 102400,
            maxFiles: 1,
            clickable: darkLogoId + " .dropzone-select",
            previewsContainer: false,
            accept: function (file, done) {
                checkFile(file, 512, 128)
                    .then(() => {
                        done();
                    })
                    .catch((error) => {
                        abp.message.error(error);
                        dropZone.removeFile(file);
                        done(error);
                    });
            }
        });

        dropZone.on("sending", function (file, xhr, formData) {
            var token = abp.security.antiForgery.getToken();
            formData.append('__RequestVerificationToken', token);
        });

        dropZone.on("success", function (file, response) {
            abp.notify.info(app.localize('SavedSuccessfully'));
            refreshLogo(
                abp.appPath +
                'TenantCustomization/GetTenantLogo?skin=dark&tenantId=' +
                response.result.tenantId +
                '&t=' +
                new Date().getTime(),
                'dark'
            );
        });

        dropZone.on("error", function (file, response) {
            abp.message.error(response);
        });

        dropZone.on("complete", function (file) {
            dropZone.removeFile(file);
        });

        $('#SettingsDarkLogoUploadForm button[type=reset]').click(function () {
            _tenantSettingsService.clearDarkLogo().done(function () {
                refreshLogo(abp.appPath + 'Common/Images/app-logo-on-dark.svg', 'dark');
                abp.notify.info(app.localize('ClearedSuccessfully'));
            });
        });


        var url = abp.appPath + 'TenantCustomization/UploadDarkLogoMinimal';
        const minDarkLogoId = '#SettingsDarkLogoMinimalUploadButton';

        var dropZone = new Dropzone(minDarkLogoId, {
            url: url,
            method: 'post',
            paramName: 'file',
            maxFilesize: 51200,
            maxFiles: 1,
            clickable: minDarkLogoId + " .dropzone-select",
            previewsContainer: false,
            accept: function (file, done) {
                checkFile(file, 128, 128)
                    .then(() => {
                        done();
                    })
                    .catch((error) => {
                        abp.message.error(error);
                        dropZone.removeFile(file);
                        done(error);
                    });
            }
        });

        dropZone.on("sending", function (file, xhr, formData) {
            var token = abp.security.antiForgery.getToken();
            formData.append('__RequestVerificationToken', token);
        });

        dropZone.on("success", function (file,response) {
            abp.notify.info(app.localize('SavedSuccessfully'));
            refreshLogo(
                abp.appPath +
                'TenantCustomization/GetTenantLogo?skin=dark-sm&tenantId=' +
                response.result.tenantId +
                '&t=' +
                new Date().getTime(),
                'dark-sm'
            );
        });

        dropZone.on("error", function (file,response) {
            abp.message.error(response);
        });

        dropZone.on("complete", function (file) {
            dropZone.removeFile(file);
        });

        $('#SettingsDarkLogoMinimalUploadForm button[type=reset]').click(function () {
            _tenantSettingsService.clearDarkLogoMinimal().done(function () {
                refreshLogo(abp.appPath + 'Common/Images/app-logo-on-dark-sm.svg', 'dark-sm');

                abp.notify.info(app.localize('ClearedSuccessfully'));
            });
        });


        var url = abp.appPath + 'TenantCustomization/UploadLightLogo';
        const lightLogoId = '#SettingsLightLogoUploadForm';

        var dropZone = new Dropzone(lightLogoId, {
            url: url,
            method: 'post',
            paramName: 'file',
            maxFilesize: 102400,
            maxFiles: 1,
            clickable: lightLogoId + " .dropzone-select",
            previewsContainer: false,
            accept: function (file, done) {
                checkFile(file, 512, 128)
                    .then(() => {
                        done();
                    })
                    .catch((error) => {
                        abp.message.error(error);
                        dropZone.removeFile(file);
                        done(error);
                    });
            }
        });

        dropZone.on("sending", function (file, xhr, formData) {
            var token = abp.security.antiForgery.getToken();
            formData.append('__RequestVerificationToken', token);
        });

        dropZone.on("success", function (file, response) {
            abp.notify.info(app.localize('SavedSuccessfully'));
            refreshLogo(
                abp.appPath +
                'TenantCustomization/GetTenantLogo?skin=light&tenantId=' +
                response.result.tenantId +
                '&t=' +
                new Date().getTime(),
                'light'
            );
        });

        dropZone.on("error", function (file, response) {
            abp.message.error(response);
        });

        dropZone.on("complete", function (file) {
            dropZone.removeFile(file);
        });

        $('#SettingsLightLogoUploadForm button[type=reset]').click(function () {
            _tenantSettingsService.clearLightLogo().done(function () {
                refreshLogo(abp.appPath + 'Common/Images/app-logo-on-light.svg', 'light');
                abp.notify.info(app.localize('ClearedSuccessfully'));
            });
        });


        var url = abp.appPath + 'TenantCustomization/UploadLightLogoMinimal';
        const minLightLogoId = '#SettingsLightLogoMinimalUploadButton';

        var dropZone = new Dropzone(minLightLogoId, {
            url: url,
            method: 'post',
            paramName: 'file',
            maxFilesize: 102400,
            maxFiles: 1,
            clickable: minLightLogoId + " .dropzone-select",
            previewsContainer: false,
            accept: function (file, done) {
                checkFile(file, 128, 128)
                    .then(() => {
                        done();
                    })
                    .catch((error) => {
                        abp.message.error(error);
                        dropZone.removeFile(file);
                        done(error);
                    });
            }
        });

        dropZone.on("sending", function (file, xhr, formData) {
            var token = abp.security.antiForgery.getToken();
            formData.append('__RequestVerificationToken', token);
        });

        dropZone.on("success", function (file, response) {
            abp.notify.info(app.localize('SavedSuccessfully'));
            refreshLogo(
                abp.appPath +
                'TenantCustomization/GetTenantLogo?skin=light-sm&tenantId=' +
                response.result.tenantId +
                '&t=' +
                new Date().getTime(),
                'light-sm'
            );
        });

        dropZone.on("error", function (file, response) {
            abp.message.error(response);
        });

        dropZone.on("complete", function (file) {
            dropZone.removeFile(file);
        });

        $('#SettingsLightLogoMinimalUploadForm button[type=reset]').click(function () {
            _tenantSettingsService.clearLightLogoMinimal().done(function () {
                refreshLogo(abp.appPath + 'Common/Images/app-logo-on-light-sm.svg', 'light-sm');

                abp.notify.info(app.localize('ClearedSuccessfully'));
            });
        });
      
        function refreshLogo(url, postfix = 'dark') {
            $('.brand-' + postfix + '-logo img').attr('src', url);
            $('.brand-' + postfix + '-logo-preview-area').css('display', 'block');
            $('.brand-' + postfix + '-logo-preview-area img').attr('src', url);
        }

        function checkFile(file, widthLimit, heightLimit) {
            return new Promise((resolve, reject) => {
                if (file.type !== "image/jpeg" && file.type !== "image/png" && file.type !== "image/gif") {
                    reject(app.localize('CustomLogo_TypeError'));
                } else if (file.size > 102400) {
                    reject(app.localize('CustomLogo_SizeLimitError'));
                } else {
                    var img = new Image();
                    img.onload = function () {
                        if (this.width <= widthLimit && this.height <= heightLimit) {
                            resolve();
                        } else {
                            reject(app.localize('CustomLogo_DimensionError', widthLimit, heightLimit));
                        }
                    };
                    img.onerror = function () {
                        reject(app.localize('CustomLogo_InvalidFileError'));
                    };
                    img.src = URL.createObjectURL(file);
                }
            });
        }

        //Appearance/Custom CSS
        var url = abp.appPath + 'TenantCustomization/UploadCustomCss';
        const customCssId = '#customCssNotExist';

        dropZone = new Dropzone(customCssId, {
            url: url,
            method: 'post',
            paramName: 'file',
            maxFilesize: 1048576,
            maxFiles: 1,
            clickable: customCssId + " .dropzone-select",
            previewsContainer: false,
            accept: function (file, done) {
                checkCustomCssFile(file)
                    .then(() => {
                        done();
                    })
                    .catch((error) => {
                        abp.message.error(error);
                        dropZone.removeFile(file);
                        done(error);
                    });
            }
        });

        dropZone.on("sending", function (file, xhr, formData) {
            var token = abp.security.antiForgery.getToken();
            formData.append('__RequestVerificationToken', token);
        });

        dropZone.on("success", function (file, response) {
            refreshCustomCss(abp.appPath + 'TenantCustomization/GetCustomCss?tenantId=' + response.result.tenantId);
            $('#customCssExist').css('display', 'block');
            $('#customCssNotExist').css('display', 'none');
            abp.notify.info(app.localize('SavedSuccessfully'));
        });

        dropZone.on("error", function (file, esponse) {
            abp.message.error(response);
        });

        dropZone.on("complete", function (file) {
            dropZone.removeFile(file);
        });

        $('#SettingsCustomCssUploadForm button[type=reset]').click(function () {
            _tenantSettingsService.clearCustomCss().done(function () {
                refreshCustomCss(null);
                $('#customCssExist').css('display', 'none');
                $('#customCssNotExist').css('display', 'block');
                abp.notify.info(app.localize('ClearedSuccessfully'));
            });
        });

        function refreshCustomCss(url) {
            $('#TenantCustomCss').remove();
            if (url) {
                $('head').append('<link id="TenantCustomCss" href="' + url + '" rel="stylesheet"/>');
            }
        }

        function checkCustomCssFile(file) {
            return new Promise((resolve, reject) => {
                if (file.type !== "text/css") {
                    reject(app.localize('CustomCss_TypeError'));
                } else if(file.size > 1048576) {
                    reject(app.localize('CustomCss_SizeLimitError'));
                }
                else {
                    resolve();
                }
            });
        }

        function getSelectedOpenIdResponseTypes() {
            var openIdResponseTypes = '';
            if ($('#Setting_OpenIdConnect_ResponseType_Token').prop('checked')) {
                openIdResponseTypes += 'token';
            }
            if ($('#Setting_OpenIdConnect_ResponseType_IdToken').prop('checked')) {
                if (openIdResponseTypes.length > 0) {
                    openIdResponseTypes += ',';
                }
                openIdResponseTypes += 'id_token';
            }
            if ($('#Setting_OpenIdConnect_ResponseType_Code').prop('checked')) {
                if (openIdResponseTypes.length > 0) {
                    openIdResponseTypes += ',';
                }
                openIdResponseTypes += 'code';
            }

            return openIdResponseTypes;
        }

        //Save settings
        $('#SaveAllSettingsButton').click(function () {
            if (!IsSmtpSettingsFormValid()) {
                return;
            }

            if (!IsUserLockOutSettingsFormValid()) {
                return;
            }

            var userManagement = $('#UserManagementOtherSettingsForm').serializeFormToObject();

            userManagement = $.extend(userManagement, {
              allowSelfRegistration: $('#Setting_AllowSelfRegistration').is(':checked'),
              isNewRegisteredUserActiveByDefault: $('#Setting_IsNewRegisteredUserActiveByDefault').is(':checked'),
              isRestrictedEmailDomainEnabled: $('#Setting_IsRestrictedEmailDomainEnabled').is(':checked'),
              restrictedEmailDomain: $('#Settings_RestrictedEmailDomain').val(),
            });

            userManagement.captchaSettings = $.extend(userManagement.captchaSettings || {}, {
              useCaptchaOnRegistration: $('#Setting_UseCaptchaOnRegistration').is(':checked'),
              useCaptchaOnResetPassword: $('#Setting_UseCaptchaOnResetPassword').is(':checked'),
              useCaptchaOnEmailActivation: $('#Setting_UseCaptchaOnEmailActivation').is(':checked'),
              useCaptchaOnLogin: $('#Setting_UseCaptchaOnLogin').is(':checked')
            });

            userManagement.sessionTimeOutSettings = $.extend(userManagement.sessionTimeOutSettings || {}, {
              isEnabled: $('#Setting_IsSessionTimeOutEnabled').is(':checked'),
              timeOutSecond: $('#Setting_SessionTimeOutSecond').val(),
              showTimeOutNotificationSecond: $('#Setting_ShowTimeOutNotificationSecond').val(),
              showLockScreenWhenTimedOut: $('#Setting_ShowLockScreenWhenTimedOut').is(':checked'),
            });

            userManagement.passwordlessLogin = $.extend(userManagement.passwordlessLogin || {}, {
              isEmailProviderEnabled: $('#Setting_IsEmailPasswordlessLoginEnabled').is(':checked'),
              isSmsProviderEnabled: $('#Setting_IsSmsPasswordlessLoginEnabled').is(':checked'),
            });

            _tenantSettingsService
                .updateAllSettings({
                    general: $('#GeneralSettingsForm').serializeFormToObject(),
                    userManagement: userManagement,
                    email: $('#EmailSmtpSettingsForm').serializeFormToObject(),
                    ldap: $('#LdapSettingsForm').serializeFormToObject(),
                    billing: $('#BillingSettingsForm').serializeFormToObject(),
                    otherSettings: $('#OtherSettingsForm').serializeFormToObject(),
                    security: {
                        useDefaultPasswordComplexitySettings: $('#Setting_PasswordComplexity_UseDefaultSettings').is(':checked'),
                        passwordComplexity: $('#PasswordComplexitySettingsForm').serializeFormToObject(),
                        defaultPasswordComplexity: getDefaultPasswordComplexitySettings(),
                        userLockOut: $('#UserLockOutSettingsForm').serializeFormToObject(),
                        twoFactorLogin: $('#TwoFactorLoginSettingsForm').serializeFormToObject(),
                        AllowOneConcurrentLoginPerUser: $('#Setting_AllowOneConcurrentLoginPerUser').is(':checked'),
                        userPasswordSettings: {
                          enableCheckingLastXPasswordWhenPasswordChange: $('#Setting_EnableCheckingLastXPasswordWhenPasswordChange').is(':checked'),
                          checkingLastXPasswordCount: $('#Setting_CheckingLastXPasswordCount').val(),
                          enablePasswordExpiration: $('#Setting_EnablePasswordExpiration').is(':checked'),
                          passwordExpirationDayCount: $('#Setting_PasswordExpirationDayCount').val(),
                          passwordResetCodeExpirationHours: $('#Setting_PasswordResetCodeExpirationHours').val(),
                        }
                    },
                    externalLoginProviderSettings: {
                        facebook_IsDeactivated: $('#Setting_Facebook_IsDeactivated').prop('checked'),
                        facebook: {
                            appId: $('#Setting_Facebook_AppId').val(),
                            appSecret: $('#Setting_Facebook_AppSecret').val(),
                        },
                        google_IsDeactivated: $('#Setting_Google_IsDeactivated').prop('checked'),
                        google: {
                            clientId: $('#Setting_Google_ClientId').val(),
                            clientSecret: $('#Setting_Google_ClientSecret').val(),
                            userInfoEndpoint: $('#Setting_Google_UserInfoEndpoint').val(),
                        },
                        twitter_IsDeactivated: $('#Setting_Twitter_IsDeactivated').prop('checked'),
                        twitter: {
                            consumerKey: $('#Setting_Twitter_ConsumerKey').val(),
                            consumerSecret: $('#Setting_Twitter_ConsumerSecret').val(),
                        },
                        microsoft_IsDeactivated: $('#Setting_Microsoft_IsDeactivated').prop('checked'),
                        microsoft: {
                            clientId: $('#Setting_Microsoft_ClientId').val(),
                            clientSecret: $('#Setting_Microsoft_ClientSecret').val(),
                        },
                        openIdConnect_IsDeactivated: $('#Setting_OpenIdConnect_IsDeactivated').prop('checked'),
                        openIdConnect: {
                            clientId: $('#Setting_OpenIdConnect_ClientId').val(),
                            clientSecret: $('#Setting_OpenIdConnect_ClientSecret').val(),
                            authority: $('#Setting_OpenIdConnect_Authority').val(),
                            validateIssuer: $('#Setting_OpenIdConnect_ValidateIssuer').prop('checked'),
                            responseType: getSelectedOpenIdResponseTypes()
                        },
                        openIdConnectClaimsMapping: _openIdConnectClaimsManager.getValues().map((x) => {
                            return {
                                key: x.key,
                                claim: x.value,
                            };
                        }),
                        wsFederation_IsDeactivated: $('#Setting_WsFederation_IsDeactivated').prop('checked'),
                        wsFederation: {
                            clientId: $('#Setting_WsFederation_ClientId').val(),
                            tenant: $('#Setting_WsFederation_Tenant').val(),
                            metaDataAddress: $('#Setting_WsFederation_MetaDataAddress').val(),
                            wtrealm: $('#Setting_WsFederation_Wtrealm').val(),
                            authority: $('#Setting_WsFederation_Authority').val(),
                        },
                        wsFederationClaimsMapping: _wsFederationClaimsManager.getValues().map((x) => {
                            return {
                                key: x.key,
                                claim: x.value,
                            };
                        }),
                    },
                })
                .done(function () {
                    abp.notify.info(app.localize('SavedSuccessfully'));

                    var newTimezone = $('#GeneralSettingsForm [name=Timezone]').val();
                    if (
                        abp.clock.provider.supportsMultipleTimezone &&
                        _usingDefaultTimeZone &&
                        _initialTimeZone !== newTimezone
                    ) {
                        abp.message.info(app.localize('TimeZoneSettingChangedRefreshPageNotification')).done(function () {
                            window.location.reload();
                        });
                    }
                    _initialEmailSettings = $('#EmailSmtpSettingsForm').serializeFormToObject();
                });
        });

        $('#Settings_SmtpUseAuthentication').change(function () {
            $('#SmtpAuthenticationDiv').toggle();
        });

        $('#SendTestEmailButton').click(function () {
            if (!$('#EmailSmtpSettingsTestForm').valid()) {
                return;
            }

            var currentEmailSettings = $('#EmailSmtpSettingsForm').serializeFormToObject();

            if (JSON.stringify(_initialEmailSettings) !== JSON.stringify(currentEmailSettings)) {
                abp.message.confirm(
                    app.localize('SendEmailWithSavedSettingsWarning'),
                    app.localize('AreYouSure'),
                    function (isConfirmed) {
                        if (isConfirmed) {
                            _tenantSettingsService
                                .sendTestEmail({
                                    emailAddress: $('#TestEmailAddressInput').val(),
                                })
                                .done(function () {
                                    abp.notify.info(app.localize('TestEmailSentSuccessfully'));
                                });
                        }
                    }
                );
            } else {
                _tenantSettingsService
                    .sendTestEmail({
                        emailAddress: $('#TestEmailAddressInput').val(),
                    })
                    .done(function () {
                        abp.notify.info(app.localize('TestEmailSentSuccessfully'));
                    });
            }
        });

        $('.passwordShowButton').click(function () {
            var itemId = $(this).data('id');
            var item = $('#' + itemId);
            if (item) {
                if (item[0].type === 'password') {
                    item[0].type = 'text';

                    $(this).find('i').removeClass('fa-eye');
                    $(this).find('i').addClass('fa-eye-slash');
                    $(this).find('span').text(app.localize('Hide'));
                } else {
                    item[0].type = 'password';

                    $(this).find('i').removeClass('fa-eye-slash');
                    $(this).find('i').addClass('fa-eye');
                    $(this).find('span').text(app.localize('Show'));
                }
            }
        });

        function initializeOpenIdConnectClaimsMappings() {
            _openIdConnectClaimsManager.init({
                containerId: 'claimsMappingsContainer',
                name: 'openIdConnectClaimsMappings',
                keyName: 'ClaimKey',
                valueName: 'ClaimValue',
                items: openIdConnectClaimsMappings.map((x) => {
                    return {
                        key: x.Key,
                        value: x.Claim,
                    };
                }),
            });
        }

        initializeOpenIdConnectClaimsMappings();

        function initializeWsFederationClaimsMappings() {
            _wsFederationClaimsManager.init({
                containerId: 'Setting_WsFederation_ClaimsMappingsContainer',
                name: 'wsFederationClaimsMappings',
                keyName: 'ClaimKey',
                valueName: 'ClaimValue',
                items: wsFederationClaimsMappings.map((x) => {
                    return {
                        key: x.Key,
                        value: x.Claim,
                    };
                }),
            });
        }

        initializeWsFederationClaimsMappings();

        $('#Setting_Microsoft_UseHostSettings').change(function () {
            if ($(this).prop('checked')) {
                $('#Setting_Microsoft_ClientId').val('');
                $('#Setting_Microsoft_ClientSecret').val('');
                $('#ExternalLoginMicrosoftSettingsForm .collapse').collapse('hide');
                $('#Setting_Microsoft_IsDeactivated').prop('checked', false);
            } else {
                $('#ExternalLoginMicrosoftSettingsForm .collapse').collapse('show');
            }
        });

        $('#Setting_Twitter_UseHostSettings').change(function () {
            if ($(this).prop('checked')) {
                $('#Setting_Twitter_ConsumerKey').val('');
                $('#Setting_Twitter_ConsumerSecret').val('');
                $('#ExternalLoginTwitterSettingsForm .collapse').collapse('hide');
                $('#Setting_Twitter_IsDeactivated').prop('checked', false);
            } else {
                $('#ExternalLoginTwitterSettingsForm .collapse').collapse('show');
            }
        });

        $('#Setting_Google_UseHostSettings').change(function () {
            if ($(this).prop('checked')) {
                $('#Setting_Google_ClientId').val('');
                $('#Setting_Google_ClientSecret').val('');
                $('#Setting_Google_UserInfoEndpoint').val('');
                $('#ExternalLoginGoogleSettingsForm .collapse').collapse('hide');
                $('#Setting_Google_IsDeactivated').prop('checked', false);
            } else {
                $('#ExternalLoginGoogleSettingsForm .collapse').collapse('show');
            }
        });

        $('#Setting_Facebook_UseHostSettings').change(function () {
            if ($(this).prop('checked')) {
                $('#Setting_Facebook_AppId').val('');
                $('#Setting_Facebook_AppSecret').val('');
                $('#ExternalLoginFacebookSettingsForm .collapse').collapse('hide');
                $('#Setting_Facebook_IsDeactivated').prop('checked', false);
            } else {
                $('#ExternalLoginFacebookSettingsForm .collapse').collapse('show');
            }
        });

        $('#Setting_OpenIdConnect_UseHostSettings').change(function () {
            if ($(this).prop('checked')) {
                $('#Setting_OpenIdConnect_ClientId').val('');
                $('#Setting_OpenIdConnect_ClientSecret').val('');
                $('#Setting_OpenIdConnect_Authority').val('');
                $('#Setting_OpenIdConnect_ValidateIssuer').val('');

                $('#ExternalLoginOpenIdConnectSettingsForm .collapse').collapse('hide');
                $('#Setting_OpenIdConnect_IsDeactivated').prop('checked', false);
            } else {
                $('#ExternalLoginOpenIdConnectSettingsForm .collapse').collapse('show');
            }
        });

        $('#Setting_WsFederation_UseHostSettings').change(function () {
            if ($(this).prop('checked')) {
                $('#Setting_WsFederation_ClientId').val('');
                $('#Setting_WsFederation_Tenant').val('');
                $('#Setting_WsFederation_MetaDataAddress').val('');
                $('#Setting_WsFederation_Authority').val('');
                $('#Setting_WsFederation_Wtrealm').val('');

                $('#ExternalLoginWsFederationSettingsForm .collapse').collapse('hide');
                $('#Setting_WsFederation_IsDeactivated').prop('checked', false);
            } else {
                $('#ExternalLoginWsFederationSettingsForm .collapse').collapse('show');
            }
        });

        function initializeFormValidations() {
            $('#EmailSmtpSettingsForm').validate();
            $('#EmailSmtpSettingsTestForm').validate();
            $('#UserLockOutSettingsForm').validate();
            $('#UserLockOutSettingsForm').validate();
        }

        initializeFormValidations();

        function IsSmtpSettingsFormValid() {
            if (!$('#EmailSmtpSettingsForm').length) {
                return true;
            }
            return $('#EmailSmtpSettingsForm').valid();
        }

        function IsUserLockOutSettingsFormValid() {
            return $('#UserLockOutSettingsForm').valid();
        }

        $('#ApplicationLogoImage').change(function () {
            var fileName = app.localize('ChooseAFile');
            if (this.files && this.files[0]) {
                fileName = this.files[0].name;
            }
            $('#ApplicationLogoImageLabel').text(fileName);
        });

        $('#CustomCssFile').change(function () {
            var fileName = app.localize('ChooseAFile');
            if (this.files && this.files[0]) {
                fileName = this.files[0].name;
            }
            $('#CustomCssFileLabel').text(fileName);
        });

        $("#ExternalLoginSettingsTab .tab-pane:first").addClass("show active");
        $("#ExternalLoginSettingsTab ul.nav-tabs li:first a.nav-link").addClass("active");

        var _$restrictedEmailDomainSettingsFormItems = $('#FormBasedRegistrationSettingsForm')
            .find('input')
            .not('#Setting_IsRestrictedEmailDomainEnabled')
            .closest('.form-group');
        function toggleRestrictedEmailDomainSettingsFormItems() {
            if ($('#Setting_IsRestrictedEmailDomainEnabled').is(':checked')) {
                _$restrictedEmailDomainSettingsFormItems.slideDown('fast');
            } else {
                _$restrictedEmailDomainSettingsFormItems.slideUp('fast');
            }
        }

        toggleRestrictedEmailDomainSettingsFormItems();

        $('#Setting_IsRestrictedEmailDomainEnabled').change(function () {
            toggleRestrictedEmailDomainSettingsFormItems();
        });

        $('#Settings_RestrictedEmailDomain').on('input', function () {
          var isValid = /^[a-zA-Z0-9._%+-]+(?<!@)[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$/.test($(this).val());
          $(this).toggleClass('is-invalid', !isValid);
          $('#RestrictedEmailAddress_Error').toggle(!isValid);
          $('#SaveAllSettingsButton').prop('disabled', !isValid);
        });
    });
})();
