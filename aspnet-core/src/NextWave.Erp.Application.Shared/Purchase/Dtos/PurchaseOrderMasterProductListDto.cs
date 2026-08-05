using System;
using System.Collections.Generic;

namespace NextWave.Erp.Purchase.Dtos
{
    public class PurchaseOrderMasterProductListDto
    {
        public Guid Id { get; set; }
        public string ProductName { get; set; }
        public string ProductCode { get; set; }
        public decimal? Rate { get; set; }
        public Guid UnitId { get; set; }
        public Guid TaxId { get; set; }
        public List<PurchaseReturnUnitsQtyDto> UnitsList { get; set; }
    }
}
