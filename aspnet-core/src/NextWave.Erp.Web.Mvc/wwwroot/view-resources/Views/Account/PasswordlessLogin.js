var CurrentPage = (function () {

    var selectedPasswordlessProvider = function () {

        var providerDropdown = $("#providerDropdown");
        var emailPasswordlessLogin = $("#emailPasswordlessLogin");
        var smsPasswordlessLogin = $("#smsPasswordlessLogin");
        var emailPasswordlessLoginInput = $("#emailPasswordlessLoginInput");
        var smsPasswordlessLoginInput = $("#smsPasswordlessLoginInput");

        if (providerDropdown.length > 0) {
            smsPasswordlessLogin.hide();
            emailPasswordlessLogin.show();
            smsPasswordlessLoginInput.prop("type", "hidden");
            emailPasswordlessLoginInput.prop("type", "text");

            providerDropdown.change(function () {
                var selectedProvider = $(this).val();
                if (selectedProvider === "Email") {
                    emailPasswordlessLogin.show();
                    emailPasswordlessLoginInput.prop("type", "text");

                    smsPasswordlessLoginInput.val('');
                    smsPasswordlessLogin.hide();
                    smsPasswordlessLoginInput.prop("type", "hidden");
                    smsPasswordlessLoginInput.prop("name", "");

                } else if (selectedProvider === "Sms") {
                    smsPasswordlessLogin.show();
                    smsPasswordlessLoginInput.prop("type", "text");

                    emailPasswordlessLoginInput.val('');
                    emailPasswordlessLogin.hide();
                    emailPasswordlessLoginInput.prop("type", "hidden");
                    emailPasswordlessLoginInput.prop("name", "");
                }
            });
        }

    };

    return {
        init: function () {
            selectedPasswordlessProvider();
        },
    };
})();
