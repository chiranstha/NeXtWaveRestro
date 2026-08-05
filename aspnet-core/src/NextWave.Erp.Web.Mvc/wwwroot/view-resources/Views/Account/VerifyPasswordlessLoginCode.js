var CurrentPage = (function () {
  var handleValidationForm = function () {
    var $form = $('.verify-passwordless-login-code-form');
    var $remainingTimeCounter = $('.remaining-time-counter');
    $form.validate();
      $form.find('input').keypress(function (e) {
          if (e.which === 13) {
              if ($('.passwordless-login-form').valid()) {
                  $('.passwordless-login-form').submit();
              }
              return false;
          }
      });

      $form.submit(function (e) {
          e.preventDefault();

          if (!$form.valid()) {
              return;
          }

          abp.ui.setBusy(
              null,
              abp
                  .ajax({
                      contentType: app.consts.contentTypes.formUrlencoded,
                      url: $form.attr('action'),
                      data: $form.serialize(),
                  })
                  .fail(function (error) {
                      abp.ajax.showError(error);
                  })
          );
      });

    if (remainingSeconds) {
      setInterval(() => {
        remainingSeconds--;
        $remainingTimeCounter.text(
          app.localize('RemainingTime') + ': ' + app.localize('SecondShort{0}', remainingSeconds)
        );
        if (remainingSeconds === 0) {
          abp.message.warn(app.localize('TimeoutPleaseTryAgain')).then(() => {
            window.location = '/Account/PasswordlessLogin';
          });
        }
      }, 1000);
    }
  };

  return {
    init: function () {
      handleValidationForm();
    },
  };
})();
