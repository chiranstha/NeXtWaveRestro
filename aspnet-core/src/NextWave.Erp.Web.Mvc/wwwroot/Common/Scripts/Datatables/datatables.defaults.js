/************************************************************************
 * Overrides default settings for datatables                             *
 *************************************************************************/
(function ($) {
  if (!$.fn.dataTable) {
    return;
  }

  function mapCultureToDatatablesTranslation(culture) {
    if (culture.name === 'zh-Hans' || culture.name === 'zh-CN') {
      return 'Chinese (Simplified, China)';
    }

    return culture.displayNameEnglish;
  }

  var translationsUrl =
    abp.appPath +
    'Common/Scripts/Datatables/Translations/' +
    mapCultureToDatatablesTranslation(abp.localization.currentCulture) +
    '.json';

  $.ajax(translationsUrl, { async: false })
    .fail(function () {
      translationsUrl = abp.appPath + 'Common/Scripts/Datatables/Translations/English.json';
      console.log(
        'Language is set to English for datatables, because ' +
        abp.localization.currentCulture.displayNameEnglish +
        ' is not found!'
      );
    })
    .always(function (translationJson) {
      // Customize pagination
      translationJson.paginate = {
        first: '<i class="fa fa-angle-double-left"></i>',
        last: '<i class="fa fa-angle-double-right"></i>',
        next: '<i class="fa fa-angle-right"></i>',
        previous: '<i class="fa fa-angle-left"></i>'
      }

      $.extend(true, $.fn.dataTable.defaults, {
        language: translationJson,
        lengthMenu: [5, 10, 25, 50, 100, 250, 500],
        autoWidth: true,
        pageLength: 10,
        responsive: {
          details: {
            type: 'column',
          },
        },
        searching: false,
        dom: 'rt<"row"<"col-sm-12 col-md-5 d-flex align-items-center justify-content-center justify-content-md-start" li><"col-sm-12 col-md-7 d-flex align-items-center justify-content-center justify-content-md-end" p>>',
        order: [],
      });
    });
})(jQuery);
