using Abp.AutoMapper;
using NextWave.Erp.MultiTenancy.Payments.Dto;

namespace NextWave.Erp.Web.Areas.AppAreaName.Models.SubscriptionManagement;

[AutoMapFrom(typeof(SubscriptionPaymentProductDto))]
public class ShowDetailModalViewModel : SubscriptionPaymentProductDto
{
}