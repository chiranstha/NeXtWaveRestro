using Abp.Domain.Entities;
using System;
using System.Collections.Generic;

namespace NextWave.Erp.Transaction.Dtos
{
    public class PaymentMasterForViewNewDto : Entity<Guid>
    {
        public string VoucherNo { get; set; }
        public string DateMiti { get; set; }
        public decimal TotalAmount { get; set; }
        public string Description { get; set; }
        public string LedgerName { get; set; }
        public List<PaymentDetailForViewDetailDto> Details { get; set; }
    }

    public class PaymentDetailForViewDetailDto : Entity<Guid>
    {
        public decimal? Amount { get; set; }

        public string ChequeNo { get; set; }

        public string ChequeMiti { get; set; }

        public Guid LedgerId { get; set; }
        public string LedgerName { get; set; }
        public bool IsBillByBill { get; set; }
        public List<PartyaBalanceForPaymentMasterDto> PartyBalances { get; set; }
    }

    public class PartyaBalanceForPaymentMasterDto
    {
        public string VoucherNo { get; set; }
        public string VoucherTypeName { get; set; }
        public Guid? VoucherTypeId { get; set; }
        public string ReferenceType { get; set; }
        public decimal Amount { get; set; }
    }
}