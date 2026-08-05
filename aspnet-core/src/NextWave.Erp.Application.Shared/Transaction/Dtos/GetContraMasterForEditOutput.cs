using NextWave.Erp.Transaction.Enums;
using System;
using System.Collections.Generic;

namespace NextWave.Erp.Transaction.Dtos
{
    public class GetContraMasterForEditOutput
    {
        public Guid Id { get; set; }
        public ContraType Type { get; set; }

        public string VoucherNo { get; set; }

        public DateTime? Date { get; set; }

        public decimal? TotalAmount { get; set; }

        public string Narration { get; set; }

        public string DateMiti { get; set; }

        public Guid LedgerId { get; set; }

        public string LedgerName { get; set; }

        public List<GetContraDetailForEditOutput> ContraDetails { get; set; }
    }
}
