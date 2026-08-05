using Abp.Application.Services.Dto;
using System;

namespace NextWave.Erp.Transaction.Dtos
{
    public class GetAllReceiptMastersInput : PagedAndSortedResultRequestDto
    {
        public string Filter { get; set; }
        public string FromMiti { get; set; }
        public string ToMiti { get; set; }
    }
}
