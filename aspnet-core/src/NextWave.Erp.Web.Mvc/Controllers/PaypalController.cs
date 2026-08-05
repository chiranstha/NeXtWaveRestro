using System;
using System.Threading.Tasks;
using Abp.Domain.Uow;
using Microsoft.AspNetCore.Mvc;
using NextWave.Erp.MultiTenancy.Payments;
using NextWave.Erp.MultiTenancy.Payments.Paypal;
using NextWave.Erp.MultiTenancy.Payments.PayPal;
using NextWave.Erp.Web.Models.Paypal;

namespace NextWave.Erp.Web.Controllers;

public class PayPalController : ErpControllerBase
{
    private readonly PayPalPaymentGatewayConfiguration _payPalConfiguration;
    private readonly ISubscriptionPaymentRepository _subscriptionPaymentRepository;
    private readonly IPayPalPaymentAppService _payPalPaymentAppService;
    private readonly IPaymentAppService _paymentAppService;

    public PayPalController(
        PayPalPaymentGatewayConfiguration payPalConfiguration,
        ISubscriptionPaymentRepository subscriptionPaymentRepository,
        IPayPalPaymentAppService payPalPaymentAppService,
        IPaymentAppService paymentAppService)
    {
        _payPalConfiguration = payPalConfiguration;
        _subscriptionPaymentRepository = subscriptionPaymentRepository;
        _payPalPaymentAppService = payPalPaymentAppService;
        _paymentAppService = paymentAppService;
        _payPalConfiguration = payPalConfiguration;
    }

    public async Task<ActionResult> PrePayment(long paymentId)
    {
        var payment = await _paymentAppService.GetPaymentAsync(paymentId);
        SetTenantIdCookie(payment.TenantId);

        if (payment.Status != SubscriptionPaymentStatus.NotPaid)
        {
            throw new ApplicationException("This payment is processed before");
        }

        if (payment.RecurringPaymentEnabled())
        {
            throw new ApplicationException("PayPal integration doesn't support recurring payments !");
        }

        var model = new PayPalPurchaseViewModel
        {
            Payment = payment,
            Amount = payment.TotalAmount,
            Configuration = _payPalConfiguration
        };

        return View("~/Views/Payment/PayPal/PrePayment.cshtml", model);
    }

    [HttpPost]
    public async Task<ActionResult> PostPayment(long paymentId, string paypalOrderId)
    {
        try
        {
            await _payPalPaymentAppService.ConfirmPayment(paymentId, paypalOrderId);
            var returnUrl = await GetSuccessUrlAsync(paymentId);
            return Redirect(returnUrl);
        }
        catch (Exception exception)
        {
            Logger.Error(exception.Message, exception);

            var returnUrl = await GetErrorUrlAsync(paymentId);
            return Redirect(returnUrl);
        }
    }

    private async Task<string> GetSuccessUrlAsync(long paymentId)
    {
        var payment = await _subscriptionPaymentRepository.GetAsync(paymentId);
        return payment.SuccessUrl + (payment.SuccessUrl.Contains("?") ? "&" : "?") + "paymentId=" + paymentId;
    }

    private async Task<string> GetErrorUrlAsync(long paymentId)
    {
        var payment = await _subscriptionPaymentRepository.GetAsync(paymentId);
        return payment.ErrorUrl + (payment.ErrorUrl.Contains("?") ? "&" : "?") + "paymentId=" + paymentId;
    }
}

