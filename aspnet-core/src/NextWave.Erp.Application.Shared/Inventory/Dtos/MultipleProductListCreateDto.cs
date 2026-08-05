using System;

namespace NextWave.Erp.Inventory.Dtos
{
    public class MultipleProductListCreateDto
    {
        public string ProductName { get; set; }
        public string Barcode { get; set; }
        public decimal PurchaseRate { get; set; }
        public decimal SalesRate { get; set; }
        public decimal Mrp { get; set; }
        public Guid TaxId { get; set; }
    }
}
