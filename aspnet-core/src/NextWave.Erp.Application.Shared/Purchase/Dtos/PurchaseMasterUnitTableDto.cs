using System;

namespace NextWave.Erp.Purchase.Dtos
{
    public class PurchaseMasterUnitTableDto
    {
        public Guid Id { get; set; }

        public string DisplayName { get; set; }
        public decimal Rate { get; set; }
    }
}
