using Abp.Application.Services.Dto;
using System;
using System.Collections.Generic;

namespace NextWave.Erp.Transaction.Dtos
{
    public class CreateOrEditPaymentDetailDto : EntityDto<Guid?>
    {
        public decimal Amount { get; set; }
        public string ChequeNo { get; set; }
        public string ChequeMiti { get; set; }
        public bool IsBillByBill { get; set; }
        public Guid LedgerId { get; set; }
        public List<GetReceiptAgainstMasterDto> PartyBalanceDetail { get; set; }
    }
}