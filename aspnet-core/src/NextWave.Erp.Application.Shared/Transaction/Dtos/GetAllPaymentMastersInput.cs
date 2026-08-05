using Abp.Application.Services.Dto;

namespace NextWave.Erp.Transaction.Dtos
{
    public class GetAllPaymentMastersInput : PagedAndSortedResultRequestDto
    {
        public string Filter { get; set; }
        public string FromMiti { get; set; }
        public string ToMiti { get; set; }
    }
}