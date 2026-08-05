$(function () {
    var demoUiComponentsService = abp.services.app.demoUiComponents;

    //
    // date picker
    //
    var $selectedDate = {
        startDate: moment().startOf('day'),
    };

    $('.date-picker').daterangepicker(app.createDateTimePickerOptions(), (start) => $selectedDate.startDate = start);

    $('.test-btn-date-picker').click(function () {
        demoUiComponentsService
            .sendAndGetDate($selectedDate.startDate.format('YYYY-MM-DDTHH:mm:ssZ'))
            .done(function (result) {
                var formattedDate = moment(result.date).format('L');
                abp.message.info(formattedDate, app.localize('PostedValue'));
                abp.notify.info(app.localize('SavedSuccessfully'));
            });
    });

    //
    // datetime picker
    //

    var $selectedDateTime = {
        startDate: moment()
    };

    $('.datetime-picker').daterangepicker(app.createDateTimePickerOptions(true, 'L LT'),
        (start) => $selectedDateTime.startDate = start);

    $('.test-btn-datetime-picker').click(function () {
        demoUiComponentsService
            .sendAndGetDateTime($selectedDateTime.startDate.format('YYYY-MM-DDTHH:mm:ssZ'))
            .done(function (result) {
                var formattedDate = moment(result.date).format('L LT');
                abp.message.info(formattedDate, app.localize('PostedValue'));
                abp.notify.info(app.localize('SavedSuccessfully'));
            });
    });

    //
    // daterange picker
    //
    var selectedDateRange = {
        startDate: moment().add(-7, 'days').startOf('day'),
        endDate: moment().endOf('day'),
    };

    $('.daterange-picker').daterangepicker(
        $.extend(
            true,
            app.createDateRangePickerOptions({
                allowFutureDate: true,
            }),
            selectedDateRange
        ),
        function (start, end, label) {
            selectedDateRange.startDate = start;
            selectedDateRange.endDate = end;
        }
    );

    $('.test-btn-daterange-picker').click(function () {
        demoUiComponentsService
            .sendAndGetDateRange(selectedDateRange.startDate, selectedDateRange.endDate)
            .done(function (result) {
                var formattedStartDate = moment(result.startDate).format('L');
                var formattedEndDate = moment(result.endDate).format('L');
                var message = formattedStartDate + ' - ' + formattedEndDate;
                
                abp.message.info(message, app.localize('PostedValue'));
                abp.notify.info(app.localize('SavedSuccessfully'));
            });
    });
});
