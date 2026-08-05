using System;

namespace NextWave.Erp.Purchase.Dtos
{
    public class VoucherDublicateDto
    {
        public string VoucherNo { get; set; }
        public decimal? GrandTotal { get; set; }
        public Guid Id { get; set; }
        public decimal? Debit { get; set; }
        public decimal? Credit { get; set; }
    }
}
