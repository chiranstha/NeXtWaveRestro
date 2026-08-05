var CurrentPage = (function () {
  function setPayment() {
    var $periodType = $('input[name=PaymentPeriodType]:checked');
    $('input[name=DayCount]').val($periodType.data('day-count') ? $periodType.data('day-count') : 0);
  }

  var _passwordComplexityHelper = new app.PasswordComplexityHelper();

  var handleRegister = function () {
    app.utils.validation.addValidationMethod('tenancyNameRegex', 'TenancyName_Regex_Description');
    app.utils.validation.addValidationMethod('nameRegex', 'Name_Regex_Description');
    app.utils.validation.addValidationMethod('adminNameRegex', 'AdminName_Regex_Description');
    app.utils.validation.addValidationMethod('adminSurnameRegex', 'AdminSurname_Regex_Description');
    app.utils.validation.addValidationMethod('adminEmailAddressRegex', 'AdminEmailAddress_Regex_Description');

    $('input[name=PaymentPeriodType]').change(function () {
      setPayment();
    });

    $('input[name=PaymentPeriodType]:first').prop('checked', true);

    setPayment();

    _$registerForm = $('.register-form');

    const fields = ['TenancyName', 'Name', 'AdminName', 'AdminSurname', 'AdminEmailAddress'];
    const generatedRules = app.utils.validation.generateValidationRules(_$registerForm, fields);

    _$registerForm.validate({
      errorElement: 'div',
      errorClass: 'invalid-feedback',
      focusInvalid: false, // do not focus the last invalid input
      ignore: ':hidden',
      rules: $.extend(
        {
          AdminPasswordRepeat: {
            equalTo: '#AdminPassword',
          },
        },
        generatedRules
      ),
      messages: {},
      invalidHandler: function (event, validator) { },
      highlight: function (element) {
        $(element).closest('.form-group').find('input:eq(0)').addClass('is-invalid');
      },
      success: function (label) {
        label.closest('.form-group').find('input:eq(0)').removeClass('is-invalid');
        label.remove();
      },
      errorPlacement: function (error, element) {
        if (element.closest('.input-icon').length === 1) {
          error.insertAfter(element.closest('.input-icon'));
        } else {
          error.insertAfter(element);
        }
      },
      submitHandler: function (form) {
        function setCaptchaToken(callback) {
          callback = callback || function () { };
          if (!abp.setting.getBoolean('App.TenantManagement.UseCaptchaOnRegistration')) {
            callback();
          } else {
            grecaptcha.reExecute(function (token) {
              $('#recaptchaResponse').val(token);
              callback();
            });
          }
        }

        setCaptchaToken(function () {
          form.submit();
        });
      },
    });

    $('.register-form input').keypress(function (e) {
      if (e.which === 13) {
        if ($('.register-form').valid()) {
          $('.register-form').submit();
        }
        return false;
      }
    });

    $('input[name=AdminPassword]').pwstrength({
      i18n: {
        t: function (key) {
          return app.localize(key);
        },
      },
    });

    _passwordComplexityHelper.setPasswordComplexityRules(
      $('input[name=AdminPassword],input[name=AdminPasswordRepeat]'),
      window.passwordComplexitySetting
    );
  };

  function init() {
    handleRegister();
  }

  return {
    init: init,
  };
})();
