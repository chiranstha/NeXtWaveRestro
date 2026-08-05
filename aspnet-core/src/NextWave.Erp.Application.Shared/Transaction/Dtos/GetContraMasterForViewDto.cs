using NextWave.Erp.Transaction.Enums;
using System;

namespace NextWave.Erp.Transaction.Dtos
{
    public class GetContraMasterForViewDto
    {
        public Guid Id { get; set; }
        public ContraType Type { get; set; }

        public Guid LedgerId { get; set; }
        public string DateMiti { get; set; }

        public string LedgerName { get; set; }

        public string VoucherNo { get; set; }
        public decimal? TotalAmount { get; set; }
        public string Narration { get; set; }
        public string UpdateUser { get; set; }
        public string CreateUser { get; set; }
    }
}
