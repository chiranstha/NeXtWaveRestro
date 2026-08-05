using Abp.Domain.Entities;
using System;
using System.Collections.Generic;

namespace NextWave.Erp.Transaction.Dtos
{
    public class ContraMasterForViewNewDto : Entity<Guid>
    {
        public string Type { get; set; }
        public Guid LedgerId { get; set; }
        public string LedgerName { get; set; }
        public string DateMiti { get; set; }
        public string Narration { get; set; }
        public string VoucherNo { get; set; }
        public decimal? TotalAmount { get; set; }
        public List<ContraDetailsForViewNewDto> Details { get; set; }
    }

    public class ContraDetailsForViewNewDto : Entity<Guid>
    {
        public decimal? Amount { get; set; }
        public string ChequeNo { get; set; }
        public string ChequeMiti { get; set; }
        public Guid LedgerId { get; set; }
        public string LedgerName { get; set; }
    }
}