using NextWave.Erp.Transaction.Enums;
using System;

namespace NextWave.Erp.Transaction.Dtos
{
    public class PartyBalanceOnPaymentMasterAddDto
    {
        public string VoucherNo { get; set; }
        public Guid? VoucherTypeId { get; set; }
        public string VoucherTypeName { get; set; }
        public ReferenceType ReferenceType { get; set; }
        public decimal Amount { get; set; }
        public bool IsAgainst { get; set; }
    }
}
