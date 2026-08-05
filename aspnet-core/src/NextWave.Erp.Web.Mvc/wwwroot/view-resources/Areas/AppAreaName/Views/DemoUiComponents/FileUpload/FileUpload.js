$(function () {
    'use strict';
    // Change this to the location of your server-side upload handler:
    var url = abp.appPath + 'AppAreaName/DemoUiComponents/UploadFile';
    const id = '#fileupload';
    let dropZone;

    dropZone = new Dropzone(id, {
        url: url,
        method: 'post',
        paramName: 'file',
        maxFilesize: 999000,
        maxFiles: 1,
        clickable: id + " .dropzone-select"
    });

    dropZone.on("sending", function (file, xhr, formData) {
        var token = abp.security.antiForgery.getToken();
        formData.append('__RequestVerificationToken', token);
        formData.append('defaultFileUploadTextInput', $('#DefaultFileUploadTextInput').val());
    });

    dropZone.on("totaluploadprogress", function (data) {
        var progress = parseInt((data.loaded / data.total) * 100, 10);
        $('#progress .progress-bar').css('width', progress + '%');

    });

    dropZone.on("success", function (file, response) {
        var jsonResult = response.result;
        var fileUrl =
            abp.appPath +
            'AppAreaName/DemoUiComponents/GetFile?id=' +
            jsonResult.id +
            '&contentType=' +
            jsonResult.contentType;

        var uploadedFile =
            '<a href="' +
            fileUrl +
            '" target="_blank">' +
            app.localize('UploadedFile') +
            '</a><br/><br/>' +
            ' Free text: ' +
            jsonResult.defaultFileUploadTextInput;

        abp.message.info(uploadedFile, app.localize('PostedData'), { isHtml: true });
        abp.notify.info(app.localize('SavedSuccessfully'));

        $(id).prop('disabled', !$.support.fileInput)
            .parent()
            .addClass($.support.fileInput ? undefined : 'disabled');

    });

    dropZone.on("error", function (file,response) {
        abp.message.error(response);
    });

    dropZone.on("complete", function (file) {
        dropZone.removeFile(file);
    });

});
