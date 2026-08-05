// TODO: Remove this file that's content is moved to register.js

var CurrentPage = (function () {
  var _passwordComplexityHelper = new app.PasswordComplexityHelper();

  var handleRegister = function () {

    app.utils.validation.addValidationMethod('tenancyNameRegex', 'TenancyName_Regex_Description');
    app.utils.validation.addValidationMethod('nameRegex', 'Name_Regex_Description');
    app.utils.validation.addValidationMethod('adminNameRegex', 'AdminName_Regex_Description');
    app.utils.validation.addValidationMethod('adminSurnameRegex', 'AdminSurname_Regex_Description');
    app.utils.validation.addValidationMethod('adminEmailAddressRegex', 'AdminEmailAddress_Regex_Description');

    _$registerForm = $('.register-form');

    const fields = ['TenancyName', 'Name', 'AdminName', 'AdminSurname', 'AdminEmailAddress'];
    const generatedRules = app.utils.validation.generateValidationRules(_$registerForm, fields);

    _$registerForm.validate({
      errorElement: 'span', //default input error message container
      errorClass: 'help-block', // default input error message class
      focusInvalid: false, // do not focus the last invalid input
      ignore: '',
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
        $(element).closest('.form-group').addClass('has-error');
      },

      success: function (label) {
        label.closest('.form-group').removeClass('has-error');
        label.remove();
      },

      errorPlacement: function (error, element) {
        if (element.closest('.input-icon').size() === 1) {
          error.insertAfter(element.closest('.input-icon'));
        } else {
          error.insertAfter(element);
        }
      },

      submitHandler: function (form) {
        form.submit();
      },
    });

    $('.register-form input').keypress(function (e) {
      if (e.which == 13) {
        if ($('.register-form').valid()) {
          $('.register-form').submit();
        }
        return false;
      }
    });

    _passwordComplexityHelper.setPasswordComplexityRules(
      $('input[name=AdminPassword],input[name=AdminPasswordRepeat]'),
      window.passwordComplexitySetting
    );
  };

  return {
    init: function () {
      handleRegister();
    },
  };
})();
