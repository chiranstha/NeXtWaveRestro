var CurrentPage = (function () {
    var handleEmailActivation = function () {
        var $form = $('.email-activation-form');

        $form.validate();

        $form.find('input').keypress(function (e) {
            if (e.which === 13) {
                if ($('.forget-form').valid()) {
                    $('.forget-form').submit();
                }
                return false;
            }
        });

        $form.submit(function (e) {
            e.preventDefault();

            if (!$form.valid()) {
                return;
            }

            function setCaptchaToken(callback) {
                callback = callback || function () { };
                if (!abp.setting.getBoolean('App.UserManagement.UseCaptchaOnEmailActivation')) {
                    callback();
                } else {
                    grecaptcha.reExecute(function (token) {
                        $('#recaptchaResponse').val(token);
                        callback();
                    });
                }
            }

            setCaptchaToken(function () {
                abp.ui.setBusy(
                    null,
                    abp
                        .ajax({
                            contentType: app.consts.contentTypes.formUrlencoded,
                            url: $form.attr('action'),
                            data: $form.serialize(),
                        })
                        .done(function () {
                            abp.message
                                .success(app.localize('ActivationMailSentIfEmailAssociatedMessage'), app.localize('MailSent'))
                                .done(function () {
                                    location.href = abp.appPath + 'Account/Login';
                                });
                        })
                );
            });
        });
    };

    return {
        init: function () {
            handleEmailActivation();
        },
    };
})();
