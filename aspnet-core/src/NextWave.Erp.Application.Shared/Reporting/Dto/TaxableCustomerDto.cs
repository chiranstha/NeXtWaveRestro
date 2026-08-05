using System;

namespace NextWave.Erp.Reporting.Dto
{
    public class TaxableCustomerDto
    {
        public int Sn { get; set; }
        public string Name { get; set; }
        public Guid LedgerId { get; set; }
        public decimal Amount { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal GrandTotal { get; set; }
        public string PanNo { get; set; }
    }
}
