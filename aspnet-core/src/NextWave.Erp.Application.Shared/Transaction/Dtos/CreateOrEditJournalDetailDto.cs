using Abp.Application.Services.Dto;
using NextWave.Erp.Enums;
using System;

namespace NextWave.Erp.Transaction.Dtos
{
    public class CreateOrEditJournalDetailDto : EntityDto<Guid?>
    {
        public decimal Credit { get; set; }

        public decimal Amount { get; set; }
        public decimal Debit { get; set; }

        public DrOrCr DrOrCr { get; set; }

        public string ChequeNo { get; set; }

        public string ChequeMiti { get; set; }

        public Guid LedgerId { get; set; }
        public GetReceiptAgainstMasterDto PartyBalanceDetail { get; set; }
        public bool IsBillByBill { get; set; }
    }
}
