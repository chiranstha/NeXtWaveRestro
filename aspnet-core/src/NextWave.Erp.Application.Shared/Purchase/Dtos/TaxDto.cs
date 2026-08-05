using System;

namespace NextWave.Erp.Purchase.Dtos
{
    public class TaxDto
    {
        public Guid Id { get; set; }
        public decimal Rate { get; set; }
        public string Name { get; set; }
        public Guid? LedgerId { get; set; }
    }
}
