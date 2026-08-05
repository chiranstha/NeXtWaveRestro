(function() {
  app.modals.MoveTenantsToAnotherEditionModal = function() {
    var _modalManager;
    var editionService = abp.services.app.edition;
    var $form = null;

    this.init = function(modalManager) {
      _modalManager = modalManager;

      $form = _modalManager.getModal().find('form[name=MoveTenantsToAnotherEditionForm]');
      $form.validate();
    };

    this.save = function() {
      if (!$form.valid()) {
        return;
      }

      if (!canChangeEdition()) {
        abp.message.warn(app.localize('SameEditionChangeErrorMessage'));
        return;
      }

      _modalManager.setBusy(true);

      editionService
        .moveTenantsToAnotherEdition($form.serializeFormToObject())
        .done(function() {
          abp.notify.info(app.localize('MoveTenantsToAnotherEditionStartedNotification'));
          _modalManager.close();
        })
        .always(function() {
          _modalManager.setBusy(false);
        });
    };

    function canChangeEdition() {
      var selectedEditionId = $form.find('select[name=TargetEditionId] option[selected]').val();
      var currentEditionId = $form.find('select[name=TargetEditionId]').val();
      
      return selectedEditionId !== currentEditionId;
    }
  };
})();
