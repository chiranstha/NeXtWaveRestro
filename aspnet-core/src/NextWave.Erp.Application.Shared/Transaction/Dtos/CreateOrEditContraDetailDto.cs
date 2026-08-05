using Abp.Application.Services.Dto;
using System;

namespace NextWave.Erp.Transaction.Dtos
{
    public class CreateOrEditContraDetailDto : EntityDto<Guid?>
    {
        public decimal Amount { get; set; }

        public string ChequeNo { get; set; }

        public string ChequeMiti { get; set; }
        public Guid LedgerId { get; set; }
    }
}
