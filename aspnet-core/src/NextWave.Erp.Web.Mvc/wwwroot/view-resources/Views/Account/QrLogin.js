$(function () {
    const qrLoginImage = $("#qrLoginImage");
    const qrLoginContainer = $("#qrLoginContainer");

    const connection = new signalR.HubConnectionBuilder()
        .withUrl("/signalr-qr-login")
        .build();

    connection.start()
        .then(() => {
            connection.on("getAuthData", (result) => {
                loginWithAccessToken(result.accessToken);
            });

            connection.on("generateQrCode", (qrCode) => {
                qrLoginImage.attr("src", qrCode);
                qrLoginContainer.find(".card").show();
                qrLoginContainer.find(".spinner-border").hide();
            });

            setSessionId();
            scheduleSessionIdUpdates();

        }).catch(err => console.error("SignalR connection error:", err));

    const loginWithAccessToken = function (accessToken) {
        const model = {
            AccessToken: accessToken
        };

        abp.ajax({
            url: "/Account/LoginWithAccessToken",
            data: JSON.stringify(model)
        }).done(function (response) {
            const returnUrl = new URLSearchParams(window.location.search).get('returnUrl');
            window.location.href = returnUrl ? returnUrl : "/";
        });
    };

    const setSessionId = function () {
        if (connection) {
            connection.invoke("setSessionId");
        }
    };

    const scheduleSessionIdUpdates = function () {
        let counter = 0;
        const maxUpdates = 5;
        const interval = 60000; // 1 minute

        const intervalId = setInterval(() => {
            if (counter < maxUpdates) {
                setSessionId();
                counter++;
            } else {
                clearInterval(intervalId);
                showQrCodeExpired();
            }
        }, interval);
    };

    const showQrCodeExpired = function () {
        abp.message.confirm(
            abp.localization.localize("QrCodeExpiredMessage", "Erp"),
            abp.localization.localize("QrCodeExpiredTitle", "Erp"),
            function (isConfirmed) {
                if (isConfirmed) {
                    location.reload();
                }
            }
        );
    };
});