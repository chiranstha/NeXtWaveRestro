using NextWave.Erp.Enums;
using NextWave.Erp.Purchase.Dtos;
using System;
using System.Collections.Generic;

namespace NextWave.Erp.Sales.Dtos
{
    public class ProductWithPricingLevelDto
    {
        public Guid Id { get; set; }
        public string ProductName { get; set; }
        public ProductTypeEnum ProductType { get; set; }
        public Guid UnitId { get; set; }
        public Guid TaxId { get; set; }
        public decimal TaxRate { get; set; }
        public decimal Quantity { get; set; }
        public decimal Rate { get; set; }
        public decimal Mrp { get; set; }
        public List<PurchaseReturnUnitsQtyDto> UnitsList { get; set; }
    }
}
