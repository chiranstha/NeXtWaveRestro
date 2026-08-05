using Abp.Application.Services.Dto;
using System;

namespace NextWave.Erp.Transaction.Dtos
{
    public class CreateOrEditReceiptDetailDto : EntityDto<Guid?>
    {
        public decimal Amount { get; set; }

        public string ChequeNo { get; set; }

        public string ChequeMiti { get; set; }

        public Guid LedgerId { get; set; }
        public bool IsBillByBill { get; set; }

        public GetReceiptAgainstMasterDto PartyBalanceDetail { get; set; }
    }
}