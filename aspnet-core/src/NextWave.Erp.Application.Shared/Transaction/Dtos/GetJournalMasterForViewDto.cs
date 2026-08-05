using Abp.Application.Services.Dto;
using NextWave.Erp.Enums;
using System;
using System.Collections.Generic;

namespace NextWave.Erp.Transaction.Dtos
{
    public class GetJournalMasterForViewDto : EntityDto<Guid>
    {
        public string VoucherNo { get; set; }

        public DateTime? Date { get; set; }

        public decimal? DebitTotal { get; set; }
        public decimal? CreditTotal { get; set; }

        public string ReferenceNo { get; set; }
        public string Description { get; set; }

        public string DateMiti { get; set; }

        public Guid VoucherTypeId { get; set; }
        public string VoucherType { get; set; }

        public string UpdateUser { get; set; }
        public string CreateUser { get; set; }
        public List<JournamDetailsForView> GetJournalDetail { get; set; }
    }

    public class JournamDetailsForView : EntityDto<Guid>
    {
        public decimal? Credit { get; set; }

        public decimal? Amount { get; set; }
        public decimal? Debit { get; set; }

        public DrOrCr DrOrCr { get; set; }
        public string ChequeNo { get; set; }

        public string ChequeMiti { get; set; }

        public Guid LedgerId { get; set; }

        public string LedgerName { get; set; }
        public List<PartyaBalanceForPaymentMasterDto> PartyBalances { get; set; }
        public bool IsBillByBill { get; set; }
    }
}